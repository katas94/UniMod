using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Utility methods to resolve assembly definition includes.
    /// </summary>
    public static class AssemblyDefinitionIncludesUtility
    {
        private static readonly HashSet<string> Guids = new();
        private static readonly HashSet<BuildTarget> SupportedBuildTargets = new();
        
        /// <summary>
        /// Resolves and returns all the included assembly names, excluding those assemblies that are not compatible with the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedAssemblyNames(AssetIncludes<AssemblyDefinitionAsset> assetIncludes, BuildTarget buildTarget)
        {
            Guids.Clear();
            assetIncludes.ResolveIncludedGuids(Guids);
            var names = new List<string>(Guids.Count);
            ResolveSupportedAssemblyNames(buildTarget, Guids, names);
            return names;
        }
        
        /// <summary>
        /// Resolves all the included assembly names, excluding those assemblies that are not targeted to the given build target.
        /// Results will be added to the given names list.
        /// </summary>
        public static void ResolveIncludedSupportedAssemblyNames(
            AssetIncludes<AssemblyDefinitionAsset> assetIncludes,
            BuildTarget buildTarget, List<string> names)
        {
            if (names is null)
                return;
            
            Guids.Clear();
            assetIncludes.ResolveIncludedGuids(Guids);
            ResolveSupportedAssemblyNames(buildTarget, Guids, names);
        }
        
        /// <summary>
        /// Resolves and returns all the included assembly names, excluding those assemblies that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedAssemblyNames(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<AssemblyDefinitionAsset> assetIncludes, IEnumerable<AssemblyDefinitionAsset> assetExcludes)
        {
            Guids.Clear();
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, Guids);
            var names = new List<string>(Guids.Count);
            ResolveSupportedAssemblyNames(buildTarget, Guids, names);
            return names;
        }

        /// <summary>
        /// Resolves all the included assembly names, excluding those assemblies that are not targeted to the given build target.
        /// Results will be added to the given names list.
        /// </summary>
        public static void ResolveIncludedSupportedAssemblyNames(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<AssemblyDefinitionAsset> assetIncludes, IEnumerable<AssemblyDefinitionAsset> assetExcludes,
            List<string> names)
        {
            if (names is null)
                return;
            
            Guids.Clear();
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, Guids);
            ResolveSupportedAssemblyNames(buildTarget, Guids, names);
        }

        /// <summary>
        /// Resolves and returns all the assembly names for the given assembly definition GUIDs. Assembly definitions that are not targeted to the
        /// given build target will be excluded.
        /// </summary>
        public static List<string> ResolveSupportedAssemblyNames(BuildTarget buildTarget, IEnumerable<string> guids)
        {
            var names = new List<string>();
            ResolveSupportedAssemblyNames(buildTarget, guids, names);
            return names;
        }
        
        /// <summary>
        /// Resolves all the assembly names for the given assembly definition GUIDs. Assembly definitions that are not targeted to the
        /// given build target will be excluded. The results will be added to the given names list.
        /// </summary>
        public static void ResolveSupportedAssemblyNames(BuildTarget buildTarget, IEnumerable<string> guids, List<string> names)
        {
            if (names is null)
                return;
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // parse the assembly definition (unfortunately this is the only way to fetch the assembly metadata)
                var assemblyDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                JToken token = JToken.Parse(assemblyDefinition.text);
                
                // get the assembly name (this will be the name of the .dll file compiled by Unity)
                var assemblyName = token["name"]?.Value<string>();
                if (string.IsNullOrEmpty(assemblyName))
                    continue;

                // if no build target is specified then return all assemblies
                if (buildTarget == BuildTarget.NoTarget)
                {
                    names.Add(assemblyName);
                    return;
                }
                
                // check if the given target platform is supported by the assembly
                SupportedBuildTargets.Clear();
                GetAssemblyDefinitionSupportedBuildTargets(token, SupportedBuildTargets);
                if (SupportedBuildTargets.Contains(buildTarget))
                    names.Add(assemblyName);
            }
        }

        /// <summary>
        /// Given a Newtonsoft.Json JToken from a deserialized assembly definition file, it will populate the given set with the assembly's supported
        /// platforms.
        /// </summary>
        public static void GetAssemblyDefinitionSupportedBuildTargets(JToken token, ISet<BuildTarget> supportedBuildTargets)
        {
            // as specified in Unity's documentation, the includePlatforms and excludePlatforms arrays cannot be used together, so we need to check
            // which is defined and contains platforms
            JToken includedToken = token["includePlatforms"];
            if (includedToken is JArray { Count: > 0 } includedArray)
            {
                foreach (JToken platformToken in includedArray)
                {
                    BuildTarget buildTarget = GetAssemblyDefinitionPlatformAsBuildTarget(platformToken.Value<string>());
                    
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
            
            JToken excludeToken = token["excludePlatforms"];
            if (excludeToken is not JArray { Count: > 0 } excludedArray)
                return;
            
            foreach (JToken platformToken in excludedArray)
            {
                BuildTarget buildTarget = GetAssemblyDefinitionPlatformAsBuildTarget(platformToken.Value<string>());
                
                if (buildTarget != BuildTarget.NoTarget)
                    supportedBuildTargets.Remove(buildTarget);
            }
        }

        /// <summary>
        /// Given the platform string found on a custom assembly definition, tries to return the equivalent BuildTarget.
        /// </summary>
        public static BuildTarget GetAssemblyDefinitionPlatformAsBuildTarget(string platform)
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