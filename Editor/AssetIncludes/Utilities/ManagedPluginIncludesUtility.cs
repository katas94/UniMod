using System.Collections.Generic;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Utility methods to resolve managed plugin includes.
    /// </summary>
    public static class ManagedPluginIncludesUtility
    {
        /// <summary>
        /// Resolves and returns all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedManagedPluginPaths(AssetIncludes<DefaultAsset> assetIncludes, BuildTarget buildTarget)
        {
            using var _ = HashSetPool<string>.Get(out var guids);
            assetIncludes.ResolveIncludedGuids(guids);
            var paths = new List<string>(guids.Count);
            ResolveSupportedManagedPluginPaths(buildTarget, guids, paths);
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
            
            using var _ = HashSetPool<string>.Get(out var guids);
            assetIncludes.ResolveIncludedGuids(guids);
            ResolveSupportedManagedPluginPaths(buildTarget, guids, paths);
        }
        
        /// <summary>
        /// Resolves and returns all the included managed plugin paths, excluding non managed plugins and plugins that are not targeted to the given build target.
        /// </summary>
        public static List<string> ResolveIncludedSupportedManagedPluginPaths(
            BuildTarget buildTarget, bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<DefaultAsset> assetIncludes, IEnumerable<DefaultAsset> assetExcludes)
        {
            using var _ = HashSetPool<string>.Get(out var guids);
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, guids);
            var paths = new List<string>(guids.Count);
            ResolveSupportedManagedPluginPaths(buildTarget, guids, paths);
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
            
            using var _ = HashSetPool<string>.Get(out var guids);
            AssetIncludesUtility.ResolveIncludedGuids(includeAssetsFolder, folderIncludes, folderExcludes, assetIncludes, assetExcludes, guids);
            ResolveSupportedManagedPluginPaths(buildTarget, guids, paths);
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
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // since plugins have not an specific type, we need to fetch the importer to get the metadata
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                
                // add the plugin only if it is managed (non-native) and it is compatible with the given build target (or build target is no target)
                // if (importer && !importer.isNativePlugin && (buildTarget == BuildTarget.NoTarget || importer.GetCompatibleWithPlatform(buildTarget)))
                //     paths.Add(path);
                
                // I have temporarily disabled the previous check since for some reason the importer.GetCompatibleWithPlatform is not working as expected.
                // for the same compatible assembly in two different projects I get true and false results...
                if (importer && !importer.isNativePlugin)
                    paths.Add(path);
            }
        }
    }
}