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

        /// <summary>
        /// Builds the mod.
        /// </summary>
        public static async UniTask BuildAsync (this ModConfig config, CodeOptimization buildMode, string outputPath)
        {
            CheckConfig(config);
            if (config.builder is null)
                throw new Exception("No mod builder is defined in this config");
            
            try
            {
                await config.builder.BuildAsync(config, buildMode, outputPath);
                Debug.Log($"Mod built successfully!\nOutput path: {outputPath}");
                
                // cache build parameters for next Gui builds
                SetCachedBuildOutputPath(config, outputPath);
                SetCachedBuildMode(config, buildMode);
            }
            catch (Exception exception)
            {
                throw new Exception($"Mod build failed:\nID: {config.modId}\nVersion: {config.modVersion}\n\n{exception}");
            }
        }

        /// <summary>
        /// Builds the mod. The user will be asked for any unset parameters. If defaultToCachedParameters is set to true, the parameters
        /// will be automatically fetched from the cached values rather than asking the user. Cached values are stored after any successful build.
        /// </summary>
        public static async UniTask BuildWithGuiAsync (this ModConfig config, CodeOptimization? buildMode = null, string outputPath = null, bool defaultToCachedParameters = false)
        {
            if (defaultToCachedParameters)
            {
                buildMode ??= GetCachedBuildMode(config);
                outputPath ??= GetCachedBuildOutputPath(config);
            }
            
            // ask the user for the parameters that were not specified
            buildMode ??= DisplayBuildModeDialog();
            if (buildMode == CodeOptimization.None)
                return;
            
            string defaultOutputPath = GetDefaultFileOutputName(config, buildMode.Value);
            outputPath ??= DisplayModBuildOutputPathDialog(defaultOutputPath, buildMode.Value);
            if (string.IsNullOrEmpty(outputPath))
                return;
            
            await BuildAsync(config, buildMode.Value, outputPath);
        }
        
        public static string GetDefaultFileOutputName(this ModConfig config, CodeOptimization buildMode)
        {
            if (config.type is not ModType.Assemblies && UniModEditorUtility.TryGetRuntimePlatformFromBuildTarget(EditorUserBuildSettings.activeBuildTarget, out RuntimePlatform runtimePlatform))
                return $"{config.modId}-{config.modVersion}-{runtimePlatform}-{buildMode}{UniMod.ModFileExtension}";
            else
                return $"{config.modId}-{config.modVersion}-{buildMode}{UniMod.ModFileExtension}";
        }
        
        public static CodeOptimization? GetCachedBuildMode(this ModConfig config)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, BuildModeKey);
            string buildTargetValue = PlayerPrefs.GetString(uniqueKey, null);
            
            if (string.IsNullOrEmpty(buildTargetValue) || !Enum.TryParse(buildTargetValue, out CodeOptimization value))
                return null;
            
            return value;
        }

        public static void SetCachedBuildMode(this ModConfig config, CodeOptimization? buildMode)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, BuildModeKey);
            PlayerPrefs.SetString(uniqueKey, buildMode?.ToString());
        }

        public static string GetCachedBuildOutputPath(this ModConfig config)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, OutputPathKey);
            return PlayerPrefs.GetString(uniqueKey, null);
        }

        public static void SetCachedBuildOutputPath(this ModConfig config, string outputPath)
        {
            CheckConfig(config);
            string uniqueKey = UniModEditorUtility.GetUniqueKeyForAsset(config, OutputPathKey);
            PlayerPrefs.SetString(uniqueKey, outputPath);
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
        
        private static string DisplayModBuildOutputPathDialog(string defaultOutputPath, CodeOptimization buildMode)
        {
            if (buildMode == CodeOptimization.None)
                return null;
            
            string outputPath = EditorUtility.SaveFilePanel("Build mod...", null, defaultOutputPath, UniMod.ModFileExtensionNoDot);
            return outputPath;
        }
    }
}
