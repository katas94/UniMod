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

namespace Katas.UniMod
{
    /// <summary>
    /// A mod installed locally.
    /// </summary>
    public sealed class LocalMod : IMod
    {
        public readonly string ModFolder;
        
        public ModInfo Info { get; }
        public ModStatus Status { get; }
        public bool IsLoaded { get; private set; }
        public IResourceLocator ResourceLocator { get; private set; }
        public IReadOnlyList<Assembly> LoadedAssemblies => _loadedAssemblies;
        
        private readonly List<Assembly> _loadedAssemblies = new();
        private readonly bool _isAssembliesOnly;

        public LocalMod(string modFolder, ModInfo info)
        {
            ModFolder = modFolder;
            Info = info;
            _isAssembliesOnly = Info.Platform == UniModSpecification.AssembliesOnlyPlatform;
        }

        public async UniTask LoadAsync(bool loadAssemblies)
        {
            if (IsLoaded)
                return;

            if (_isAssembliesOnly && !loadAssemblies)
            {
                Debug.LogWarning($"[{Info.ModId}] the mod is assemblies only but it has been specified to not load them. Nothing will be loaded...");
                return;
            }
            
            // check mod's platform
            if (!IsPlatformSupported())
                throw new Exception($"[{Info.ModId}] this mod was built for {Info.Platform} platform");
            
            // check if the mod was built for this version of the app
            if (string.IsNullOrEmpty(Info.AppVersion))
                Debug.LogWarning($"[{Info.ModId}] could not get the app version that this mod was built for. The mod is not guaranteed to work and the application could crash or be unstable");
            if (Info.AppVersion != Application.version)
                Debug.LogWarning($"[{Info.ModId}] this mod was built for app version {Info.AppVersion}, so it is not guaranteed to work and the application could crash or be unstable");
            
            // load mod
            if (loadAssemblies)
                await LoadAssembliesAsync();
            if (!_isAssembliesOnly)
                await LoadContentAsync();
            
            IsLoaded = true;
            Debug.Log($"[{Info.ModId}] mod loaded!");
        }

        public UniTask<bool> UninstallAsync()
        {
            IOUtils.DeleteDirectory(ModFolder);
            return UniTask.FromResult(true);
        }

        public UniTask<Sprite> LoadThumbnailAsync()
        {
            throw new NotImplementedException();
        }

        private async UniTask LoadAssembliesAsync()
        {
            // fetch all the assembly file paths from the assemblies folder
            string assembliesFolder = Path.Combine(ModFolder, UniModSpecification.AssembliesFolder);
            if (!Directory.Exists(assembliesFolder))
                return;
            
            string[] paths = Directory.GetFiles(assembliesFolder, "*.dll");
            
            try
            {
                await UniTask.WhenAll(paths.Select(path => LoadAssemblyAsync(path, Debug.isDebugBuild)));
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }
        }

        private async UniTask LoadContentAsync()
        {
            // load mod content catalog
            string catalogPath = Path.Combine(ModFolder, "catalog_" + UniModSpecification.CatalogName + ".json");
            ResourceLocator = await Addressables.LoadContentCatalogAsync(catalogPath, true);
            
            // if the mod loaded any assemblies then check if it contains a startup script
            if (_loadedAssemblies.Count == 0 || !ResourceLocator.Locate(UniModSpecification.StartupAddress, typeof(object), out IList<IResourceLocation> locations))
                return;
            
            // load and execute the startup script
            IResourceLocation location = locations.FirstOrDefault();
            if (location is not null)
            {
                var startup = await Addressables.LoadAssetAsync<ModStartup>(location);
                if (startup)
                    await startup.StartAsync();
            }
        }

        // loads the given assembly file
        private async UniTask LoadAssemblyAsync (string filePath, bool loadSymbolStore)
        {
            await UniTask.SwitchToThreadPool();
            
            // try to load the assembly bytes
            (byte[] assembly, byte[] symbolStore) result = await TryLoadAssemblyBytesAsync(filePath, loadSymbolStore);
            
            // trt load the assembly into the AppDomain from the raw bytes
            Assembly assembly;
            string message;
            
            if (result.symbolStore is null)
                assembly = DomainAssemblies.Load(result.assembly, out message);
            else
                assembly = DomainAssemblies.Load(result.assembly, result.symbolStore, out message);

            if (assembly is null)
            {
                Debug.LogError($"[{Info.ModId}] failed to load the assembly: {message}");
                return;
            }
            
            // check if we have any message from the load operation 
            if (string.IsNullOrEmpty(message))
                Debug.Log($"[{Info.ModId}] successfully loaded assembly: {assembly.FullName}");
            else
                Debug.LogWarning($"[{Info.ModId}] {message}");
            
            _loadedAssemblies.Add(assembly);
        }
        
        private async UniTask<(byte[] assembly, byte[] symbolStore)> TryLoadAssemblyBytesAsync(string filePath, bool loadSymbolStore)
        {
            (byte[] assembly, byte[] symbolStore) result = (null, null);
            
            // try to load the raw assembly
            try
            {
                result.assembly = await File.ReadAllBytesAsync(filePath);
                
                if (result.assembly is null)
                    throw new Exception("Unknown error");
            }
            catch (Exception exception)
            {
                throw new Exception($"[{Info.ModId}] failed to read assembly file: {filePath}\n{exception}");
            }

            if (!loadSymbolStore)
                return result;
            
            // try to load the assembly's symbol store file
            string pdbFilePath = null;
            
            try
            {
                string folderPath = Path.GetDirectoryName(filePath) ?? string.Empty;
                pdbFilePath = Path.GetFileNameWithoutExtension(filePath);
                pdbFilePath = Path.Combine(folderPath, $"{pdbFilePath}.pdb");
                
                if (File.Exists(pdbFilePath))
                    result.symbolStore = await File.ReadAllBytesAsync(pdbFilePath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{Info.ModId}] failed to read the symbol store file: {pdbFilePath}\n{exception}");
            }
            
            return result;
        }
        
        private bool IsPlatformSupported()
        {
            if (_isAssembliesOnly)
                return true;
            
            // try to get the RuntimePlatform value from the info
            if (!Enum.TryParse(Info.Platform, false, out RuntimePlatform platform))
                return false;
            
#if UNITY_EDITOR
            // special case for unity editor (mod builds are never set to any of the Editor platforms)
            return Application.platform switch
            {
                RuntimePlatform.WindowsEditor => platform == RuntimePlatform.WindowsPlayer,
                RuntimePlatform.OSXEditor => platform == RuntimePlatform.OSXPlayer,
                RuntimePlatform.LinuxEditor => platform == RuntimePlatform.LinuxPlayer,
                _ => false
            };
#else
            return Application.platform == platform;
#endif
        }
    }
}