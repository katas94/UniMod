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
using Katas.Modman;

using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Katas.ModmanEditor
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
            outputPath = IOUtils.EnsureFileExtension(outputPath, ModErator.ModFileExtensionNoDot);
            
            // initialize Addressables if it was not before
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);

            // create a new Addressable Assets group for the assemblies and startup assets (we will clean it after the build)
            var aaSettings = AddressableAssetSettingsDefaultObject.Settings;
            var aaProfileSettings = aaSettings.profileSettings;
            string previousActiveAaProfileId = aaSettings.activeProfileId;
            string aaTmpProfileId = null;
            string tmpFolder = null;
            string assembliesOutputFolder = null;
            string startupGui = null;
            
            try
            {
                // create the temporary output folder for the Addressables build
                tmpFolder = IOUtils.CreateTmpFolder();
                string tmpOutputFolder = Path.Combine(tmpFolder, config.modId);
                Directory.CreateDirectory(tmpOutputFolder);

                // if the mod has assemblies, build them and include them on the Addressables build
                if (hasAssemblies)
                {
                    // create the assemblies output folder and build the assemblies
                    assembliesOutputFolder = Path.Combine(tmpOutputFolder, RuntimeMod.AssembliesFolder);
                    Directory.CreateDirectory(assembliesOutputFolder);
                    await assemblyBuilder.BuildAssembliesAsync(config, buildMode, buildTarget, assembliesOutputFolder);
                    
                    // if the mod includes a startup script, include it in the Addressables build
                    if (config.startup)
                    {
                        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(config.startup, out startupGui, out long _))
                            throw new Exception($"Could not get the asset GUID for the mod startup instance.");
                        
                        var entry = aaSettings.CreateOrMoveEntry(startupGui, aaSettings.DefaultGroup, true, false);
                        entry.address = RuntimeMod.StartupAddress;
                    }
                }

                // prepare the Addressable Assets settings for the build
                aaSettings.OverridePlayerVersion = RuntimeMod.CatalogName;
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
                string infoFilePath = Path.Combine(tmpOutputFolder, ModErator.InfoFile);
                await File.WriteAllTextAsync(infoFilePath, infoJson);

                // compress all mod contents into the final mod file
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                
                ZipFile.CreateFromDirectory(tmpOutputFolder, outputPath, compressionLevel, true);
            }
            finally
            {
                // cleanup
                if (startupGui is not null)
                    aaSettings.RemoveAssetEntry(startupGui);
                
                IOUtils.DeleteDirectory(tmpFolder);
                IOUtils.DeleteDirectory(assembliesOutputFolder, true);
                aaProfileSettings.RemoveProfile(aaTmpProfileId);
                aaSettings.activeProfileId = previousActiveAaProfileId;
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
        /// Checks if the given filePath corresponds to a valid managed assembly.
        /// </summary>
        public static bool IsManagedAssembly (string filePath)
        {
            try
            {
                _ = AssemblyName.GetAssemblyName(filePath);
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
