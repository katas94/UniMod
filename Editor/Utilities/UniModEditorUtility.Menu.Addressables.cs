﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    public static partial class UniModEditorUtility
    {
        private const string AddressablesMenu = "UniMod/Addressables Utility";
        
        [MenuItem(AddressablesMenu + "/Reload Groups Editor")]
        public static void ReloadAddressablesGroupsEditor()
        {
            if (AddressableAssetsWindow is null || GroupEditorField is null || ReloadMethod is null)
                return;
            
            EditorWindow window = EditorWindow.GetWindow(AddressableAssetsWindow);
            if (window is null)
                return;
            
            object groupEditor = GroupEditorField.GetValue(window);
            if (groupEditor is null)
                return;
            
            ReloadMethod.Invoke(groupEditor, Array.Empty<object>());
        }
        
        [MenuItem(AddressablesMenu + "/Create Mod Assets Group Template")]
        public static void CreateAddressablesModGroupTemplate()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(false);

            if (!settings)
            {
                Debug.LogError("Cannot create the mod group template because Addressables have not been initialized in the project!");
                return;
            }
            
            CreateAddressablesModGroupTemplate(settings);
        }
        
        /// <summary>
        /// Creates an Addressables group template which is the recommended way to create addressable groups for mods.
        /// </summary>
        public static void CreateAddressablesModGroupTemplate(AddressableAssetSettings settings)
        {
            if (IdField is null)
            {
                Debug.LogError("Failed to use reflexion to properly setup the template group, please kindly ask Unity to fix Addressables :)");
                return;
            }
            
            string assetPath = $"{settings.GroupTemplateFolder}/Mod Assets.asset";
            if (!Directory.Exists(settings.GroupTemplateFolder))
                Directory.CreateDirectory(settings.GroupTemplateFolder);
            
            // create and save template object
            AddressableAssetGroupTemplate template = ScriptableObject.CreateInstance<AddressableAssetGroupTemplate>();
            template.Description = "UniMod assets group template recommended for your mod assets. It is designed so you can share the group objects in custom Unity packages.";
            AssetDatabase.CreateAsset(template, assetPath);
            
            // add schemas
            template.AddSchema(typeof(BundledAssetGroupSchema));
            template.AddSchema(typeof(ContentUpdateGroupSchema));
            
            // setup bundled schema
            var bundledSchema = template.GetSchemaByType(typeof(BundledAssetGroupSchema)) as BundledAssetGroupSchema;

            if (bundledSchema is null)
            {
                AssetDatabase.DeleteAsset(assetPath);
                UnityEngine.Object.DestroyImmediate(template);
                Debug.LogError("Failed to create the mod assets group template");
                return;
            }

            try
            {
                // set custom build and load paths so the group can be shared across projects without depending on the Addressable settings profiles
                IdField.SetValue(bundledSchema.BuildPath, "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[UnityEditor.EditorUserBuildSettings.activeBuildTarget]");
                IdField.SetValue(bundledSchema.LoadPath, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[UnityEditor.EditorUserBuildSettings.activeBuildTarget]");
            }
            catch (Exception)
            {
                AssetDatabase.DeleteAsset(assetPath);
                UnityEngine.Object.DestroyImmediate(template);
                Debug.LogError("Failed to use reflexion to properly setup the template group, please kindly ask Unity to fix Addressables :)");
                return;
            }
            
            settings.AddGroupTemplateObject(template);
            AssetDatabase.SaveAssets();
        }
        
        public static void RemoveAddressableSettingsGroupMissingReferences(AddressableAssetSettings settings)
        {
            if (!settings || RemoveMissingGroupReferencesMethod is null)
                return;
            
            bool result = (bool)RemoveMissingGroupReferencesMethod.Invoke(settings, Array.Empty<object>());
            if (result)
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.GroupRemoved, null, true, true);
        }

#region REFLEXION_INFO
        private static readonly Type AddressableAssetsWindow =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetTypes()
                    .FirstOrDefault(type => type.Name == "AddressableAssetsWindow")
                ).FirstOrDefault(type => type is not null);
        
        private static readonly FieldInfo GroupEditorField =
            AddressableAssetsWindow.GetField("m_GroupEditor", BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static readonly Type AddressableAssetsSettingsGroupEditor =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetTypes()
                    .FirstOrDefault(type => type.Name == "AddressableAssetsSettingsGroupEditor")
                ).FirstOrDefault(type => type is not null);
        
        private static readonly MethodInfo ReloadMethod =
            AddressableAssetsSettingsGroupEditor.GetMethod("Reload", BindingFlags.Instance | BindingFlags.Public);
        
        private static readonly MethodInfo RemoveMissingGroupReferencesMethod =
            typeof(AddressableAssetSettings).GetMethod("RemoveMissingGroupReferences", BindingFlags.Instance | BindingFlags.NonPublic);
        
        private static readonly FieldInfo IdField =
            typeof(ProfileValueReference).GetField("m_Id", BindingFlags.Instance | BindingFlags.NonPublic);
#endregion
    }
}