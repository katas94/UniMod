using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Katas.Modman.Editor
{
    [CreateAssetMenu(fileName = "ModConfig", menuName = "Modman/Mod Config")]
    public sealed partial class ModConfig : ScriptableObject
    {
        [Serializable]
        public struct ModDependency
        {
            public string id;
            public string version;
        }
        
        public string appId;
        public string appVersion;
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public ModStartup startup;
        public List<ModDependency> dependencies;
        
        [Header("Includes")][Space(5)]
        public bool includeAssetsFolder = true;
        public List<DefaultAsset> folderIncludes;
        public List<DefaultAsset> managedPluginIncludes;
        public List<AssemblyDefinitionAsset> assemblyDefinitionIncludes;
        
        [Header("Excludes")][Space(5)]
        public List<DefaultAsset> folderExcludes;
        public List<DefaultAsset> managedPluginExcludes;
        public List<AssemblyDefinitionAsset> assemblyDefinitionExcludes;
        
        [Header("Build")][Space(5)]
        public ModBuilder builder;
    }
}
