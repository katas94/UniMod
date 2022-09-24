using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Utility methods to resolve AssetIncludes configurations.
    /// </summary>
    public static class AssetIncludesUtility
    {
        private static readonly List<string> ValidFolders = new();

        /// <summary>
        /// Resolves and returns the included asset GUIDs from the asset includes.
        /// </summary>
        public static HashSet<string> ResolveIncludedGuids<T>(this AssetIncludes<T> assetIncludes)
            where T : Object
        {
            var guids = new HashSet<string>();
            ResolveIncludedGuids(assetIncludes.includeAssetsFolder,
                assetIncludes.folderIncludes, assetIncludes.folderExcludes,
                assetIncludes.assetIncludes, assetIncludes.assetExcludes,
                guids);
            
            return guids;
        }
        
        /// <summary>
        /// Resolves the included asset GUIDs from the asset includes and populates the results into the given guids set.
        /// </summary>
        public static void ResolveIncludedGuids<T>(this AssetIncludes<T> assetIncludes, ISet<string> guids)
            where T : Object
        {
            ResolveIncludedGuids(assetIncludes.includeAssetsFolder,
                assetIncludes.folderIncludes, assetIncludes.folderExcludes,
                assetIncludes.assetIncludes, assetIncludes.assetExcludes,
                guids);
        }

        /// <summary>
        /// It resolves the given includes and excludes for the given asset type and populates the guids set with the
        /// resolved asset GUIDs.
        /// </summary>
        public static void ResolveIncludedGuids<T>(bool includeAssetsFolder,
            IEnumerable<DefaultAsset> folderIncludes, IEnumerable<DefaultAsset> folderExcludes,
            IEnumerable<T> assetIncludes, IEnumerable<T> assetExcludes,
            ISet<string> guids) where T : Object
        {
            var filter = $"t:{typeof(T).Name}";
            
            // find all guids in included and excluded folders
            string[] includedGuids = FindAssets(filter, includeAssetsFolder, folderIncludes);
            string[] excludedGuids = FindAssets(filter, false, folderExcludes);

            // include/exclude folders
            guids.UnionWith(includedGuids);
            guids.ExceptWith(excludedGuids);
            
            // now include specific assets (they will override folder excludes)
            if (assetIncludes is not null)
                foreach (T asset in assetIncludes)
                    if (asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                        guids.Add(guid);

            // now exclude specific assets (they will override anything)
            if (assetExcludes is not null)
                foreach (T asset in assetExcludes)
                    if (asset && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                        guids.Remove(guid);
        }
        
        /// <summary>
        /// Helper method similar to AssetDatabase.FindAssets but with a collection of DefaultAsset objects that should be valid folders.
        /// If the given collection is empty, an empty array will be returned (contrary to AssetDatabase.FindAssets which would search on all folders)
        /// </summary>
        public static string[] FindAssets(string filter, bool includeAssetsFolder, IEnumerable<DefaultAsset> folderAssets)
        {
            ValidFolders.Clear();

            if (folderAssets is not null)
            {
                foreach (DefaultAsset folderAsset in folderAssets)
                {
                    string folder = AssetDatabase.GetAssetPath(folderAsset);
                    if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                        continue;
                    
                    ValidFolders.Add(folder);
                }
            }
            
            if (includeAssetsFolder)
                ValidFolders.Add("Assets");
            
            if (ValidFolders.Count == 0)
                return Array.Empty<string>();
            
            return AssetDatabase.FindAssets(filter, ValidFolders.ToArray());
        }
    }
}

    