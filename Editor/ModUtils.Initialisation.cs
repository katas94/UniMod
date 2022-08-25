using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;
using Newtonsoft.Json.Linq;

namespace ModmanEditor
{
    public static partial class ModUtils
    {
        public const string MAIN_ASSEMBLY_DEFINITION_FILE = "Main.asmdef";
        public const string MOD_ASSETS_FOLDER = "ModAssets";
        public const string MOD_PLUGINS_FOLDER = "ModPlugins";

        public const string INIT_MOD_SETUP_MENU_ITEM = MENU_ITEM + "Init or check mod setup";

        public static readonly string ModAssetsFolder = Path.Combine("Assets", MOD_ASSETS_FOLDER);
        public static readonly string ModPluginsFolder = Path.Combine("Assets", MOD_PLUGINS_FOLDER);
        public static readonly string ModScriptsFolder = Path.Combine(ModAssetsFolder, "Scripts");
        public static readonly string MainAssemblyDefinitionFile = Path.Combine(ModScriptsFolder, MAIN_ASSEMBLY_DEFINITION_FILE);

        private static string MainAssemblyName => string.IsNullOrEmpty(ModDefinition.Instance?.Id) ? "Main" : ModDefinition.Instance.Id;

        [MenuItem(INIT_MOD_SETUP_MENU_ITEM, false, 0)]
        private static void InitialiseOrCheckModSetupMenu ()
            => InitialiseOrCheckModSetup();

        /// <summary>
        /// Checks the current mod setup validity.
        /// </summary>
        public static bool VerifyModSetup ()
        {
            // check that there is a unique ModDefinition instance and that addressable assets settings are initialised
            bool isSetupValid = ModDefinition.Instance != null && AddressableAssetSettingsDefaultObject.SettingsExists;

            // if there is a main assembly definition file, check that the assembly name equals the mod's id
            if (isSetupValid && File.Exists(MainAssemblyDefinitionFile))
            {
                // check the main assembly name
                var token = JToken.Parse(File.ReadAllText(MainAssemblyDefinitionFile));
                return token["name"].Value<string>() == MainAssemblyName;
            }

            return isSetupValid;
        }

        /// <summary>
        /// Initialises/checks the mod setup. It ensures that:<br/>
        /// * There is a mod definition asset.<br/>
        /// * There is a ModAssets root folder.<br/>
        /// * There is a ModPlugins root folder.<br/>
        /// * Addressable Assets settings are initialised within the project.<br/>
        /// * The main mod assembly under the ModAssets/Scripts folder exits and it is named with the defined mod's id.<br/>
        /// </summary>
        public static void InitialiseOrCheckModSetup ()
        {
            // if there is no mod definition file create a default one
            if (ModDefinition.Instance == null)
            {
                var definition = ScriptableObject.CreateInstance<ModDefinition>();
                definition.Id = "com.company.modname";
                definition.Version = "1.0.0";
                definition.DisplayName = "Mod Name";
                AssetDatabase.CreateAsset(definition, Path.Combine("Assets", "ModDefinition.asset"));
            }

            // check ModAssets and ModPlugins root folders
            if (!Directory.Exists(ModAssetsFolder))
                Directory.CreateDirectory(ModAssetsFolder);
            if (!Directory.Exists(ModPluginsFolder))
                Directory.CreateDirectory(ModPluginsFolder);
            // create the scripts folder if does not exist
            if (!Directory.Exists(ModScriptsFolder))
                Directory.CreateDirectory(ModScriptsFolder);

            // initialise AddressableAssets settings if not done already
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);
            
            // create/update the assembly definition file
            // NOTE: i would prefer an "official" way from unity to create an AssemblyDefinitionAsset instance, but there seems to be none at the moment
            string mainAssemblyName = MainAssemblyName;
            bool assemblyWasOutdated = false;

            if (File.Exists(MainAssemblyDefinitionFile))
            {
                // update the assembly name
                var token = JToken.Parse(File.ReadAllText(MainAssemblyDefinitionFile));

                if (token["name"].Value<string>() != mainAssemblyName)
                {
                    token["name"] = mainAssemblyName;
                    File.WriteAllText(MainAssemblyDefinitionFile, token.ToString());
                    assemblyWasOutdated = true;
                }
            }
            else
            {
                // create a new assembly definition file
                string assemblyText = $"{{\n\t\"name\": \"{mainAssemblyName}\"\n}}";
                File.WriteAllText(MainAssemblyDefinitionFile, assemblyText);
                assemblyWasOutdated = true;
            }

            AssetDatabase.Refresh();

            // make sure that the new precompiled assembly is generated
            if (assemblyWasOutdated)
                CompilationPipeline.RequestScriptCompilation();
        }

        // Checks the mod setup any time the project is opened (only if a ModDefinition file is present in the project)
        [InitializeOnLoad]
        private static class Startup
        {
            static Startup ()
            {
                if (ModDefinition.Instance == null) return;
                InitialiseOrCheckModSetup();
            }
        }
    }
}
