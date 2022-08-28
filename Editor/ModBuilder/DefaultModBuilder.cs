using System;
using System.IO;
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
    public enum ModAssemblyBuilderType
    {
        Fast,
        PlatformSpecific,
        Custom
    }
    
    [CreateAssetMenu(fileName = "DefaultModBuilder", menuName = "Modman/Default Mod Builder")]
    public class DefaultModBuilder : ModBuilder
    {
        public const string AAProfileSettingsName = "__mod_profile";
        
        public CompressionLevel compressionLevel = CompressionLevel.Optimal;
        public ModAssemblyBuilderType assemblyBuilderType = ModAssemblyBuilderType.PlatformSpecific;
        public List<CustomModAssemblyBuilder> customAssemblyBuilders;
        public bool ignoreMissingAssemblies = false;
        
        /// <summary>
        /// Builds the mod with the specified parameters.
        /// </summary>
        public override async UniTask BuildAsync (ModConfig config, CodeOptimization buildMode, string outputPath)
        {
            // validate parameters
            if (config is null)
                throw new NullReferenceException("Null mod configuration");
            if (buildMode == CodeOptimization.None)
                throw new Exception("The specified build mode is None. Please specify a valid build mode (Release or Debug)");
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("Build output path cannot be null or empty");
            
            // get current active build target and try to get the equivalent runtime platform value
            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            if (!TryGetRuntimePlatformFromBuildTarget(buildTarget, out var runtimePlatform))
                throw new Exception($"Couldn't get the equivalent runtime platform value for the current active build target: {buildTarget}");
            
            // check if the mod config includes any assemblies, in that case check if we have an assembly builder that supports the current active platform
            var includedAssemblyNames = config.GetAllIncludes(buildTarget);
            bool hasAssemblies = includedAssemblyNames.Count > 0;
            IModAssemblyBuilder assemblyBuilder = null;

            if (hasAssemblies && !TryGetModAssemblyBuilder(buildTarget, out assemblyBuilder))
                throw new Exception($"Could not find a mod assembly builder that supports the current build target: {buildTarget}");
            
            // make sure the output path has the proper mod extension
            outputPath = IOUtils.EnsureFileExtension(outputPath, ModService.ModFileExtensionNoDot);
            
            // initialize Addressables if it was not before
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);

            // create a new Addressable Assets group for the assemblies and startup assets (we will clean it after the build)
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            var aaProfileSettings = aaSettings.profileSettings;
            var aaModAssetsGroup = aaSettings.CreateGroup("__mod_assets", false, true, false, aaSettings.DefaultGroup.Schemas);
            string previousActiveAaProfileId = aaSettings.activeProfileId;
            string aaTmpProfileId = null;
            string tmpFolder = null;
            string tmpAssembliesFolder = null;
            
            // add the assemblies label to Addressable settings
            aaSettings.AddLabel(ModService.AssembliesLabel, false);

            try
            {
                // create the temporary output folder for the Addressables build
                tmpFolder = IOUtils.CreateTmpFolder();
                string tmpOutputFolder = Path.Combine(tmpFolder, config.modId);
                Directory.CreateDirectory(tmpOutputFolder);

                // if the mod has assemblies, build them and include them on the Addressables build
                if (hasAssemblies)
                {
                    // create the temporary folder for the assemblies inside the Assets folder
                    tmpAssembliesFolder = IOUtils.GetUniqueFolderPath("Assets");
                    Directory.CreateDirectory(tmpAssembliesFolder);
                    
                    // build the assemblies and fetch the output paths
                    var assemblyPaths = await assemblyBuilder.BuildAssembliesAsync(config, buildMode, buildTarget);
                    
                    // copy each assembly to the tmp folder in Assets, changing the extensions to .bytes
                    foreach (string assemblyPath in assemblyPaths)
                        CopyAssemblyForAddressablesBuild(assemblyPath, tmpAssembliesFolder, buildMode);
                    
                    // process all copied assemblies and include them on Addressables
                    string[] paths = Directory.GetFiles(tmpAssembliesFolder); // get paths before the .meta files are created
                    AssetDatabase.Refresh(); // generate meta files
                    
                    foreach (string path in paths)
                    {
                        string guid = AssetDatabase.AssetPathToGUID(path);
                        string assemblyName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path));
                        var entry = aaSettings.CreateOrMoveEntry(guid, aaModAssetsGroup, true, false);
                        
                        // only assembly assets are given a label so we can easely fetch all of them from Addressables
                        if (path.EndsWith(".dll.bytes"))
                        {
                            entry.address = assemblyName;
                            entry.SetLabel(ModService.AssembliesLabel, true, false, false);
                        }
                        else
                        {
                            // pdb files are given an address the same as its assembly address plus the pdb extension
                            entry.address = $"{assemblyName}.pdb";
                        }
                    }
                }

                // if the mod has assemblies then it may also include a startup script, in that case include it in the Addressables build
                if (hasAssemblies && config.startup)
                {
                    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config.startup, out string startupGui, out long _))
                        throw new Exception($"Could not get the asset GUID for the mod startup instance.");
                    
                    var entry = aaSettings.CreateOrMoveEntry(startupGui, aaModAssetsGroup, true, false);
                    entry.address = ModService.StartupAddress;
                }

                // prepare the Addressable Assets settings for the build
                aaSettings.OverridePlayerVersion = ModService.CatalogName;
                aaSettings.BuildRemoteCatalog = true;
                aaSettings.RemoteCatalogBuildPath = aaSettings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().BuildPath;
                aaSettings.RemoteCatalogLoadPath = aaSettings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().LoadPath;

                // clean the mod profile, create a new one and set it as the active profile
                if (aaProfileSettings.GetAllProfileNames().Contains(AAProfileSettingsName))
                    aaProfileSettings.RemoveProfile(aaProfileSettings.GetProfileId(AAProfileSettingsName));
                
                aaTmpProfileId = aaProfileSettings.AddProfile(AAProfileSettingsName, null);
                aaSettings.activeProfileId = aaTmpProfileId;

                // setup profile local paths
                string loadPath = $"{{UnityEngine.Application.persistentDataPath}}/Mods/{config.modId}";
                aaProfileSettings.SetValue(aaTmpProfileId, "Local.BuildPath", tmpOutputFolder);
                aaProfileSettings.SetValue(aaTmpProfileId, "Local.LoadPath", loadPath);

                // build Addressables
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

                if (!string.IsNullOrEmpty(result.Error))
                    throw new Exception($"Failed to build the Addressables content. Error message:\n{result.Error}");

                // create the mod info file
                ModInfo info = new ()
                {
                    AppId = config.appId,
                    AppVersion = config.appVersion,
                    ModId = config.modId,
                    ModVersion = config.modVersion,
                    DisplayName = config.displayName,
                    Description = config.description,
                    Platform = runtimePlatform.ToString(),
                    DebugBuild = buildMode == CodeOptimization.Debug,
                    HasAssemblies = hasAssemblies
                };
                
                string infoJson = JsonConvert.SerializeObject(info, Formatting.Indented);
                string infoFilePath = Path.Combine(tmpOutputFolder, ModService.InfoFile);
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                // compress all mod contents into the final mod file
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                
                ZipFile.CreateFromDirectory(tmpOutputFolder, outputPath, compressionLevel, true);
            }
            finally
            {
                // cleanup
                IOUtils.DeleteDirectory(tmpFolder);
                IOUtils.DeleteDirectory(tmpAssembliesFolder, true);
                aaSettings.RemoveGroup(aaModAssetsGroup);
                aaProfileSettings.RemoveProfile(aaTmpProfileId);
                aaSettings.activeProfileId = previousActiveAaProfileId;
                aaSettings.RemoveLabel(ModService.AssembliesLabel, false);
                AssetDatabase.Refresh();
            }
        }
        
        /// <summary>
        /// Tries to get a mod assembly builder for the given build target, based on the current configured mob assembly builder type and custom builders.
        /// </summary>
        public virtual bool TryGetModAssemblyBuilder(BuildTarget buildTarget, out IModAssemblyBuilder assemblyBuilder)
        {
            assemblyBuilder = null;
            
            switch (assemblyBuilderType)
            {
                case ModAssemblyBuilderType.Fast:
                    assemblyBuilder = new FastModAssemblyBuilder();
                    return true;
                
                case ModAssemblyBuilderType.PlatformSpecific:
                    // TODO: implement platform specific builders
                    return false;
                
                case ModAssemblyBuilderType.Custom:
                    foreach (var builder in customAssemblyBuilders)
                    {
                        if (!builder.SupportsBuildTarget(buildTarget))
                            continue;
                        
                        assemblyBuilder = builder;
                        return true;
                    }
                    
                    return false;
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Copies the assembly at path into the destination, copying also the pdb files if doing a debug build and changing the extensions to .bytes so
        /// the files can be included in the Addressables build.
        /// </summary>
        protected virtual void CopyAssemblyForAddressablesBuild (string path, string destination, CodeOptimization buildMode)
        {
            if (string.IsNullOrEmpty(destination) || !Directory.Exists(destination))
                throw new Exception("Cannot copy the assembly with a null or non-existent destination folder");
            
            // the destination paths use the .bytes format so they are recognised as TextAsset
            // this will allow us to include the files on the Addressable Assets bundle
            string name = Path.GetFileNameWithoutExtension(path);
            string dllSrcPath = path;
            string dllDestPath = Path.Combine(destination, name) + ".dll.bytes";
            string pdbSrcPath = Path.ChangeExtension(path, ".pdb");
            string pdbDestPath = Path.Combine(destination, name) + ".pdb.bytes";

            // check if the assembly exists
            if (!File.Exists(dllSrcPath))
            {
                if (ignoreMissingAssemblies)
                    return;
                
                throw new FileNotFoundException($"Could not find the assembly file at \"{dllSrcPath}\"");
            }

            // check if the assembly is a valid net managed assembly
            if (!IsNetManagedAssembly(dllSrcPath))
                throw new Exception($"\"{dllSrcPath}\" is not a managed assembly");
            
            File.Copy(dllSrcPath, dllDestPath, true);

            // copy pdb file only if in debug mode. do nothing if the pdb is not found
            if (buildMode == CodeOptimization.Debug && File.Exists(pdbSrcPath))
                File.Copy(pdbSrcPath, pdbDestPath, true);
        }

        /// <summary>
        /// Checks if the given filePath corresponds to a valid managed NET assembly.
        /// </summary>
        public static bool IsNetManagedAssembly (string filePath)
        {
            try
            {
                var testAssembly = AssemblyName.GetAssemblyName(filePath);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Tries to get the equivalent RuntimePlatform value from the given BuildTarget. Returns true if succeeded.
        /// </summary>
        public static bool TryGetRuntimePlatformFromBuildTarget(BuildTarget buildTarget, out RuntimePlatform runtimePlatform)
        {
            RuntimePlatform? platform = buildTarget switch
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

            if (platform.HasValue)
            {
                runtimePlatform = platform.Value;
                return true;
            }
            
            runtimePlatform = default;
            return false;
        }
    }
}
