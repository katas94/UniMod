using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor.AddressableAssets.Build;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Katas.UniMod.Editor
{
    public enum ModAssemblyBuilderType
    {
        Fast,
        PlatformSpecific,
        Custom
    }
    
    /// <summary>
    /// Builds a mod that can be loaded by the LocalMod implementation.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalModBuilder", menuName = "UniMod/Local Mod Builder")]
    public sealed class LocalModBuilder : ModBuilder
    {
        public CompressionLevel compressionLevel = CompressionLevel.Optimal;
        public ModAssemblyBuilderType assemblyBuilderType = ModAssemblyBuilderType.PlatformSpecific;
        public List<CustomModAssemblyBuilder> customAssemblyBuilders;
        
        private readonly ModAssetsBuilder _assetsBuilder = new();
        
        /// <summary>
        /// Builds the mod with the specified parameters.
        /// </summary>
        public override async UniTask BuildAsync (ModConfig config, CodeOptimization buildMode, string outputPath)
        {
            // validate parameters
            if (config is null)
                throw new NullReferenceException("The given mod configuration is null");
            if (buildMode == CodeOptimization.None)
                throw new Exception("The specified build mode is None. Please specify a valid build mode (Release or Debug)");
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("The given build output path is null or empty");
            
            // create the temporary output folder for the mod build
            string tmpFolder = IOUtils.CreateTmpFolder();
            string tmpBuildFolder = Path.Combine(tmpFolder, config.modId);
            Directory.CreateDirectory(tmpBuildFolder);
            
            try
            {
                // get current active build target and build the assemblies (if there are not included assemblies then nothing will be built)
                BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                await BuildAssembliesAsync(config, buildMode, buildTarget, tmpBuildFolder);
                
                // build assets if enabled (this will run an Addressables build)
                if (config.buildAssets)
                    BuildAssets(config, tmpBuildFolder);
                
                // create the output mod archive file with all the built contents on it
                await CreateModFileFromBuildAsync(config, tmpBuildFolder, buildTarget, outputPath);
            }
            finally
            {
                // cleanup
                IOUtils.DeleteDirectory(tmpFolder);
            }
        }

        private async UniTask BuildAssembliesAsync(ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            // resolve all the included assemblies in the config that are compatible with the build target
            List<string> assemblyNames = AssemblyDefinitionIncludesUtility.ResolveIncludedSupportedAssemblyNames(config.assemblyDefinitions, buildTarget);
            List<string> managedPluginPaths = ManagedPluginIncludesUtility.ResolveIncludedSupportedManagedPluginPaths(config.managedPlugins, buildTarget);
            
            // return if there are no assemblies included
            if (assemblyNames.Count == 0 && managedPluginPaths.Count == 0)
                return;
            
            // try to get an assembly builder for the target platform
            if (!TryGetModAssemblyBuilder(buildTarget, out IModAssemblyBuilder assemblyBuilder))
                throw new Exception($"Could not find a mod assembly builder that supports the current build target: {buildTarget}");
            
            // build the assemblies
            string assembliesOutputFolder = Path.Combine(outputFolder, UniMod.AssembliesFolder);
            Directory.CreateDirectory(assembliesOutputFolder);
            await assemblyBuilder.BuildAssembliesAsync(assemblyNames, managedPluginPaths, buildMode, buildTarget, assembliesOutputFolder);
        }
        
        private void BuildAssets(ModConfig config, string outputFolder)
        {
            if (config.startup)
                _assetsBuilder.AddAsset(config.startup, UniMod.StartupAddress);
            
            AddressablesPlayerBuildResult result = _assetsBuilder.Build(config.modId, outputFolder);
            
            if (!string.IsNullOrEmpty(result.Error))
                throw new Exception($"Failed to build mod assets.\nError: {result.Error}");
        }

        private async UniTask CreateModFileFromBuildAsync(ModConfig config, string buildFolder, BuildTarget buildTarget, string outputPath)
        {
            // get the mod's supported platform
            string platform;
            
            // currently we are only supporting managed assemblies so a mod that only contains assemblies will support all platforms
            if (!config.buildAssets)
                platform = UniMod.AnyPlatform;
            else
            {
                // try to get the build target's equivalent runtime platform value
                if (!UniModEditorUtility.TryGetRuntimePlatformFromBuildTarget(buildTarget, out RuntimePlatform runtimePlatform))
                    throw new Exception($"Couldn't get the equivalent runtime platform value for the current active build target: {buildTarget}");
                
                platform = runtimePlatform.ToString();
            }
            
            // make sure the output path has the proper mod extension
            outputPath = IOUtils.EnsureFileExtension(outputPath, UniMod.ModFileExtensionNoDot);
            
            // create the mod info file
            ModInfo info = new ()
            {
                Target = new ModTargetInfo()
                {
                    UnityVersion = Application.unityVersion,
                    UniModVersion = UniMod.Version,
                    Platform = platform,
                    AppId = string.IsNullOrEmpty(config.appId) ? null : config.appId,
                    AppVersion = string.IsNullOrEmpty(config.appVersion) ? null : config.appVersion,
                },
                Id = config.modId,
                Version = config.modVersion,
                DisplayName = config.displayName,
                Description = config.description,
            };
            
            string infoJson = JsonConvert.SerializeObject(info, Formatting.Indented);
            string infoFilePath = Path.Combine(buildFolder, UniMod.InfoFile);
            await File.WriteAllTextAsync(infoFilePath, infoJson);
            
            // overwrite the existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            // compress all mod contents into the final mod file
            ZipFile.CreateFromDirectory(buildFolder, outputPath, compressionLevel, true);
        }
        
        // tries to get a mod assembly builder for the given build target, based on the current configured mob assembly builder type and custom builders.
        private bool TryGetModAssemblyBuilder(BuildTarget buildTarget, out IModAssemblyBuilder assemblyBuilder)
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
                    foreach (CustomModAssemblyBuilder builder in customAssemblyBuilders)
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
    }
}
