using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
        [JsonRequired][JsonConverter(typeof(StringEnumConverter))]
        public ModType Type;
        public string DisplayName;
        public string Description;
        public Dictionary<string, string> Dependencies;
        [JsonRequired]
        public ModTargetInfo Target;
    }
    
    /// <summary>
    /// Contains all relevant information about the target of a mod build.
    /// </summary>
    public struct ModTargetInfo
    {
        [JsonRequired]
        public string UniModVersion;
        public string AppId;
        public string AppVersion;
        public string Platform;
    }

    public enum ModType
    {
        ContentAndAssemblies = 0,
        Content = 1,
        Assemblies = 2
    }
}
