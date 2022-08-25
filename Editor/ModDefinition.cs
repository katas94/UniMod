using System;
using UnityEngine;
using UnityEditor;
using Modman;

namespace ModmanEditor
{
    public class ModDefinition : ScriptableObject
    {
        // tries to get the project's ModDefinition object. If none is defined it will return null. If more than one is defined it will throw error
        public static ModDefinition Instance
        {
            get
            {
                string[] configs = AssetDatabase.FindAssets($"t:{nameof(ModDefinition)}", new string[] { "Assets" });
                
                if (configs.Length > 1)
                    throw new Exception($"More than one {nameof(ModDefinition)} asset is defined in the project. Please remove any extra objects.");
                
                string guid = configs != null && configs.Length > 0 ? configs[0] : null;

                if (string.IsNullOrEmpty(guid))
                    return null;
                else
                    return AssetDatabase.LoadAssetAtPath<ModDefinition>(AssetDatabase.GUIDToAssetPath(guid));
            }
        }

        public string Id;
        public string Version;
        public string DisplayName;
        public ModDependency[] Dependencies;
        public ModInitialiser Initialiser;
    }

    [Serializable]
    public struct ModDependency
    {
        public string Id;
        public string Version;
    }
}
