using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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

        public static readonly string DefaultInstallationFolder = Path.Combine(Application.persistentDataPath, "Mods");

        private static ModErator _instance;
        public static ModErator Instance => _instance ?? new ModErator();

        public IEnumerable<string> InstalledModIds => _mods.Keys;
        public IEnumerable<IMod> InstalledMods => _mods.Values;
        public string InstallationFolder { get; set; }
        
        private readonly Dictionary<string, IMod> _mods = new();
        
        public ModErator(string installationFolder = null)
        {
            if (_instance is not null)
                throw new Exception("[Modman] There can only be one ModErator instance");
            
            InstallationFolder = installationFolder ?? DefaultInstallationFolder;
            _instance = this;
        }
        
        public async UniTask RefreshInstallationFolderAsync()
        {
            if (!Directory.Exists(InstallationFolder))
                return;
            
            // get all the mod folders from the installation folder and refresh them. we do this just in case there has been a manual installation
            string[] modFolders = Directory.GetDirectories(InstallationFolder);
            await UniTask.WhenAll(modFolders.Select(RefreshModFolderAsync));
            
            // install any new mods and delete the mod files after the installation
            await InstallModsAsync(InstallationFolder, true);

            // TODO: resolve dependency tree
            
            // make sure that callers say on main thread after awaiting this
            await UniTask.SwitchToMainThread();
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
            // utility method to install a mod and log any exceptions instead of throwing
            async UniTask InstallWithoutThrowingAsync(string modFilePath)
            {
                try
                {
                    await InstallModAsync(modFilePath, deleteModFilesAfter);
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
            }
            
            return UniTask.WhenAll(modFilePaths.Select(InstallWithoutThrowingAsync));
        }

        public UniTask InstallModsAsync(bool deleteModFilesAfter, params string[] modFilePaths)
        {
            return InstallModsAsync(modFilePaths, deleteModFilesAfter);
        }

        public async UniTask InstallModAsync(string modFilePath, bool deleteModFileAfter)
        {
            // validate parameters
            if (string.IsNullOrEmpty(modFilePath))
                throw new Exception("The given mod file path cannot be null or empty");
            if (!File.Exists(modFilePath))
                throw new Exception($"Couldn't find mod file: {modFilePath}");
            if (Path.GetExtension(modFilePath) != ModFileExtension)
                throw new Exception($"\"{modFilePath}\": expected \"{ModErator.ModFileExtension}\" file extension");
            
            // install the mod on a separated thread
            await UniTask.SwitchToThreadPool();
            ZipArchive archive = null;
            string modId = null;
            string modFolder;

            try
            {
                // open the mod zip file and fetch the mod id by reading the name of the root folder
                archive = ZipFile.Open(modFilePath, ZipArchiveMode.Read);
                string firstEntryFullName = archive.Entries.FirstOrDefault()?.FullName;
                modId = firstEntryFullName?.Split('/')[0];
                
                if (string.IsNullOrEmpty(modId))
                    throw new Exception("Couldn't fetch the mod ID from the archive file");

                // we want to avoid to override the current installation if it is already loaded
                if (_mods.TryGetValue(modId, out var mod) && mod.IsLoaded)
                    throw new ModInstallationException(modId, "The mod ID is currently loaded. Avoiding to override the current installation...");

                // check that the extract path is ready and perform the extraction
                modFolder = Path.Combine(InstallationFolder, modId);
                
                if (Directory.Exists(modFolder))
                    IOUtils.DeleteDirectory(modFolder);
                else if (!Directory.Exists(InstallationFolder))
                    Directory.CreateDirectory(InstallationFolder);
    
                archive.ExtractToDirectory(InstallationFolder, true);
            }
            catch (ModInstallationException)
            {
                await UniTask.SwitchToMainThread();
                throw;
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                
                if (string.IsNullOrEmpty(modId))
                    throw;
                else
                    throw new ModInstallationException(modId, exception);
            }
            finally
            {
                archive?.Dispose();
            }
            
            Debug.Log($"Successfully installed mod {modId}");
            
            // mod was installed, now refresh the folder so the mod's instance gets created and registered
            await RefreshModFolderAsync(modFolder);
            
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
                return;
            }
            
            // make sure that callers stay on the main thread
            await UniTask.SwitchToMainThread();
        }

        public async UniTask UninstallModAsync(string id)
        {
            if (!_mods.TryGetValue(id, out IMod mod))
                return;
            
            bool needsRestart = await mod.UninstallAsync();
            if (!needsRestart)
                _mods.Remove(id);
        }

        public async UniTask<bool> TryLoadAllModsAsync(bool loadAssemblies, List<string> failedModIds)
        {
            // TODO once we have the depencency graph implementation we need to load mods in the correct order
            await UniTask.WhenAll(_mods.Values.Select(mod => mod.LoadAsync(loadAssemblies)));
            await UniTask.SwitchToMainThread();
            return true;
        }

        public IMod GetMod(string id)
        {
            return _mods.TryGetValue(id, out var mod) ? mod : null;
        }
        
        // assumes that the given modFolder is inside of the installation folder. It will create and register the instance for the mod
        // if it doesn't exist already. It will do nothing if it already exists. This method will run on a separate thread
        private async UniTask RefreshModFolderAsync(string modFolder)
        {
            // do nothing if the instance already exists
            string id = Path.GetFileName(modFolder);
            if (_mods.ContainsKey(id))
                return;
            
            // create and register the mod instance on a separated thread
            await UniTask.SwitchToThreadPool();
            var error = await RegisterModInstanceAsync(id);
            
            if (!string.IsNullOrEmpty(error))
                Debug.LogError($"Could not register mod's instance {id}: {error}");
            else
                Debug.Log($"Successfully registered mod instance {id}");
        }
        
        /// <summary>
        /// Tries to create and register the mod instance for the given ID. It will look for the mod folder in the installation folder and try to load
        /// its info file. This method also registers the created instance in the _mods dictionary, overriding any previous instance for the
        /// same ID.
        ///
        /// Returns a non empty error string if the mod instance could not be created and registered.
        /// </summary>
        private async UniTask<string> RegisterModInstanceAsync(string id)
        {
            // check if the info file exists
            string modFolder = Path.Combine(InstallationFolder, id);
            string infoPath = Path.Combine(modFolder, InfoFile);
            if (!File.Exists(infoPath))
                return $"Could not find the mod's {InfoFile} file";

            try
            {
                // try to read and parse the info file
                using StreamReader reader = File.OpenText(infoPath);
                string json = await reader.ReadToEndAsync();
                var info = JsonConvert.DeserializeObject<ModInfo>(json);
                
                // instantiate and register the mod instance (this will override any previous mod instance with same id)
                lock (_mods)
                    _mods[id] = new RuntimeMod(modFolder, info);
                    
                return null;
            }
            catch (Exception exception)
            {
                return exception.ToString();
            }
        }
    }
}