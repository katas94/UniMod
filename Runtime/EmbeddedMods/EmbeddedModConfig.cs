using System.Collections.Generic;
using UnityEngine;

namespace Katas.UniMod
{
    [CreateAssetMenu(fileName = "EmbeddedModConfig", menuName = "UniMod/Embedded Mod Config")]
    public sealed class EmbeddedModConfig : ScriptableObject
    {
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public bool containsAssets;
        public ModStartup startup;
        public List<ModReference> dependencies;
        public string appId;
        public string appVersion;
        public List<EmbeddedModAssemblies> assemblies = new();
    }
}