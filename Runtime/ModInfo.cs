using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Katas.UniMod
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
        [JsonConverter(typeof(StringEnumConverter))]
        public ModType Type;
        public string DisplayName;
        public string Description;
        public string Platform;
        public Dictionary<string, string> Dependencies;
    }

    public enum ModType
    {
        ContentAndAssemblies = 0,
        Content = 1,
        Assemblies = 2
    }
}
