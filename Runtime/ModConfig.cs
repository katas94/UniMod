using System;

namespace Modman
{
    /// <summary>
    /// Structure for the mod config.json file
    /// </summary>
    [Serializable]
    public struct ModConfig
    {
        public string id;
        public string version;
        public string displayName;
        public string platform;
        public string sukiruVersion;
        public bool debugBuild;
        public string[] assemblies;
    }
}
