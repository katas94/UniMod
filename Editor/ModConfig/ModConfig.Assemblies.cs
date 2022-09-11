using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace Katas.Modman.Editor
{
    public partial class ModConfig
    {
        private const string ASSEMBLY_DEFINITION_ASSET_FILTER = "t:" + nameof(AssemblyDefinitionAsset);
        
        private static readonly HashSet<string> Guids = new();
        private static readonly HashSet<BuildTarget> SupportedBuildTargets = new();

        public List<string> GetIncludedAssemblies(BuildTarget buildTarget)
        {
            var names = new List<string>();
            GetIncludedAssemblies(buildTarget, names);
            return names;
        }
        
        /// <summary>
        /// Populates the given list with all the names for the currently included assembly definitions.
        /// Those included assemblies that don't support the given build target will be excluded from the results.
        /// </summary>
        public void GetIncludedAssemblies(BuildTarget buildTarget, List<string> names)
        {
            if (names is null)
                return;
            
            // find all assembly definition assets in included and excluded folders
            var includedGuids = FindAssets(ASSEMBLY_DEFINITION_ASSET_FILTER, folderIncludes, includeAssetsFolder);
            var excludedGuids = FindAssets(ASSEMBLY_DEFINITION_ASSET_FILTER, folderExcludes, false);
            
            // add included guids from the included folders and specific includes
            Guids.Clear();
            Guids.UnionWith(includedGuids);
            foreach (var asset in assemblyDefinitionIncludes)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    Guids.Add(guid);
            
            // remove the excluded guids from the excluded folders and specific excludes
            Guids.ExceptWith(excludedGuids);
            foreach (var asset in assemblyDefinitionExcludes)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    Guids.Remove(guid);
            
            // populate the paths
            foreach (string guid in Guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // parse the assembly definition (unfortunately this is the only way to fetch the assembly metadata)
                var assemblyDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                var token = JToken.Parse(assemblyDefinition.text);
                
                // get the assembly name (this will be the name of the .dll file compiled by Unity)
                string name = token["name"]?.Value<string>();
                if (string.IsNullOrEmpty(name))
                    continue;
                
                // check if the given target platform is supported by the assembly
                SupportedBuildTargets.Clear();
                GetAssemblyDefinitionSupportedBuildTargets(token, SupportedBuildTargets);
                if (SupportedBuildTargets.Contains(buildTarget))
                    names.Add(name);
            }
        }
        
        private static void GetAssemblyDefinitionSupportedBuildTargets(JToken token, HashSet<BuildTarget> supportedBuildTargets)
        {
            // as specified in Unity's documentation, the includePlatforms and excludePlatforms arrays cannot be used toguether, so we need to check
            // which is defined and contains platforms
            var includedToken = token["includePlatforms"];
            if (includedToken is JArray includedArray && includedArray.Count > 0)
            {
                foreach (var platformToken in includedArray)
                {
                    var buildTarget = GetAssemblyDefinitionPlatformAsBuildTarget(platformToken.Value<string>());
                    
                    if (buildTarget != BuildTarget.NoTarget)
                        supportedBuildTargets.Add(buildTarget);
                }
                
                return;
            }
            
            // if no includes are specified, we need to add all supported platforms by default and then exclude them
            supportedBuildTargets.Add(BuildTarget.Android);
            supportedBuildTargets.Add(BuildTarget.EmbeddedLinux);
            supportedBuildTargets.Add(BuildTarget.GameCoreXboxSeries);
            supportedBuildTargets.Add(BuildTarget.GameCoreXboxOne);
            supportedBuildTargets.Add(BuildTarget.iOS);
            supportedBuildTargets.Add(BuildTarget.StandaloneLinux64);
            supportedBuildTargets.Add(BuildTarget.CloudRendering);
            supportedBuildTargets.Add(BuildTarget.Lumin);
            supportedBuildTargets.Add(BuildTarget.StandaloneOSX);
            supportedBuildTargets.Add(BuildTarget.PS4);
            supportedBuildTargets.Add(BuildTarget.PS5);
            supportedBuildTargets.Add(BuildTarget.Stadia);
            supportedBuildTargets.Add(BuildTarget.Switch);
            supportedBuildTargets.Add(BuildTarget.tvOS);
            supportedBuildTargets.Add(BuildTarget.WSAPlayer);
            supportedBuildTargets.Add(BuildTarget.WebGL);
            supportedBuildTargets.Add(BuildTarget.StandaloneWindows);
            supportedBuildTargets.Add(BuildTarget.StandaloneWindows64);
            supportedBuildTargets.Add(BuildTarget.XboxOne);
            
            var excludeToken = token["excludePlatforms"];
            if (excludeToken is JArray excludedArray && excludedArray.Count > 0)
            {
                foreach (var platformToken in excludedArray)
                {
                    var buildTarget = GetAssemblyDefinitionPlatformAsBuildTarget(platformToken.Value<string>());
                    
                    if (buildTarget != BuildTarget.NoTarget)
                        supportedBuildTargets.Remove(buildTarget);
                }
            }
        }

        private static BuildTarget GetAssemblyDefinitionPlatformAsBuildTarget(string platform)
        {
            return platform switch
            {
                "Android" => BuildTarget.Android,
                "Editor" => BuildTarget.NoTarget,
                "EmbeddedLinux" => BuildTarget.EmbeddedLinux,
                "GameCoreScarlett" => BuildTarget.GameCoreXboxSeries,
                "GameCoreXboxOne" => BuildTarget.GameCoreXboxOne,
                "iOS" => BuildTarget.iOS,
                "LinuxStandalone64" => BuildTarget.StandaloneLinux64,
                "CloudRendering" => BuildTarget.CloudRendering,
                "Lumin" => BuildTarget.Lumin,
                "macOSStandalone" => BuildTarget.StandaloneOSX,
                "PS4" => BuildTarget.PS4,
                "PS5" => BuildTarget.PS5,
                "Stadia" => BuildTarget.Stadia,
                "Switch" => BuildTarget.Switch,
                "tvOS" => BuildTarget.tvOS,
                "WSA" => BuildTarget.WSAPlayer,
                "WebGL" => BuildTarget.WebGL,
                "WindowsStandalone32" => BuildTarget.StandaloneWindows,
                "WindowsStandalone64" => BuildTarget.StandaloneWindows64,
                "XboxOne" => BuildTarget.XboxOne,
                _ => BuildTarget.NoTarget
            };
        }
    }
}
