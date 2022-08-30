using System;
using System.Collections.Generic;
using UnityEditor;

namespace Katas.ModmanEditor
{
    public partial class ModConfig
    {
        private const string PLUGIN_FILTER = "t:" + nameof(DefaultAsset);
        
        private static readonly List<string> ValidFolders = new();

        public List<string> GetIncludedManagedPlugins(BuildTarget buildTarget)
        {
            var paths = new List<string>();
            GetIncludedManagedPlugins(buildTarget, paths);
            return paths;
        }
        
        /// <summary>
        /// Populates the given list with all the paths for the currently included managed plugins.
        /// Returned paths are guaranteed to be valid managed plugins supporting the given buildTarget.
        /// </summary>
        public void GetIncludedManagedPlugins(BuildTarget buildTarget, List<string> paths)
        {
            if (paths is null)
                return;
            
            // find all default assets in included and excluded folders (plugins doesn't have an specific asset type)
            var includedGuids = FindAssets(PLUGIN_FILTER, folderIncludes, includeAssetsFolder);
            var excludedGuids = FindAssets(PLUGIN_FILTER, folderExcludes, false);
            
            // add included guids from the included folders and specific includes
            Guids.Clear();
            Guids.UnionWith(includedGuids);
            foreach (var asset in managedPluginIncludes)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    Guids.Add(guid);

            // remove the excluded guids from the excluded folders and specific excludes
            Guids.ExceptWith(excludedGuids);
            foreach (var asset in managedPluginExcludes)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    Guids.Remove(guid);
            
            // populate the paths
            foreach (string guid in Guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                    continue;
                
                // since plugins have not an specific type, we need to fetch the importer to get the metadata
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                
                // add the plugin only if it is managed (non-native) and it is compatible with the given build target
                if (importer && !importer.isNativePlugin && importer.GetCompatibleWithPlatform(buildTarget))
                    paths.Add(path);
            }
        }
        
        // helper method similar to AssetDatabase.FindAssets but with a collection of DefaultAsset objects that should be valid folders
        // if the given collection is empty, an empty array will be returned (contrary to AssetDatabase.FindAssets which would search on all folders)
        private static string[] FindAssets(string filter, IEnumerable<DefaultAsset> folderAssets, bool includeAssetsFolder)
        {
            ValidFolders.Clear();
            
            foreach (DefaultAsset folderAsset in folderAssets)
            {
                string folder = AssetDatabase.GetAssetPath(folderAsset);
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    continue;
                
                ValidFolders.Add(folder);
            }
            
            if (includeAssetsFolder)
                ValidFolders.Add("Assets");
            
            if (ValidFolders.Count == 0)
                return Array.Empty<string>();
            
            return AssetDatabase.FindAssets(filter, ValidFolders.ToArray());
        }
    }
}
