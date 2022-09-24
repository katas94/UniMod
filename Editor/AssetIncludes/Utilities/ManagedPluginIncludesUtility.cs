using System.Collections.Generic;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Utility methods to resolve managed plugin includes.
    /// </summary>
    public static class ManagedPluginIncludesUtility
    {
        private static readonly HashSet<string> Guids = new();
        
        /// <summary>
        /// Resolves and returns all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedManagedPluginPaths(AssetIncludes<DefaultAsset> assetIncludes, BuildTarget buildTarget)
        {
            Guids.Clear();
            assetIncludes.ResolveIncludedGuids(Guids);
            var paths = new List<string>(Guids.Count);
            ResolveSupportedManagedPluginPaths(buildTarget, Guids, paths);
            return paths;
        }
        
        /// <summary>
        /// Resolves all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// Results will be added to the given paths list.
        /// </summary>
        public static void ResolveIncludedSupportedManagedPluginPaths(
            AssetIncludes<DefaultAsset> assetIncludes,
            BuildTarget buildTarget, List<string> paths)
        {
            if (paths is null)
                return;
            
            Guids.Clear();
            assetIncludes.ResolveIncludedGuids(Guids);
            ResolveSupportedManagedPluginPaths(buildTarget, Guids, paths);
        }
        
        /// <summary>
        /// Resolves and returns all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedManagedPluginPaths(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<DefaultAsset> assetIncludes, IEnumerable<DefaultAsset> assetExcludes)
        {
            Guids.Clear();
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, Guids);
            var paths = new List<string>(Guids.Count);
            ResolveSupportedManagedPluginPaths(buildTarget, Guids, paths);
            return paths;
        }

        /// <summary>
        /// Resolves all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// Results will be added to the given paths list.
        /// </summary>
        public static void ResolveIncludedSupportedManagedPluginPaths(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<DefaultAsset> assetIncludes, IEnumerable<DefaultAsset> assetExcludes,
            List<string> paths)
        {
            if (paths is null)
                return;
            
            Guids.Clear();
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, Guids);
            ResolveSupportedManagedPluginPaths(buildTarget, Guids, paths);
        }

        /// <summary>
        /// Resolves and returns all the plugin paths for the given GUIDs. Non managed plugins and plugins that are not targeted to the
        /// given build target will be excluded.
        /// </summary>
        public static List<string> ResolveSupportedManagedPluginPaths(BuildTarget buildTarget, IEnumerable<string> guids)
        {
            var paths = new List<string>();
            ResolveSupportedManagedPluginPaths(buildTarget, guids, paths);
            return paths;
        }
        
        /// <summary>
        /// Resolves all the plugin paths for the given GUIDs. Non managed plugins and plugins that are not targeted to the
        /// given build target will be excluded. The results will be added to the given paths list.
        /// </summary>
        public static void ResolveSupportedManagedPluginPaths(BuildTarget buildTarget, IEnumerable<string> guids, List<string> paths)
        {
            if (paths is null)
                return;
            
            foreach (string guid in Guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // since plugins have not an specific type, we need to fetch the importer to get the metadata
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                
                // add the plugin only if it is managed (non-native) and it is compatible with the given build target (or build target is no target)
                if (importer && !importer.isNativePlugin && (buildTarget == BuildTarget.NoTarget || importer.GetCompatibleWithPlatform(buildTarget)))
                    paths.Add(path);
            }
        }
    }
}