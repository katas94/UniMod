using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace Katas.Modman
{
    public sealed class ModErator : IModErator
    {
        public const string InfoFile = "info.json";
        public const string ModFileExtensionNoDot = "mod";
        public const string ModFileExtension = "." + ModFileExtensionNoDot;
        public const string CatalogName = "mod";
        public const string StartupAddress = "__mod_startup";
        public const string AssembliesLabel = "__mod_assembly";
        
        public static readonly string DefaultInstallationFolder = Path.Combine(Application.persistentDataPath, "Mods");

        private static ModErator _instance;
        public static ModErator Instance => _instance ?? new ModErator();

        public IEnumerable<string> InstalledModIds => _mods.Keys;
        public IEnumerable<IMod> InstalledMods => _mods.Values;
        public string InstallationFolder { get; set; }
        
        private readonly Dictionary<string, IMod> _mods;
        private readonly HashSet<string> _loadedAssemblies;

        public ModErator(string installationFolder = null)
        {
            if (_instance is not null)
                throw new Exception("[Modman] There can only be one ModErator instance");
            
            InstallationFolder = installationFolder ?? DefaultInstallationFolder;
            _mods = new();
            _loadedAssemblies = new();
            
            // get all the full names for the assemblies that are currently loaded
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _loadedAssemblies.UnionWith(assemblies.Select(assembly => assembly.FullName));
            
            _instance = this;
        }
        
        public UniTask InstallModsAsync()
        {
            return InstallModsAsync(InstallationFolder, true);
        }

        public UniTask InstallModsAsync(string folderPath, bool deleteModFilesAfter)
        {
            if (folderPath is null)
                return UniTask.CompletedTask;
            
            string[] modFiles = Directory.GetFiles(InstallationFolder);
            return InstallModsAsync(modFiles, deleteModFilesAfter);
        }
        
        public UniTask InstallModsAsync(IEnumerable<string> modFilePaths, bool deleteModFilesAfter)
        {
            async UniTask InstallAsync(string modFilePath)
            {
                // avoids throwing if one mod fails to install and log the exception instead
                try
                {
                    await InstallModAsync(modFilePath, deleteModFilesAfter);
                }
                catch (ModInstallationException exception)
                {
                    Debug.LogError(exception);
                }
            }
            
            return UniTask.WhenAll(modFilePaths.Select(InstallAsync));
        }

        public UniTask InstallModsAsync(bool deleteModFilesAfter, params string[] modFilePaths)
        {
            return InstallModsAsync(modFilePaths, deleteModFilesAfter);
        }

        public async UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter)
        {
            await UniTask.SwitchToThreadPool();
            
            if (Path.GetExtension(modFilePath) != ModFileExtension)
                return;

            string modId = Path.GetFileNameWithoutExtension(modFilePath);
            string extractPath = Path.Combine(InstallationFolder, modId);
            
            try
            {
                if (Directory.Exists(extractPath))
                    IOUtils.DeleteDirectory(extractPath);
                else if (!Directory.Exists(InstallationFolder))
                    Directory.CreateDirectory(InstallationFolder);
                
                ZipFile.ExtractToDirectory(modFilePath, InstallationFolder, true);
            }
            catch (Exception exception)
            {
                throw new ModInstallationException(modId, exception);
            }
            
            // don't throw if we could not delete the mod file but we successfully installed the mod
            try
            {
                if (deleteModFileAfter)
                    File.Delete(modFilePath);
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                Debug.LogWarning($"Could not delete mod file after installation: {modFilePath}\n{exception}");
            }

            await UniTask.SwitchToMainThread();
            Debug.Log($"Successfully installed mod {modId}");
        }

        public async UniTask UninstallModAsync(string id)
        {
            if (!_mods.TryGetValue(id, out var mod))
                return;
            
            if (mod.IsContentLoaded || mod.AreAssembliesLoaded)
            {
                Debug.LogError($"Could not uninstall mod {id}: its content or assemblies are currently loaded...");
                return;
            }
            
            await UniTask.SwitchToThreadPool();
            IOUtils.DeleteDirectory(mod.Path);
            await UniTask.SwitchToMainThread();
            
            _mods.Remove(id);
            Debug.Log($"Successfully uninstalled mod {id}");
        }

        public async UniTask RefreshInstalledModsAsync()
        {
            async UniTask<IMod> LoadAsync(string modFolder)
            {
                // avoids throwing if one mod fails to be loaded and logs the exception instead
                try
                {
                    return await LoadModFromFolderAsync(modFolder);
                }
                catch (ModLoadException exception)
                {
                    Debug.LogError(exception);
                    return null;
                }
            }
            
            if (!Directory.Exists(InstallationFolder))
                return;
            
            string[] modFolders = Directory.GetDirectories(InstallationFolder);
            var mods = await UniTask.WhenAll(modFolders.Select(LoadAsync));

            foreach (IMod mod in mods)
                if (mod is not null)
                    _mods[mod.Info.ModId] = mod;
            
            // TODO: resolve dependency tree
        }

        public UniTask<bool> TryLoadAllModsContentAsync(List<string> failedModIds)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> TryLoadAllModsAssembliesAsync(List<string> failedModIds)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<bool> TryLoadAllModsAsync(List<string> failedModIds)
        {
            throw new System.NotImplementedException();
        }

        public IMod GetInstalledMod(string id)
        {
            return _mods.TryGetValue(id, out var mod) ? mod : null;
        }

        private async UniTask<IMod> LoadModFromFolderAsync(string modFolder)
        {
            if (string.IsNullOrEmpty(modFolder) || !Directory.Exists(modFolder))
                return null;
            
            // check if the mod is already loaded
            string id = Path.GetFileName(modFolder);
            if (_mods.TryGetValue(id, out var mod))
                return mod;
            
            Debug.Log($"Loading mod {id}...");
            await UniTask.SwitchToThreadPool();
            
            // check if the info file exists
            string infoPath = Path.Combine(modFolder, InfoFile);
            if (!File.Exists(infoPath))
                throw new ModLoadException(id, $"Could not find the mod's {InfoFile} file");

            try
            {
                // try to read and parse the info file
                using StreamReader reader = File.OpenText(infoPath);
                string json = await reader.ReadToEndAsync();
                var info = JsonConvert.DeserializeObject<ModInfo>(json);
                await UniTask.SwitchToMainThread();
                
                Debug.Log($"Successfully loaded mod {id}#{info.ModVersion}");
                return new PlayerMod(modFolder, info);
            }
            catch (Exception exception)
            {
                throw new ModLoadException(id, exception);
            }
        }
    }
}