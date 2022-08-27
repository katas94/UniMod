using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Modman;

namespace ModmanEditor
{
    public static partial class ModUtils
    {
        public const string MENU_ITEM = "Modman/";

        private const string OUTPUT_PATH_KEY = "modding_output_path";
        private const string BUILD_TARGET_KEY = "modding_build_target";

        public static string CachedBuildOutputPath
        {
            get => EditorPrefs.GetString(OUTPUT_PATH_KEY, null);
            set => EditorPrefs.SetString(OUTPUT_PATH_KEY, value);
        }

        public static CodeOptimization? CachedBuildMode
        {
            get
            {
                string buildTargetValue = EditorPrefs.GetString(BUILD_TARGET_KEY, null);

                if (string.IsNullOrEmpty(buildTargetValue) || !Enum.TryParse(buildTargetValue, out CodeOptimization value))
                    return null;
                
                return value;
            }

            set => EditorPrefs.SetString(BUILD_TARGET_KEY, value.ToString());
        }

        /// <summary>
        /// Builds the given mod config with the given parameters.
        /// </summary>
        public static async UniTask BuildModAsync (ModConfig config, CodeOptimization buildMode, string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("The given output path is null or empty.");
            if (buildMode == CodeOptimization.None)
                throw new Exception("Build mode cannot be none");

            IModBuilder builder = new FastModBuilder(config, buildMode, outputPath);
            await builder.BuildAsync();
            Debug.Log("Mod build completed!");
        }

        /// <summary>
        /// Builds the given mod config with the given parameters. The user will be asked for any unset parameters.
        /// </summary>
        public static async UniTask BuildModWithGuiAsync (ModConfig config, CodeOptimization? buildMode = null, string outputPath = null)
        {
            if (config is null)
                return;
            
            // ask the user for the parameters that were not specified
            buildMode ??= DisplayBuildModeDialog();
            if (buildMode == CodeOptimization.None)
                return;
            
            outputPath ??= DisplayModBuildOutputPathDialog(config, buildMode.Value);
            if (string.IsNullOrEmpty(outputPath))
                return;
            
            // cache parameters and build the mod
            CachedBuildOutputPath = outputPath;
            CachedBuildMode = buildMode;
            
            await BuildModAsync(config, buildMode.Value, outputPath);
        }

        public static CodeOptimization DisplayBuildModeDialog()
        {
            int option = EditorUtility.DisplayDialogComplex("Build mode", "Select a build mode", "Release", "Cancel", "Debug");

            return option switch
            {
                0 => CodeOptimization.Release,
                2 => CodeOptimization.Debug,
                _ => CodeOptimization.None
            };
        }
        
        public static string DisplayModBuildOutputPathDialog(ModConfig config, CodeOptimization buildMode)
        {
            if (config is null || buildMode == CodeOptimization.None)
                return null;
            
            string defaultName = $"{config.modId}-{config.modVersion}-{buildMode}{ModService.ModFileExtension}";
            string outputPath = EditorUtility.SaveFilePanel("Build mod...", null, defaultName, ModService.ModFileExtensionNoDot);
            return outputPath;
        }

        /// <summary>
        /// Returns the assembly names for all the custom defined assemblies (with assembly definition files) within the Assets folder.
        /// </summary>
        public static string[] GetProjectAssemblyNames (params string[] searchInFolders)
        {
            // copy all the user defined assemblies
            string[] assemblyDefinitionGUIDs = AssetDatabase.FindAssets($"t:{nameof(AssemblyDefinitionAsset)}", searchInFolders);
            string[] names = new string[assemblyDefinitionGUIDs.Length];

            for (int i = 0; i < assemblyDefinitionGUIDs.Length; ++i)
            {
                string guid = assemblyDefinitionGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var assemblyDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                string name = JToken.Parse(assemblyDefinition.text)["name"].Value<string>();

                if (string.IsNullOrEmpty(name))
                    throw new Exception($"Could not parse the assembly name from the assembly definition file at \"{path}\"");

                names[i] = name;
            }

            return names;
        }
    }
}
