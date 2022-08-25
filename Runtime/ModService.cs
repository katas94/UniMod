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

using Object = UnityEngine.Object;

namespace Modman
{
    public class ModService : IModService
    {
        public const string MOD_FILE_EXTENSION_NO_DOT = "skm";
        public const string MOD_FILE_EXTENSION = "." + MOD_FILE_EXTENSION_NO_DOT;
        public const string CATALOG_NAME = "mod";
        public const string CONFIG_FILE = "config.json";
        public const string SUKIRU_VERSION_FILE = "SukiruVersion.txt";
        public const string INITIALISER_ADDRESS = "__mod_initialiser";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private const string BUILD_TARGET = "StandaloneWindows64";
#elif UNITY_ANDROID
        private const string BUILD_TARGET = "Android";
#else
        private const string BUILD_TARGET = "Unkown build target";
#endif

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
                await RefreshModsFolder();
            }
            catch (Exception exception)
            {
                Debug.LogError($"There was a problem while refreshing the mods folder:\nException: {exception}\nStack trace: {exception.StackTrace}");
            }
        }

        public UniTask RefreshModsFolder ()
        {
            string[] files = Directory.GetFiles(ModsFolder);

            foreach (string file in files)
            {
                if (Path.GetExtension(file) == MOD_FILE_EXTENSION)
                {
                    ZipFile.ExtractToDirectory(file, ModsFolder, true);
                    Debug.Log($"Mod installed successfully: \"{file}\"");
                    File.Delete(file);
                }
            }

            return default;
        }

        public async UniTask LoadAllMods ()
        {
            if (!Directory.Exists(ModsFolder)) return;

            string[] modFolders = Directory.GetDirectories(ModsFolder);

            foreach (string modFolder in modFolders)
            {
                string id = Path.GetFileName(modFolder);

                try
                {
                    await LoadMod(id);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Error while loading mod: \"{id}\".\nLoad exception: {exception}");
                }
            }
        }

        public async UniTask LoadMod (string id)
        {
            Debug.Log($"Loading mod: {id}...");
            string folder = Path.Combine(ModsFolder, id);

            if (!Directory.Exists(folder))
                throw new Exception($"Could not find any installed mod with id \"{id}\".");
            
            // load mod content catalog
            string modcatalogPath = Path.Combine(folder, "catalog_" + CATALOG_NAME + ".json");
            IResourceLocator resourceLocator = await Addressables.LoadContentCatalogAsync(modcatalogPath, true);
            
            // load mod config file
            string modConfigPath = Path.Combine(folder, CONFIG_FILE);

            if (!File.Exists(modConfigPath))
                throw new Exception($"Could not find the config.json file.");
            
            using StreamReader reader = File.OpenText(modConfigPath);
            string json = await reader.ReadToEndAsync();
            var config = JsonConvert.DeserializeObject<ModConfig>(json);

            // check if the mod build is compatible with the current platform
            if (!Enum.TryParse(config.platform, false, out RuntimePlatform platform))
                throw new Exception($"Mod's target platform is unknown: \"{config.platform}\"");

            // check if the mod was built for this version of the game
            if (string.IsNullOrEmpty(config.sukiruVersion))
                Debug.LogWarning("Could not get the Sukiru version that this mod was built for. The mod is not guaranted to work and the game could crash or be unstable.");
            if (config.sukiruVersion != Application.version)
                Debug.LogWarning($"This mod was built for Sukiru {config.sukiruVersion}, so it is not guaranted to work and the game could crash or be unstable.");

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
                throw new Exception($"Current platform is unsupported: this mod build was targeted for \"{platform}\"");
            
            // load all the mod assemblies specified in the config file
            if (config.assemblies != null)
                foreach (string assembly in config.assemblies)
                    await LoadAssembly(resourceLocator, assembly);

            // initialise the mod (if it contains an initialiser)
            if (resourceLocator.Locate(INITIALISER_ADDRESS, typeof(GameObject), out IList<IResourceLocation> locations))
            {
                var initialiserGo = await Addressables.LoadAssetAsync<GameObject>(locations.FirstOrDefault());
                initialiserGo = Object.Instantiate(initialiserGo);
                var initialiser = initialiserGo.GetComponent<ModInitialiser>();

                if (initialiser == null)
                    throw new Exception("Could not get the mod initialiser component from the initialiser prefab.");
                
                await initialiser.Initialise();
            }

            if (!Debug.isDebugBuild && config.debugBuild)
                Debug.LogWarning($"{id}: This is a development build.");

            Debug.Log($"Mod loaded successfully: {id}");
        }

        // loads the given assembly name from the specified resource locator. if it fineds a pdb file it will load it too so the assembly can be debuged
        protected async UniTask LoadAssembly (IResourceLocator locator, string name)
        {
            string dllKey = name + ".dll.bytes";
            string pdbKey = name + ".pdb.bytes";

            if (!locator.Locate(dllKey, typeof(TextAsset), out IList<IResourceLocation> dllLocations))
                throw new Exception($"Could not load assembly from the mod content catalog: {name}.dll");
            
            Assembly assembly;
            TextAsset rawAssembly = await Addressables.LoadAssetAsync<TextAsset>(dllLocations.FirstOrDefault());

            // if we are able to locate a symbol store file, then load it with the assembly so it can be debuged
            if (locator.Locate(pdbKey, typeof(TextAsset), out IList<IResourceLocation> pdbLocations))
            {
                TextAsset rawSymbolStore = await Addressables.LoadAssetAsync<TextAsset>(pdbLocations.FirstOrDefault());
                assembly = Assembly.Load(rawAssembly.bytes, rawSymbolStore.bytes);
            }
            else
                assembly = Assembly.Load(rawAssembly.bytes);
            
            // check if we have conflicts with any other loaded assembly
            if (_loadedAssemblies.Contains(assembly.FullName))
                Debug.LogWarning($"Tried to load an already loaded assembly: {assembly.FullName}");
            
            _loadedAssemblies.Add(assembly.FullName);
        }
    }
}