using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Katas.Modman
{
    public class RuntimeMod : IMod
    {
        public const string CatalogName = "mod";
        public const string StartupAddress = "__mod_startup";
        public const string AssembliesFolder = "Assemblies";
        
        public readonly string ModFolder;
        
        public ModInfo Info { get; }
        public ModStatus Status { get; }
        public bool IsLoaded { get; private set; }
        public bool AreAssembliesLoaded { get; private set; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies;
        
        private readonly List<Assembly> _loadedAssemblies = new();

        public RuntimeMod(string modFolder, ModInfo info)
        {
            ModFolder = modFolder;
            Info = info;
        }

        public async UniTask LoadAsync(bool loadAssemblies)
        {
            if (IsLoaded)
                return;
            
            // check if the mod build is compatible with the current platform
            if (!Enum.TryParse(Info.Platform, false, out RuntimePlatform platform))
                throw new Exception($"Mod's target platform is unknown: \"{Info.Platform}\"");
            
#if UNITY_EDITOR
            // special case for unity editor (mod builds are never set to any of the Editor platforms)
            bool isPlatformSupported = Application.platform switch
            {
                RuntimePlatform.WindowsEditor => platform == RuntimePlatform.WindowsPlayer,
                RuntimePlatform.OSXEditor => platform == RuntimePlatform.OSXPlayer,
                RuntimePlatform.LinuxEditor => platform == RuntimePlatform.LinuxPlayer,
                _ => false
            };
#else
            bool isPlatformSupported = Application.platform == platform;
#endif
            
            if (!isPlatformSupported)
                throw new Exception($"Current platform is unsupported: this mod was built for \"{platform}\"");
            
            // check if the mod was built for this version of the app
            if (string.IsNullOrEmpty(Info.AppVersion))
                Debug.LogWarning("Could not get the app version that this mod was built for. The mod is not guaranteed to work and the application could crash or be unstable.");
            if (Info.AppVersion != Application.version)
                Debug.LogWarning($"This mod was built for version {Info.AppVersion}, so it is not guaranteed to work and the application could crash or be unstable.");

            // load mod content catalog
            string modcatalogPath = Path.Combine(ModFolder, "catalog_" + CatalogName + ".json");
            ResourceLocator = await Addressables.LoadContentCatalogAsync(modcatalogPath, true);
            IsLoaded = true;
            
            // if we are not loading the assemblies or the mod has no assemblies we are done
            if (!loadAssemblies || !Info.HasAssemblies)
            {
                Debug.Log($"Mod loaded successfully: {Info.ModId}");
                return;
            }
            
            // load all assemblies
            await LoadAssembliesAsync();
            
            // if the mod contains a startup script, then load and execute it
            if (ResourceLocator.Locate(StartupAddress, typeof(object), out IList<IResourceLocation> locations))
            {
                var location = locations.FirstOrDefault();
                if (location is not null)
                {
                    var startup = await Addressables.LoadAssetAsync<ModStartup>(location);
                    if (startup)
                        await startup.StartAsync();
                }
            }

            if (!Debug.isDebugBuild && Info.DebugBuild)
                Debug.LogWarning($"{Info.ModId}: using a development build");
        }

        public UniTask UninstallAsync()
        {
            IOUtils.DeleteDirectory(ModFolder);
            return UniTask.CompletedTask;
        }

        public UniTask<Sprite> LoadThumbnailAsync()
        {
            throw new System.NotImplementedException();
        }

        private async UniTask LoadAssembliesAsync()
        {
            // get paths from all mod assembly files
            string assembliesFolder = Path.Combine(ModFolder, AssembliesFolder);
            string[] paths = Directory.GetFiles(assembliesFolder, "*.dll");
            
            // load all assemblies
            try
            {
                await UniTask.WhenAll(paths.Select(LoadAssemblyAsync));
            }
            catch (Exception)
            {
                await UniTask.SwitchToMainThread();
                throw;
            }
            
            await UniTask.SwitchToMainThread();
        }

        // loads the given assembly file
        private async UniTask LoadAssemblyAsync (string filePath)
        {
            await UniTask.SwitchToThreadPool();
            
            // try to load the assembly asset
            byte[] rawAssembly = null;
            byte[] rawSymbolStore = null;
            
            // try to load the raw assembly
            try
            {
                rawAssembly = await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to read assembly file: {filePath}\n{exception}");
            }
            
            // if debugging is enabled try to also load the symbol store asset
            string pdbFilePath = null;
            
            try
            {
                if (Info.DebugBuild)
                {
                    string folderPath = Path.GetDirectoryName(filePath);
                    pdbFilePath = Path.GetFileNameWithoutExtension(filePath);
                    pdbFilePath = Path.Combine(folderPath, $"{pdbFilePath}.pdb");
                    
                    if (File.Exists(pdbFilePath))
                        rawSymbolStore = await File.ReadAllBytesAsync(pdbFilePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to read the symbol store file: {pdbFilePath}\n{exception}");
            }

            // trt load the assembly from the raw bytes
            string error;
            bool assemblyLoadedSuccessfully;
            
            if (rawSymbolStore is null)
                assemblyLoadedSuccessfully = DomainAssemblies.Load(rawAssembly, out error);
            else
                assemblyLoadedSuccessfully = DomainAssemblies.Load(rawAssembly, rawSymbolStore, out error);

            // check if we successfully loaded
            if (!assemblyLoadedSuccessfully)
                Debug.LogError($"Failed to load the assembly: {error}");
        }
    }
}