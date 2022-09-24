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
        
        private readonly ModContentBuilder _contentBuilder = new();
        
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
                // build mod and create the output archive file
                BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                await BuildContentAndAssembliesAsync(config, buildMode, buildTarget, tmpBuildFolder);
                await CreateModFileFromBuildAsync(config, tmpBuildFolder, buildTarget, outputPath);
            }
            finally
            {
                // cleanup
                IOUtils.DeleteDirectory(tmpFolder);
            }
        }

        private async UniTask BuildContentAndAssembliesAsync(ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            // check if the mod config includes any assemblies
            List<string> includedAssemblyNames = config.GetAllIncludes(buildTarget);
            bool hasAssemblies = includedAssemblyNames.Count > 0;
            
            if (!hasAssemblies)
            {
                if (config.assembliesOnly)
                    throw new Exception("The mod is configured as an assemblies only mod but it has no included assemblies for the current build target");
                
                // if mod doesnt have assemblies then we just build the content
                BuildContent(config.modId, outputFolder);
                return;
            }
            
            // try to get an assembly builder for the target platform
            if (!TryGetModAssemblyBuilder(buildTarget, out IModAssemblyBuilder assemblyBuilder))
                throw new Exception($"Could not find a mod assembly builder that supports the current build target: {buildTarget}");
            
            // build the assemblies
            string assembliesOutputFolder = Path.Combine(outputFolder, UniModConstants.AssembliesFolder);
            Directory.CreateDirectory(assembliesOutputFolder);
            await assemblyBuilder.BuildAssembliesAsync(config, buildMode, buildTarget, assembliesOutputFolder);

            // also build the content if proceeds
            if (!config.assembliesOnly)
            {
                // include the mod's startup script (its ok if the startup script is null)
                _contentBuilder.AddAsset(config.startup, UniModConstants.StartupAddress);
                BuildContent(config.modId, outputFolder);
            }
        }

        private async UniTask CreateModFileFromBuildAsync(ModConfig config, string buildFolder, BuildTarget buildTarget, string outputPath)
        {
            // get the mod's supported platform
            string platform;
            
            if (config.assembliesOnly)
                platform = UniModConstants.AssembliesOnlyPlatform;
            else
            {
                // try to get the build target's equivalent runtime platform value
                if (!ModBuildingUtility.TryGetRuntimePlatformFromBuildTarget(buildTarget, out RuntimePlatform runtimePlatform))
                    throw new Exception($"Couldn't get the equivalent runtime platform value for the current active build target: {buildTarget}");
                
                platform = runtimePlatform.ToString();
            }
            
            // make sure the output path has the proper mod extension
            outputPath = IOUtils.EnsureFileExtension(outputPath, UniModConstants.ModFileExtensionNoDot);

            // create the mod info file
            ModInfo info = new ()
            {
                AppId = config.appId,
                AppVersion = config.appVersion,
                ModId = config.modId,
                ModVersion = config.modVersion,
                DisplayName = config.displayName,
                Description = config.description,
                Platform = platform,
            };
            
            string infoJson = JsonConvert.SerializeObject(info, Formatting.Indented);
            string infoFilePath = Path.Combine(buildFolder, UniModConstants.InfoFile);
            await File.WriteAllTextAsync(infoFilePath, infoJson);
            
            // overwrite the existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            // compress all mod contents into the final mod file
            ZipFile.CreateFromDirectory(buildFolder, outputPath, compressionLevel, true);
        }
        
        private void BuildContent(string modId, string outputFolder)
        {
            AddressablesPlayerBuildResult result = _contentBuilder.BuildContent(modId, outputFolder);
            
            if (!string.IsNullOrEmpty(result.Error))
                throw new Exception($"Failed to build Addressables content.\nError: {result.Error}");
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
