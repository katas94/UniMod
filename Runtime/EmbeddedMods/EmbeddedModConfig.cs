using System.Collections.Generic;
using UnityEngine;

namespace Katas.UniMod
{
    [CreateAssetMenu(fileName = "EmbeddedModConfig", menuName = "UniMod/Embedded Mod Config")]
    public sealed class EmbeddedModConfig : ScriptableObject
    {
        [Header("Configuration")]
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public ModStartup startup;
        public List<ModReference> dependencies = new();
        
        [Header("Target Application")][Space(5)]
        public string appId;
        public string appVersion;
        
        [Header("Content")][Space(5)]
        public List<EmbeddedModAsset> assets = new();
        public List<EmbeddedModAssemblies> assemblies = new();
        
        public bool ContainsAssets => startup || (assets is not null && assets.Count > 0);
    }
}