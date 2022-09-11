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
        public const string AssembliesLabel = "__mod_assembly";
        
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
                var startup = await Addressables.LoadAssetAsync<ModStartup>(locations.FirstOrDefault());
                if (startup)
                    await startup.StartAsync();
            }

            if (!Debug.isDebugBuild && Info.DebugBuild)
                Debug.LogWarning($"{Info.ModId}: This is a development build.");
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

        public void UnloadThumbnail()
        {
            throw new System.NotImplementedException();
        }

        private async UniTask LoadAssembliesAsync()
        {
            // fetch all assembly locations
            if (!ResourceLocator.Locate(AssembliesLabel, typeof(TextAsset), out IList<IResourceLocation> assemblyLocations))
                throw new Exception("Could not load the assembly locations from Addressables");
            
            foreach (var location in assemblyLocations)
                await LoadAssemblyAsync(location, Info.DebugBuild);
        }

        // loads the assembly from the given location
        private async UniTask LoadAssemblyAsync (IResourceLocation location, bool debuggingEnabled)
        {
            string name = location.PrimaryKey;
            TextAsset assemblyAsset;
            TextAsset symbolStoreAsset = null;
            
            // try to load the assembly asset
            try
            {
                assemblyAsset = await Addressables.LoadAssetAsync<TextAsset>(location);
            }
            catch (Exception exception)
            {
                throw new Exception($"Failed to load assembly from Addressables: {name}\n{exception}");
            }
            
            // if debugging is enabled try to also load the symbol store asset
            try
            {
                if (debuggingEnabled)
                    symbolStoreAsset = await Addressables.LoadAssetAsync<TextAsset>($"{name}.pdb");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Failed to load the symbol store file for the assembly: {name}\n{exception}");
            }

            try
            {
                // trt load the assembly from the raw bytes
                string error;
                bool assemblyLoadedSuccessfully;
                
                if (symbolStoreAsset)
                    assemblyLoadedSuccessfully = DomainAssemblies.Load(assemblyAsset.bytes, symbolStoreAsset.bytes, out error);
                else
                    assemblyLoadedSuccessfully = DomainAssemblies.Load(assemblyAsset.bytes, out error);

                // check if we successfully loaded
                if (!assemblyLoadedSuccessfully)
                    Debug.LogError($"Failed to load the assembly: {error}");
            }
            finally
            {
                // release the assets
                if (assemblyAsset)
                    Addressables.Release(assemblyAsset);
                if (symbolStoreAsset)
                    Addressables.Release(symbolStoreAsset);
            }
        }
    }
}