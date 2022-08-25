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

        public static string ModBuildOutputPath
        {
            get => PlayerPrefs.GetString(OUTPUT_PATH_KEY, null);
            set => PlayerPrefs.SetString(OUTPUT_PATH_KEY, value);
        }

        public static CodeOptimization? BuildTarget
        {
            get
            {
                string buildTargetValue = PlayerPrefs.GetString(BUILD_TARGET_KEY, null);

                if (string.IsNullOrEmpty(buildTargetValue) || !Enum.TryParse(buildTargetValue, out CodeOptimization value))
                    return null;
                
                return value;
            }

            set => PlayerPrefs.SetString(BUILD_TARGET_KEY, value.ToString());
        }

        [MenuItem(MENU_ITEM + "Build mod...")]
        public static void Build ()
            => BuildModWithGui();

        [MenuItem(MENU_ITEM + "Rebuild mod")]
        public static void Rebuild ()
            => BuildModWithGui(ModBuildOutputPath, BuildTarget);

        /// <summary>
        /// Builds the mod project to the given path.
        /// </summary>
        public static async UniTask BuildMod (string outputPath, CodeOptimization target)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("The given output path is null or empty.");
            if (target == CodeOptimization.None)
                throw new Exception("The given build target cannot be none");

            ModBuilder builder = new FastModBuilder(ModDefinition.Instance, target, outputPath);
            await builder.Build();
            Debug.Log("Mod build completed!");
        }

        // builds the mod and asks to the user the output path and target (if not given in the arguments)
        private static void BuildModWithGui (string outputPath = null, CodeOptimization? target = null)
        {
            if (!CheckModDefinition()) return;
            
            // if no build target is specified, ask for one to the user
            if (!target.HasValue)
            {
                int option = EditorUtility.DisplayDialogComplex("Build target", "Chose the build target", "Release", "Cancel build", "Debug");

                target = option switch
                {
                    0 => CodeOptimization.Release,
                    2 => CodeOptimization.Debug,
                    _ => null
                };

                if (!target.HasValue) return;
            }

            // if no output path is specified, ask for one to the user
            if (string.IsNullOrEmpty(outputPath))
            {
                string defaultName = $"{ModDefinition.Instance.DisplayName}-{target.Value}{ModService.MOD_FILE_EXTENSION}";
                outputPath = EditorUtility.SaveFilePanel("Build mod", null, defaultName, ModService.MOD_FILE_EXTENSION_NO_DOT);
                if (string.IsNullOrEmpty(outputPath)) return;
            }

            ModBuildOutputPath = outputPath;
            BuildTarget = target.Value;
            BuildMod(outputPath, target.Value).Forget();
        }

        private static bool CheckModDefinition ()
        {
            ModDefinition definition = ModDefinition.Instance;

            if (definition == null)
            {
                if (EditorUtility.DisplayDialog("Missing mod definition", "Could not find the mod definition file within the project. Do you want to initialise the mod setup?", "Yes", "No, cancel build"))
                {
                    InitialiseOrCheckModSetup();
                    Selection.activeObject = ModDefinition.Instance;
                }
                
                return false;
            }

            return true;
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
