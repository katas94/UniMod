using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Katas.UniMod
{
    /// <summary>
    /// Structure for the mod's info.json file.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
    public struct ModInfo
    {
        [JsonRequired]
        public string Id;
        [JsonRequired]
        public string Version;
        public string DisplayName;
        public string Description;
        public Dictionary<string, string> Dependencies;
        [JsonRequired]
        public ModTargetInfo Target;
    }
}
