using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Modman;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace ModmanEditor
{
    public abstract class ModBuilderBase : IModBuilder
    {
        public const string AAProfileSettingsName = "__mod_profile";
        public const string AAAssemblyLabel = "__mod_assembly";

        protected readonly ModConfig ModConfig;
        protected readonly CodeOptimization BuildMode;
        protected readonly string OutputPath;
        protected readonly RuntimePlatform TargetPlatform;

        protected string TMPAssetsFolder;

        public ModBuilderBase (ModConfig modConfig, CodeOptimization buildMode, string outputPath)
        {
            ModConfig = modConfig;
            BuildMode = buildMode;
            // make sure the output path has the proper mod extension
            OutputPath = IOUtils.EnsureFileExtension(outputPath, ModService.ModFileExtensionNoDot);
            
            // check if the current build target is supported by this builder
            RuntimePlatform? platform = BuildTargetToRuntimePlatform(EditorUserBuildSettings.activeBuildTarget);

            if (!platform.HasValue)
                throw new Exception("The current active build target is not recognised.");
            if (!SupportsPlatform(platform.Value))
                throw new Exception($"The current active build target is not supported by {GetType().Name}");
            
            TargetPlatform = platform.Value;
        }

        /// <summary>
        /// Builds the mod.
        /// </summary>
        public async UniTask BuildAsync ()
        {
            if (BuildMode == CodeOptimization.None)
                throw new Exception("The specified build mode is None. Please specify a proper build mode (Release or Debug)");
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                throw new Exception("There is no Addressables configurations within the project, did you removed it accidentally?");

            // create a new Addressable Assets group for the mod assemblies and initialiser assets (we will clean it after the build)
            AddressableAssetSettings aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            AddressableAssetProfileSettings profileSettings = aaSettings.profileSettings;
            AddressableAssetGroup assembliesGroup = aaSettings.CreateGroup("ModAssemblies", false, true, false, aaSettings.DefaultGroup.Schemas);
            string previousActiveProfileId = aaSettings.activeProfileId;
            string aaTmpProfileId = null;
            string tmpFolder = null;

            try
            {
                // verify the mod setup is correct before building
                if (!ModUtils.VerifyModSetup())
                    throw new Exception($"The current mod setup is not valid. Please initialise or check the mod setup using the \"{ModUtils.InitModSetupMenuItem}\" menu.");

                // create the temporary output folder for the mod bundle
                tmpFolder = IOUtils.CreateTmpFolder();
                string tmpOutputFolder = Path.Combine(tmpFolder, ModConfig.modId);
                Directory.CreateDirectory(tmpOutputFolder);

                // create the temporary assets folder
                TMPAssetsFolder = IOUtils.GetUniqueFolderPath("Assets");
                Directory.CreateDirectory(TMPAssetsFolder);

                // register all plugins under the mod assets folder
                string[] plugins = IOUtils.FindAllFilesWithExtension(ModUtils.ModPluginsFolder, "dll", true);

                foreach (string plugin in plugins)
                    RegisterAssembly(plugin);

                // register all the assemblies defined by assembly definition files under the mod assets root folder
                string[] assemblies = ModUtils.GetProjectAssemblyNames(ModUtils.ModAssetsFolder);
                await RegisterAssemblies(assemblies);

                // process all registered assemblies so they can be included on the addressable assets bundle
                string[] paths = Directory.GetFiles(TMPAssetsFolder); // get paths before the .meta files are created
                AssetDatabase.Refresh(); // generate meta files
                
                foreach (string path in paths)
                {
                    string guid = AssetDatabase.AssetPathToGUID(path);
                    AddressableAssetEntry entry = aaSettings.CreateOrMoveEntry(guid, assembliesGroup, true, false);
                    entry.address = Path.GetFileName(path);
                }

                // register the mod initialiser prefab in addressables (if any)
                string initialiserGuid = null;

                if (ModConfig.instanceProvider != null && !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ModConfig.instanceProvider, out initialiserGuid, out long _))
                    throw new Exception($"Could not get the asset GUID for the mod initialiser. Is the initialiser properly configured in the {nameof(ModConfig)} file?");
                
                if (initialiserGuid != null)
                {
                    AddressableAssetEntry initialiserEntry = aaSettings.CreateOrMoveEntry(initialiserGuid, assembliesGroup, true, false);
                    initialiserEntry.address = ModService.InitialiserAddress;
                }

                // prepare the Addressable Assets settings for the build
                aaSettings.OverridePlayerVersion = ModService.CatalogName;
                aaSettings.BuildRemoteCatalog = true;
                aaSettings.RemoteCatalogBuildPath = aaSettings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().BuildPath;
                aaSettings.RemoteCatalogLoadPath = aaSettings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().LoadPath;

                // clean the mod profile, create a new one and set it as the active profile
                if (profileSettings.GetAllProfileNames().Contains(AAProfileSettingsName))
                    profileSettings.RemoveProfile(profileSettings.GetProfileId(AAProfileSettingsName));
                
                aaTmpProfileId = profileSettings.AddProfile(AAProfileSettingsName, null);
                aaSettings.activeProfileId = aaTmpProfileId;

                // setup profile local paths
                string loadPath = $"{{UnityEngine.Application.persistentDataPath}}/Mods/{ModConfig.modId}";
                profileSettings.SetValue(aaTmpProfileId, "Local.BuildPath", tmpOutputFolder);
                profileSettings.SetValue(aaTmpProfileId, "Local.LoadPath", loadPath);

                // build the addressables assets bundle
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

                if (!string.IsNullOrEmpty(result.Error))
                    throw new Exception($"Failed to build assets bundle. Error message:\n{result.Error}");

                // initialise the mod config
                ModInfo info = new ();

                info.ModId = ModConfig.modId;
                info.ModVersion = ModConfig.modVersion;
                info.DisplayName = ModConfig.displayName;
                info.Platform = TargetPlatform.ToString();
                info.AppVersion = File.ReadAllText($"Assets/{ModService.SukiruVersionFile}");
                info.DebugBuild = BuildMode == CodeOptimization.Debug;
                
                var assetNames = paths.Select(path => Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path))); // remove .bytes and .dll/.pdb
                var assemblyNames = new HashSet<string>(assetNames);
                // info.assemblies = assemblyNames.ToArray();

                // write the config.json file
                string serializedConfig = JsonConvert.SerializeObject(info, Formatting.Indented);
                File.WriteAllText(Path.Combine(tmpOutputFolder, ModService.ConfigFile), serializedConfig);

                // compress all mod contents into the final .skm (Sukiru Mod) file
                if (File.Exists(OutputPath)) File.Delete(OutputPath);
                ZipFile.CreateFromDirectory(tmpOutputFolder, OutputPath, CompressionLevel.Optimal, true);
            }
            finally // cleanup
            {
                IOUtils.DeleteDirectory(tmpFolder);
                IOUtils.DeleteDirectory(TMPAssetsFolder, true);
                aaSettings.RemoveGroup(assembliesGroup);
                profileSettings.RemoveProfile(aaTmpProfileId);
                aaSettings.activeProfileId = previousActiveProfileId;

                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Builds and registers all the given assembly names. It must use the RegisterAssembly() method to register each assembly.
        /// </summary>
        protected abstract UniTask RegisterAssemblies (string[] assemblyNames);

        /// <summary>
        /// Returns true if this ModBuilder supports the given runtime platform
        /// </summary>
        protected abstract bool SupportsPlatform (RuntimePlatform platform);

        /// <summary>
        /// Includes the given assembly for the build.
        /// </summary>
        protected virtual void RegisterAssembly (string path, bool throwIfNotFound = true)
        {
            if (string.IsNullOrEmpty(TMPAssetsFolder) || !Directory.Exists(TMPAssetsFolder))
                throw new Exception("You tried to register an assembly without an initialised temporary assets folder.");
            
            // the destination paths use the .bytes format so they are recognised as TextAsset
            // this will allow us to include the files on the Addressable Assets bundle
            string name = Path.GetFileNameWithoutExtension(path);
            string dllSrcPath = path;
            string dllDestPath = Path.Combine(TMPAssetsFolder, name) + ".dll.bytes";
            string pdbSrcPath = Path.ChangeExtension(path, ".pdb");
            string pdbDestPath = Path.Combine(TMPAssetsFolder, name) + ".pdb.bytes";

            // check if the assembly exists
            if (!File.Exists(dllSrcPath))
            {
                if (throwIfNotFound)
                    throw new FileNotFoundException($"Could not find the assembly file at \"{dllSrcPath}\"");
                else
                    return;
            }

            // check if the assembly is a valid net managed assembly
            if (!IsValidAssembly(dllSrcPath))
                throw new Exception($"\"{dllSrcPath}\" is not a valid NET managed assebmly.");
            
            File.Copy(dllSrcPath, dllDestPath, true);

            // copy pdb file only if in debug mode. do nothing if the pdb is not found
            if (BuildMode == CodeOptimization.Debug && File.Exists(pdbSrcPath))
                File.Copy(pdbSrcPath, pdbDestPath, true);
        }

        /// <summary>
        /// Checks if the given filePath corresponds to a valid managed NET assembly.
        /// </summary>
        protected static bool IsValidAssembly (string filePath)
        {
            try
            {
                AssemblyName testAssembly = AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected static RuntimePlatform? BuildTargetToRuntimePlatform(BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.PS4 => RuntimePlatform.PS4,
                BuildTarget.PS5 => RuntimePlatform.PS5,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.CloudRendering => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                BuildTarget.Switch => RuntimePlatform.Switch,
                BuildTarget.WSAPlayer => RuntimePlatform.WSAPlayerARM,
                BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.tvOS => RuntimePlatform.tvOS,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.Lumin => RuntimePlatform.Lumin,
                BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                BuildTarget.Stadia => RuntimePlatform.Stadia,
                BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxArm64,
                _ => null
            };
        }
    }
}
