using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Katas.UniMod.Editor
{
    [CreateAssetMenu(fileName = "ModConfig", menuName = "UniMod/Mod Config")]
    public sealed class ModConfig : ScriptableObject
    {
        public EmbeddedModConfig linkedEmbeddedConfig;
        
        [Header("Configuration")][Space(5)]
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public bool buildAssets;
        public ModStartup startup;
        public List<ModReference> dependencies;
        
        [Header("Target Application")][Space(5)]
        public string appId;
        public string appVersion;
        
        [Header("Includes")][Space(5)]
        public AssetIncludes<AssemblyDefinitionAsset> assemblyDefinitions;
        public AssetIncludes<DefaultAsset> managedPlugins;

        [Header("Build")][Space(5)]
        public ModBuilder builder;
        
        
#region VALIDATION
        public event Action IncludesModified;
        
        private void OnValidate()
        {
            assemblyDefinitions.Validate();
            managedPlugins.Validate(IsManagedPlugin);
            
            if (assemblyDefinitions.Changed || managedPlugins.Changed)
                IncludesModified?.Invoke();
            
            UpdateLinkedEmbeddedConfig();
        }

        private static bool IsManagedPlugin(DefaultAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            
            if (!importer)
                return false;
            
            return !importer.isNativePlugin;
        }
#endregion

        private void UpdateLinkedEmbeddedConfig()
        {
            if (!linkedEmbeddedConfig)
                return;
            
            linkedEmbeddedConfig.modId = modId;
            linkedEmbeddedConfig.modVersion = modVersion;
            linkedEmbeddedConfig.displayName = displayName;
            linkedEmbeddedConfig.description = description;
            linkedEmbeddedConfig.containsAssets = buildAssets;
            linkedEmbeddedConfig.startup = startup;
            linkedEmbeddedConfig.dependencies = dependencies;
            linkedEmbeddedConfig.appId = appId;
            linkedEmbeddedConfig.appVersion = appVersion;
            
            EditorUtility.SetDirty(linkedEmbeddedConfig);
        }
    }
}
