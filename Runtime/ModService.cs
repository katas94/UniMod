using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;

namespace Katas.Modman
{
    public class ModService : IModService
    {
        public const string InfoFile = "info.json";
        public const string ModFileExtensionNoDot = "mod";
        public const string ModFileExtension = "." + ModFileExtensionNoDot;
        public const string CatalogName = "mod";
        public const string StartupAddress = "__mod_startup";
        public const string AssembliesLabel = "__mod_assembly";

        public static readonly string ModsFolder = Path.Combine(Application.persistentDataPath, "Mods");

        protected readonly HashSet<string> _loadedAssemblies = new ();

        public async UniTask Load ()
        {
            // get all the full names for the assemblies that are currently loaded
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            _loadedAssemblies.UnionWith(assemblies.Select(assembly => assembly.FullName));

            // install any newly added mods in the mods folder (skm files)
            try
            {
                await RefreshModsFolderAsync();
            }
            catch (Exception exception)
            {
                Debug.LogError($"There was a problem while refreshing the mods folder:\nException: {exception}\nStack trace: {exception.StackTrace}");
            }
        }

        public UniTask RefreshModsFolderAsync ()
        {
            string[] files = Directory.GetFiles(ModsFolder);

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == ModFileExtension)
                {
                    ZipFile.ExtractToDirectory(file, ModsFolder, true);
                    Debug.Log($"Mod installed successfully: \"{file}\"");
                    File.Delete(file);
                }
            }

            return default;
        }

        public async UniTask LoadAllModsAsync ()
        {
            if (!Directory.Exists(ModsFolder)) return;

            string[] modFolders = Directory.GetDirectories(ModsFolder);

            foreach (string modFolder in modFolders)
            {
                string id = Path.GetFileName(modFolder);

                try
                {
                    await LoadModAsync(id);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Error while loading mod: \"{id}\".\nLoad exception: {exception}");
                }
            }
        }

        public async UniTask LoadModAsync (string id)
        {
            Debug.Log($"Loading mod: {id}...");
            // check if the mod folder exists
            string folder = Path.Combine(ModsFolder, id);
            if (!Directory.Exists(folder))
                throw new Exception($"Could not find any installed mod with id \"{id}\".");
            
            // load mod info
            var info = await LoadModInfoAsync(folder);

            // check if the mod build is compatible with the current platform
            if (!Enum.TryParse(info.Platform, false, out RuntimePlatform platform))
                throw new Exception($"Mod's target platform is unknown: \"{info.Platform}\"");
            
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
            if (string.IsNullOrEmpty(info.AppVersion))
                Debug.LogWarning("Could not get the app version that this mod was built for. The mod is not guaranteed to work and the application could crash or be unstable.");
            if (info.AppVersion != Application.version)
                Debug.LogWarning($"This mod was built for version {info.AppVersion}, so it is not guaranteed to work and the application could crash or be unstable.");

            // load mod content catalog
            string modcatalogPath = Path.Combine(folder, "catalog_" + CatalogName + ".json");
            IResourceLocator resourceLocator = await Addressables.LoadContentCatalogAsync(modcatalogPath, true);
            
            // if the mod has no assemblies we are done
            if (!info.HasAssemblies)
            {
                Debug.Log($"Mod loaded successfully: {id}");
                return;
            }
            
            // load all assemblies
            await LoadAllAssembliesAsync(info, resourceLocator);
            
            // if the mod contains a startup script, then load and execute it
            if (resourceLocator.Locate(StartupAddress, typeof(object), out IList<IResourceLocation> locations))
            {
                var startup = await Addressables.LoadAssetAsync<ModStartup>(locations.FirstOrDefault());
                if (startup)
                    await startup.StartAsync();
            }

            if (!Debug.isDebugBuild && info.DebugBuild)
                Debug.LogWarning($"{id}: This is a development build.");
        }

        protected async UniTask<ModInfo> LoadModInfoAsync(string folder)
        {
            string infoPath = Path.Combine(folder, InfoFile);

            if (!File.Exists(infoPath))
                throw new Exception($"Could not find the {InfoFile} file");
            
            using StreamReader reader = File.OpenText(infoPath);
            string json = await reader.ReadToEndAsync();
            var info = JsonConvert.DeserializeObject<ModInfo>(json);
            
            return info;
        }

        protected async UniTask LoadAllAssembliesAsync(ModInfo info, IResourceLocator locator)
        {
            // fetch all assembly locations with the label
            if (!locator.Locate(AssembliesLabel, typeof(TextAsset), out IList<IResourceLocation> assemblyLocations))
                throw new Exception("Could not load the assembly locations from Addressables");
            
            foreach (var location in assemblyLocations)
                await LoadAssemblyAsync(location, info.DebugBuild);
        }

        // loads the given assembly name from the specified resource locator. if it fineds a pdb file it will load it too so the assembly can be debuged
        protected async UniTask LoadAssemblyAsync (IResourceLocation location, bool debuggingEnabled)
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
                // trt load the assembly into the application from the raw bytes (include the symbol store if we could get one)
                Assembly assembly = symbolStoreAsset
                    ? Assembly.Load(assemblyAsset.bytes, symbolStoreAsset.bytes)
                    : Assembly.Load(assemblyAsset.bytes);

                // check if we have conflicts with any other loaded assembly
                if (_loadedAssemblies.Contains(assembly.FullName))
                    Debug.LogWarning($"Tried to load an already loaded assembly: {assembly.FullName}");
                else
                    _loadedAssemblies.Add(assembly.FullName);
            }
            catch (Exception exception)
            {
                throw new Exception($"Something went wrong while loading the assembly: {name}\n{exception}");
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