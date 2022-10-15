using System.Collections.Generic;
using UnityEngine;

namespace Katas.UniMod
{
    [CreateAssetMenu(fileName = "EmbeddedModConfig", menuName = "UniMod/Embedded Mod Config")]
    public class EmbeddedModConfig : ScriptableObject
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
    }
}