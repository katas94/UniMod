using System;
using Cysharp.Threading.Tasks;
using Modman;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ModmanEditor
{
    public partial class ModConfig
    {
        public CodeOptimization? CachedBuildMode
        {
            get
            {
                string buildTargetValue = EditorPrefs.GetString(_buildTargetKey, null);
                
                if (string.IsNullOrEmpty(buildTargetValue) || !Enum.TryParse(buildTargetValue, out CodeOptimization value))
                    return null;
                
                return value;
            }

            set => EditorPrefs.SetString(_buildTargetKey, value.ToString());
        }

        public string CachedBuildOutputPath
        {
            get => EditorPrefs.GetString(_outputPathKey, null);
            set => EditorPrefs.SetString(_outputPathKey, value);
        }

        private string _buildTargetKey;
        private string _outputPathKey;
        private bool _cacheKeysRefreshed;

        /// <summary>
        /// Builds the mod.
        /// </summary>
        public async UniTask BuildModAsync (CodeOptimization buildMode, string outputPath)
        {
            if (builder is null)
                throw new Exception("No mod builder is defined in this config");
            
            try
            {
                await builder.BuildAsync(this, buildMode, outputPath);
                Debug.Log($"Mod built successfully!\nOutput path: {outputPath}");
                
                // cache build parameters for next Gui builds
                RefreshCacheKeys();
                CachedBuildOutputPath = outputPath;
                CachedBuildMode = buildMode;
            }
            catch (Exception exception)
            {
                throw new Exception($"Mod build failed:\nID: {modId}\nVersion: {modVersion}\n\n{exception}");
            }
        }

        /// <summary>
        /// Builds the mod. The user will be asked for any unset parameters. If tryRebuild is set to true, the parameters
        /// will be automatically fetched from the cached values rather than asking the user. Cached values are set from a previous build with Gui.
        /// </summary>
        public async UniTask BuildModWithGuiAsync (CodeOptimization? buildMode = null, string outputPath = null, bool tryRebuild = false)
        {
            if (tryRebuild)
            {
                RefreshCacheKeys();
                buildMode ??= CachedBuildMode;
                outputPath ??= CachedBuildOutputPath;
            }
            
            // ask the user for the parameters that were not specified
            buildMode ??= DisplayBuildModeDialog();
            if (buildMode == CodeOptimization.None)
                return;
            
            outputPath ??= DisplayModBuildOutputPathDialog(buildMode.Value);
            if (string.IsNullOrEmpty(outputPath))
                return;
            
            await BuildModAsync(buildMode.Value, outputPath);
        }
        
        private CodeOptimization DisplayBuildModeDialog()
        {
            int option = EditorUtility.DisplayDialogComplex("Build mode", "Select a build mode", "Release", "Cancel", "Debug");

            return option switch
            {
                0 => CodeOptimization.Release,
                2 => CodeOptimization.Debug,
                _ => CodeOptimization.None
            };
        }
        
        private string DisplayModBuildOutputPathDialog(CodeOptimization buildMode)
        {
            if (buildMode == CodeOptimization.None)
                return null;
            
            string defaultName = $"{modId}-{modVersion}-{buildMode}{ModService.ModFileExtension}";
            string outputPath = EditorUtility.SaveFilePanel("Build mod...", null, defaultName, ModService.ModFileExtensionNoDot);
            return outputPath;
        }
        
        private void RefreshCacheKeys()
        {
            if (_cacheKeysRefreshed)
                return;
            
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(this, out string guid, out long _))
                return;
            
            // initialize editor prefs keys for caching build target and output path for the rebuild tool
            _buildTargetKey = $"mod_config_buildtarget-{guid}";
            _outputPathKey = $"mod_config_outputpath-{guid}";
            _cacheKeysRefreshed = true;
        }
    }
}
