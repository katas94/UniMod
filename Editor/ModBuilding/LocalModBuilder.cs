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
        Final,
        Fast,
        Custom
    }
    
    /// <summary>
    /// Mod builder implementation that builds mods that can be loaded by <see cref="LocalModLoader"/>. You can extend this implementation
    /// to override any building step individually or add extra pre and post building steps (i.e.: including extra files in the build folder
    /// so they are archived in the final mod file).
    /// </summary>
    [CreateAssetMenu(fileName = "LocalModBuilder", menuName = "UniMod/Local Mod Builder")]
    public class LocalModBuilder : ModBuilder
    {
        private const string StartupGroupName = "ModStartup";
        
        [Tooltip(CompressionLevelTooltip)]
        public CompressionLevel compressionLevel = CompressionLevel.Optimal;
        [Tooltip(AssemblyBuilderTypeTooltip)]
        public ModAssemblyBuilderType assemblyBuilderType = ModAssemblyBuilderType.Final;
        [Tooltip(CustomAssemblyBuildersTooltip)]
        public List<CustomAssemblyBuilder> customAssemblyBuilders;
        
        public override UniTask BuildAsync (ModConfig config, CodeOptimization buildMode, string outputPath)
            => BuildInternalAsync(config, buildMode, outputPath, false, false, false);
        
        public override UniTask BuildForDevelopmentAsync (ModConfig config, CodeOptimization buildMode, string outputFolder,
            bool skipAssemblies = false, bool skipAssets = false)
            => BuildInternalAsync(config, buildMode, outputFolder, true, skipAssemblies, skipAssets);
        
        protected virtual async UniTask BuildInternalAsync (ModConfig config, CodeOptimization buildMode, string outputPath,
            bool developmentBuild, bool skipAssemblies, bool skipAssets)
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
                BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;
                
                await OnPreBuildAsync(config, buildMode, buildTarget, tmpBuildFolder);
                
                // export the mod thumbnail if any
                await ExportThumbnailAsync(config, tmpBuildFolder);
                
                // build the mod assemblies if there are any
                if (!developmentBuild || !skipAssemblies)
                    await BuildAssembliesAsync(config, buildMode, buildTarget, tmpBuildFolder);
                
                // build assets if there are any included (this will run an Addressables build)
                if (!developmentBuild || !skipAssets)
                    BuildAssets(config, tmpBuildFolder);
                
                await OnPostBuildAsync(config, buildMode, buildTarget, tmpBuildFolder);
                
                // create the output mod archive file from the build
                await CreateModFileFromBuildAsync(config, tmpBuildFolder, buildTarget, outputPath, skipArchiving: developmentBuild);
                
                if (!developmentBuild)
                    return;
                
                // if we are doing a development build then we have to create/update the mod folder in the output path with the build artifacts
                CreateOrUpdateDevelopmentOutputFolder(config, tmpBuildFolder, outputPath, skipAssemblies, skipAssets);
            }
            finally
            {
                // cleanup
                IOUtils.DeleteDirectory(tmpFolder);
            }
        }

        /// <summary>
        /// Invoked before building the mod. The tmpBuildFolder is already created.
        /// </summary>
        protected virtual UniTask OnPreBuildAsync(ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string tmpBuildFolder)
            => UniTask.CompletedTask;
        
        /// <summary>
        /// Invoked after building the mod but before archiving the tmpBuildFolder and deleting it.
        /// </summary>
        protected virtual UniTask OnPostBuildAsync(ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string tmpBuildFolder)
            => UniTask.CompletedTask;
        
        protected virtual async UniTask ExportThumbnailAsync(ModConfig config, string outputFolder)
        {
            if (!config.thumbnail)
                return;

            TextureImporter importer = null;

            try
            {
                // if the thumbnail texture is non-readable we will try to make it readable temporarily for the export
                if (!config.thumbnail.isReadable)
                {
                    string assetPath = AssetDatabase.GetAssetPath(config.thumbnail);
                    importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer is null)
                        throw new Exception("The thumbnail texture is non-readable");

                    importer.isReadable = true;
                    importer.SaveAndReimport();
                }

                string path = Path.Combine(outputFolder, UniModRuntime.ThumbnailFile);
                byte[] bytes = config.thumbnail.EncodeToPNG();
                await File.WriteAllBytesAsync(path, bytes);
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to export the mod thumbnail", exception);
            }
            finally
            {
                if (importer is not null)
                {
                    // set the texture back to non-readable if it was before
                    importer.isReadable = false;
                    importer.SaveAndReimport();
                }
            }
        }

        protected virtual async UniTask BuildAssembliesAsync(ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            // resolve all the included assemblies in the config that are compatible with the build target
            List<string> assemblyNames = AssemblyDefinitionIncludesUtility.ResolveIncludedSupportedAssemblyNames(config.assemblyDefinitions, buildTarget);
            List<string> managedPluginPaths = ManagedPluginIncludesUtility.ResolveIncludedSupportedManagedPluginPaths(config.managedPlugins, buildTarget);
            
            // return if there are no assemblies included
            if (assemblyNames.Count == 0 && managedPluginPaths.Count == 0)
                return;

            string assembliesOutputFolder = Path.Combine(outputFolder, UniModRuntime.AssembliesFolder);
            Directory.CreateDirectory(assembliesOutputFolder);
            
            // build user defined assemblies
            if (assemblyNames.Count > 0)
            {
                if (!TryGetModAssemblyBuilder(buildTarget, out IAssemblyBuilder assemblyBuilder))
                    throw new Exception($"Could not find a mod assembly builder that supports the current build target: {buildTarget}");
                
                await assemblyBuilder.BuildAssembliesAsync(assemblyNames, buildMode, buildTarget, assembliesOutputFolder);
            }
            
            // copy managed plugins to the output folder
            await CopyManagedPlugins(managedPluginPaths, assembliesOutputFolder, buildMode);
        }

        protected virtual UniTask CopyManagedPlugins(IEnumerable<string> managedPluginPaths, string outputFolder, CodeOptimization buildMode)
        {
            bool isDebugBuild = buildMode is CodeOptimization.Debug;
            return UniTaskUtility.WhenAll(managedPluginPaths.Select(
                path => UniModEditorUtility.CopyManagedAssemblyAsync(path, outputFolder, isDebugBuild)
            ));
        }
        
        protected virtual void BuildAssets(ModConfig config, string outputFolder)
        {
            if (!config.ContainsAssets)
                return;
            
            AddressablesBuilder addressablesBuilder = null;

            try
            {
                addressablesBuilder = new AddressablesBuilder();
                
                // add the startup script for the assets build if any
                if (config.startup)
                {
                    AddressablesBuilder.IGroupBuilder groupBuilder = addressablesBuilder.CreateGroup(StartupGroupName);
                    groupBuilder.CreateEntry(config.startup, UniModRuntime.StartupAddress);
                }
                
                // add all the config addressable groups to the build
                addressablesBuilder.AddGroups(config.addressableGroups);
                
                // get the load path and perform the build
                string buildPath = Path.Combine(outputFolder, UniModRuntime.AssetsFolder);
                string loadPath = UniModRuntime.GetAddressablesLoadPathForMod(config.modId);
                AddressablesPlayerBuildResult result = addressablesBuilder.Build(buildPath, loadPath);
                
                if (!string.IsNullOrEmpty(result.Error))
                    throw new Exception($"Failed to build mod assets.\nError: {result.Error}");
            }
            finally
            {
                addressablesBuilder?.Dispose();
            }
        }

        protected virtual async UniTask CreateModFileFromBuildAsync(ModConfig config, string buildFolder, BuildTarget buildTarget, string outputPath, bool skipArchiving = false)
        {
            // create the mod info struct
            ModInfo info = new ()
            {
                Id = config.modId,
                Version = config.modVersion,
                DisplayName = config.displayName,
                Description = config.description,
                Dependencies = UniModUtility.CreateDictionaryFromModReferences(config.dependencies),
                Target = UniModEditorUtility.CreateModTargetInfo(config, buildTarget)
            };
            
            // write the info file
            string infoJson = JsonConvert.SerializeObject(info, Formatting.Indented);
            string infoFilePath = Path.Combine(buildFolder, UniModRuntime.InfoFile);
            await File.WriteAllTextAsync(infoFilePath, infoJson);
            
            if (skipArchiving)
                return;
            
            // make sure the output path has the proper mod extension
            outputPath = IOUtils.EnsureFileExtension(outputPath, UniModRuntime.ModFileExtensionNoDot);
            
            // overwrite the existing file
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            // compress all mod contents into the final mod file
            ZipFile.CreateFromDirectory(buildFolder, outputPath, compressionLevel, true);
        }
        
        protected virtual void CreateOrUpdateDevelopmentOutputFolder(ModConfig config, string buildFolder, string outputPath,
            bool skipAssemblies, bool skipAssets)
        {
            outputPath = Path.Combine(outputPath, config.modId);

            // fully delete previous assemblies and assets folder if we rebuilt them
            string outputAssembliesPath = Path.Combine(outputPath, UniModRuntime.AssembliesFolder);
            string outputAssetsPath = Path.Combine(outputPath, UniModRuntime.AssetsFolder);
            if (!skipAssemblies && Directory.Exists(outputAssembliesPath))
                IOUtils.DeleteDirectory(outputAssembliesPath);
            if (!skipAssets && Directory.Exists(outputAssetsPath))
                IOUtils.DeleteDirectory(outputAssetsPath);

            IOUtils.CopyDirectory(buildFolder, outputPath, overwriteExistingFiles: true);
        }

        // tries to get a mod assembly builder for the given build target, based on the current configured mob assembly builder type and custom builders.
        protected virtual bool TryGetModAssemblyBuilder(BuildTarget buildTarget, out IAssemblyBuilder assemblyBuilder)
        {
            switch (assemblyBuilderType)
            {
                case ModAssemblyBuilderType.Final:
                    return TryGetFinalModAssemblyBuilder(buildTarget, out assemblyBuilder);
                
                case ModAssemblyBuilderType.Fast:
                    assemblyBuilder = FastAssemblyBuilder.Instance;
                    return true;
                
                case ModAssemblyBuilderType.Custom:
                    // try to find a custom builder that supports the given build target
                    foreach (CustomAssemblyBuilder builder in customAssemblyBuilders)
                    {
                        if (!builder.SupportsBuildTarget(buildTarget))
                            continue;
                        
                        assemblyBuilder = builder;
                        return true;
                    }
                    
                    assemblyBuilder = null;
                    return false;
                
                default:
                    assemblyBuilder = null;
                    return false;
            }
        }
        
        // override this if you have extended the FinalAssemblyBuilder and don't want to create an assembly builder asset
        protected virtual bool TryGetFinalModAssemblyBuilder(BuildTarget buildTarget, out IAssemblyBuilder assemblyBuilder)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneLinux64:
                case BuildTarget.StandaloneOSX:
                    assemblyBuilder = StandaloneAssemblyBuilder.Instance;
                    return true;
                case BuildTarget.Android:
                    assemblyBuilder = AndroidAssemblyBuilder.Instance;
                    return true;
                
                default:
                    assemblyBuilder = null;
                    return false;
            }
        }
        
#region TOOLTIPS
        private const string CompressionLevelTooltip = "The compression level to use for the output mod file";
        private const string AssemblyBuilderTypeTooltip = "Specify how to build the script assemblies from the project.\n\n\tFast: skips building the scripts by using the Editor's precompiled assemblies (be aware that they are compiled with the Editor's defines).\n\n\tFinal: performs an scripts only build of the project to produce assemblies with the right defines for the current active platform. Use this option for release builds.\n\n\tCustom: looks on the configured custom assembly builders for any builder compatible with the current platform an uses it.";
        private const string CustomAssemblyBuildersTooltip = "Define here any custom assembly builders that will be used when \"Custom\" is set in the Assembly Builder Type field";
#endregion
    }
}
