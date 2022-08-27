using System;
using System.Collections.Generic;
using UnityEngine;
using Modman;
using UnityEditor;
using UnityEditorInternal;

namespace ModmanEditor
{
    [CreateAssetMenu(fileName = "ModConfig", menuName = "Modman/ModConfig")]
    public partial class ModConfig : ScriptableObject
    {
        [Serializable]
        public struct ModDependency
        {
            public string Id;
            public string Version;
        }
        
        public string appId;
        public string appVersion;
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public List<ModDependency> dependencies;
        public ModInstanceProvider instanceProvider;
        
        [Header("Includes")][Space(5)]
        public List<DefaultAsset> folderIncludes;
        public List<DefaultAsset> managedPluginIncludes;
        public List<AssemblyDefinitionAsset> assemblyDefinitionIncludes;
        
        [Header("Excludes")][Space(5)]
        public List<DefaultAsset> folderExcludes;
        public List<DefaultAsset> managedPluginExcludes;
        public List<AssemblyDefinitionAsset> assemblyDefinitionExcludes;
    }
}
