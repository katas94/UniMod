using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Katas.UniMod
{
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