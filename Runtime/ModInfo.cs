using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Katas.Mango
{
    /// <summary>
    /// Structure for the mod's info.json file
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public struct ModInfo
    {
        public string AppId;
        public string AppVersion;
        public string ModId;
        public string ModVersion;
        public string DisplayName;
        public string Description;
        public string Platform;
        public Dictionary<string, string> Dependencies;
    }
}
