using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditorInternal;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Utility methods to resolve assembly definition includes.
    /// </summary>
    public static class AssemblyDefinitionIncludesUtility
    {
        // map of platform strings used in assembly definitions to their build targets
        private static readonly Dictionary<string, BuildTarget> AssemblyDefinitionTargetsByPlatform
            = CompilationPipeline.GetAssemblyDefinitionPlatforms()
                .ToDictionary(platform => platform.Name, platform => platform.BuildTarget);
        
        // all build targets supported in assembly definition files
        private static readonly HashSet<BuildTarget> AssemblyDefinitionTargets
            = CompilationPipeline.GetAssemblyDefinitionPlatforms()
                .Select(platform => platform.BuildTarget)
                .ToHashSet();

        /// <summary>
        /// Resolves and returns all the included assembly names, excluding those assemblies that are not compatible with the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedAssemblyNames(AssetIncludes<AssemblyDefinitionAsset> assetIncludes, BuildTarget buildTarget)
        {
            using var _ = HashSetPool<string>.Get(out var guids);
            assetIncludes.ResolveIncludedGuids(guids);
            var names = new List<string>(guids.Count);
            ResolveSupportedAssemblyNames(buildTarget, guids, names);
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
            
            using var _ = HashSetPool<string>.Get(out var guids);
            assetIncludes.ResolveIncludedGuids(guids);
            ResolveSupportedAssemblyNames(buildTarget, guids, names);
        }
        
        /// <summary>
        /// Resolves and returns all the included assembly names, excluding those assemblies that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedAssemblyNames(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<AssemblyDefinitionAsset> assetIncludes, IEnumerable<AssemblyDefinitionAsset> assetExcludes)
        {
            using var _ = HashSetPool<string>.Get(out var guids);
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, guids);
            var names = new List<string>(guids.Count);
            ResolveSupportedAssemblyNames(buildTarget, guids, names);
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
            
            using var _ = HashSetPool<string>.Get(out var guids);
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, guids);
            ResolveSupportedAssemblyNames(buildTarget, guids, names);
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
            
            using var _ = HashSetPool<BuildTarget>.Get(out var supportedBuildTargets);
            
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
                supportedBuildTargets.Clear();
                GetAssemblyDefinitionSupportedBuildTargets(token, supportedBuildTargets);
                if (supportedBuildTargets.Contains(buildTarget))
                    names.Add(assemblyName);
            }
        }

        /// <summary>
        /// Given a Newtonsoft.Json JToken from a deserialized assembly definition file, it will populate the given set with the assembly's supported
        /// platforms.
        /// </summary>
        public static void GetAssemblyDefinitionSupportedBuildTargets(JToken token, ISet<BuildTarget> supportedBuildTargets)
        {
            BuildTarget buildTarget;
            
            // as specified in Unity's documentation, the includePlatforms and excludePlatforms arrays cannot be used together, so we need to check
            // which is defined and contains platforms
            JToken includedToken = token["includePlatforms"];
            if (includedToken is JArray { Count: > 0 } includedArray)
            {
                foreach (JToken platformToken in includedArray)
                {
                    string platform = platformToken.Value<string>();
                    
                    if (TryGetAssemblyDefinitionPlatformAsBuildTarget(platform, out buildTarget))
                        supportedBuildTargets.Add(buildTarget);
                }
                
                return;
            }
            
            // if no includes are specified, we need to add all supported platforms by default and then exclude them
            supportedBuildTargets.UnionWith(AssemblyDefinitionTargets);
            
            JToken excludeToken = token["excludePlatforms"];
            if (excludeToken is not JArray { Count: > 0 } excludedArray)
                return;
            
            foreach (JToken platformToken in excludedArray)
            {
                string platform = platformToken.Value<string>();
                
                if (TryGetAssemblyDefinitionPlatformAsBuildTarget(platform, out buildTarget))
                    supportedBuildTargets.Remove(buildTarget);
            }
        }

        /// <summary>
        /// Given the platform string found on a custom assembly definition, tries to return the equivalent BuildTarget.
        /// </summary>
        public static bool TryGetAssemblyDefinitionPlatformAsBuildTarget(string platform, out BuildTarget buildTarget)
        {
            return AssemblyDefinitionTargetsByPlatform.TryGetValue(platform, out buildTarget);
        }
    }
}