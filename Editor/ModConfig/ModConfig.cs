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
        [Serializable]
        public struct ModDependency
        {
            public string id;
            public string version;
        }
        
        [Header("Configuration")]
        public string appId;
        public string appVersion;
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public bool assembliesOnly;
        public ModStartup startup;
        public List<ModDependency> dependencies;
        
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
    }
}
