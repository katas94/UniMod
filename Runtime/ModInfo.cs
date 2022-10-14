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
    
    /// <summary>
    /// Contains all relevant information about the target of a mod build.
    /// </summary>
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy), ItemNullValueHandling = NullValueHandling.Ignore)]
    public struct ModTargetInfo
    {
        [JsonRequired]
        public string UnityVersion;
        [JsonRequired]
        public string UniModVersion;
        public string Platform;
        public string AppId;
        public string AppVersion;
    }
}
