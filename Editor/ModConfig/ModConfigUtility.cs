using System;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    public static class ModConfigUtility
    {
        private const string BuildModeKey = "buildmode";
        private const string OutputPathKey = "outputpath";
        private const string DevBuildModeKey = "dev_buildmode";
        private const string DevOutputFolderKey = "dev_outputfolder";
        
        /// <summary>
        /// Builds the mod.
        /// </summary>
        public static UniTask BuildAsync (this ModConfig config, CodeOptimization buildMode, string outputPath)
            => BuildInternalAsync(config, buildMode, outputPath, false, false, false);
        
        /// <summary>
        /// Builds the mod for development (skips archiving process and allows to skip assets/scripts).
        /// </summary>
        public static UniTask BuildForDevelopmentAsync (this ModConfig config, CodeOptimization buildMode, string outputFolder,
            bool skipAssemblies = false, bool skipAssets = false)
            => BuildInternalAsync(config, buildMode, outputFolder, true, skipAssemblies, skipAssets);
        
        /// <summary>
        /// Builds the mod. The user will be asked for any unset parameters. If defaultToCachedParameters is set to true, the parameters
        /// will be automatically fetched from the cached values rather than asking the user. Cached values are stored after any successful build.
        /// </summary>
        public static UniTask BuildWithGuiAsync (this ModConfig config, CodeOptimization? buildMode = null, string outputPath = null,
            bool defaultToCachedParameters = false)
            => BuildWithGuiInternalAsync(config, buildMode, outputPath, defaultToCachedParameters, false, false, false);
        
        /// <summary>
        /// Builds the mod for development (skips archiving process and allows to skip assets/scripts). The user will be asked for any
        /// unset parameters. If defaultToCachedParameters is set to true, the parameters will be automatically fetched from the cached
        /// values rather than asking the user. Cached values are stored after any successful build.
        /// </summary>
        public static UniTask BuildWithGuiForDevelopmentAsync (this ModConfig config, bool skipAssemblies = false, bool skipAssets = false,
            CodeOptimization? buildMode = null, string outputFolder = null, bool defaultToCachedParameters = false)
            => BuildWithGuiInternalAsync(config, buildMode, outputFolder, defaultToCachedParameters, true, skipAssemblies, skipAssets);
        
        public static string GetDefaultFileOutputName(this ModConfig config, CodeOptimization buildMode)
        {
            if (config.ContainsAssets && UniModEditorUtility.TryGetRuntimePlatformFromBuildTarget(EditorUserBuildSettings.activeBuildTarget, out RuntimePlatform runtimePlatform))
                return $"{config.modId}-{config.modVersion}-{runtimePlatform}-{buildMode}{UniModRuntime.ModFileExtension}";
            else
                return $"{config.modId}-{config.modVersion}-{buildMode}{UniModRuntime.ModFileExtension}";
        }
        
        public static CodeOptimization? GetCachedBuildMode(this ModConfig config, bool developmentBuild = false)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, developmentBuild ? DevBuildModeKey : BuildModeKey);
            string buildTargetValue = PlayerPrefs.GetString(uniqueKey, null);
            
            if (string.IsNullOrEmpty(buildTargetValue) || !Enum.TryParse(buildTargetValue, out CodeOptimization value))
                return null;
            
            return value;
        }

        public static void SetCachedBuildMode(this ModConfig config, CodeOptimization? buildMode, bool developmentBuild = false)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, developmentBuild ? DevBuildModeKey : BuildModeKey);
            PlayerPrefs.SetString(uniqueKey, buildMode?.ToString());
        }

        public static string GetCachedBuildOutputPath(this ModConfig config, bool developmentBuild = false)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, developmentBuild ? DevOutputFolderKey : OutputPathKey);
            return PlayerPrefs.GetString(uniqueKey, null);
        }

        public static void SetCachedBuildOutputPath(this ModConfig config, string outputPath, bool developmentBuild = false)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, developmentBuild ? DevOutputFolderKey : OutputPathKey);
            PlayerPrefs.SetString(uniqueKey, outputPath);
        }
        
        private static async UniTask BuildInternalAsync (this ModConfig config, CodeOptimization buildMode, string outputPath,
            bool developmentBuild, bool skipAssemblies, bool skipAssets)
        {
            CheckConfig(config);
            if (config.builder is null)
                throw new Exception("No mod builder is defined in this config");
            
            try
            {
                if (developmentBuild)
                {
                    await config.builder.BuildForDevelopmentAsync(config, buildMode, outputPath, skipAssemblies, skipAssets);
                    Debug.Log($"Mod built successfully!\nDevelopment build output: {outputPath}");
                }
                else
                {
                    await config.builder.BuildAsync(config, buildMode, outputPath);
                    Debug.Log($"Mod built successfully!\nOutput path: {outputPath}");
                }
                
                // cache build parameters for next Gui builds
                SetCachedBuildOutputPath(config, outputPath, developmentBuild);
                SetCachedBuildMode(config, buildMode, developmentBuild);
            }
            catch (Exception exception)
            {
                throw new Exception($"Mod build failed:\nID: {config.modId}\nVersion: {config.modVersion}\n\n{exception}");
            }
        }

        private static async UniTask BuildWithGuiInternalAsync (this ModConfig config, CodeOptimization? buildMode, string outputPath,
            bool defaultToCachedParameters, bool developmentBuild, bool skipAssemblies, bool skipAssets)
        {
            if (defaultToCachedParameters)
            {
                buildMode ??= GetCachedBuildMode(config, developmentBuild);
                outputPath ??= GetCachedBuildOutputPath(config, developmentBuild);
            }
            
            // ask the user for the parameters that were not specified
            buildMode ??= DisplayBuildModeDialog();
            if (buildMode == CodeOptimization.None)
                return;
            
            string defaultOutputPath = GetDefaultFileOutputName(config, buildMode.Value);
            outputPath ??= DisplayModBuildOutputPathDialog(defaultOutputPath, buildMode.Value, developmentBuild);
            if (string.IsNullOrEmpty(outputPath))
                return;
            
            await BuildInternalAsync(config, buildMode.Value, outputPath, developmentBuild, skipAssemblies, skipAssets);
        }

        private static void CheckConfig(ModConfig config)
        {
            if (!config)
                throw new Exception("The mod config is null or has been destroyed");
        }
        
        private static CodeOptimization DisplayBuildModeDialog()
        {
            int option = EditorUtility.DisplayDialogComplex("Build mode", "Select a build mode", "Release", "Cancel", "Debug");

            return option switch
            {
                0 => CodeOptimization.Release,
                2 => CodeOptimization.Debug,
                _ => CodeOptimization.None
            };
        }
        
        private static string DisplayModBuildOutputPathDialog(string defaultOutputPath, CodeOptimization buildMode, bool developmentBuild)
        {
            if (buildMode == CodeOptimization.None)
                return null;

            if (developmentBuild)
            {
                string outputFolder = EditorUtility.SaveFolderPanel("Build mod for development...", null, null);
                return outputFolder;
            }
            
            string outputPath = EditorUtility.SaveFilePanel("Build mod...", null, defaultOutputPath, UniModRuntime.ModFileExtensionNoDot);
            return outputPath;
        }
    }
}
