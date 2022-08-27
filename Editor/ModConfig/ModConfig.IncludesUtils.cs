using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditorInternal;

namespace ModmanEditor
{
    public partial class ModConfig
    {
        private const string ASSEMBLY_DEFINITION_ASSET_FILTER = "t:" + nameof(AssemblyDefinitionAsset);
        private const string PLUGIN_FILTER = "t:" + nameof(DefaultAsset);
        
        private static readonly List<string> Paths = new List<string>();
        private static readonly List<string> ValidFolders = new List<string>();
        private static readonly HashSet<string> Guids = new HashSet<string>();
        private static readonly HashSet<BuildTarget> SupportedBuildTargets = new HashSet<BuildTarget>();
        
        public List<string> GetAllIncludedManagedAssemblyNames(BuildTarget targetPlatform)
        {
            var assemblyNames = new List<string>();
            GetAllIncludedManagedAssemblyNames(targetPlatform, assemblyNames);
            return assemblyNames;
        }
        
        /// <summary>
        /// Populates the given list with all the managed assembly names (without the dll extension) that are included for this
        /// mod config. It will filter out those that doesn't support the given target platform. The results will contain both assemblies
        /// from assembly definition files and plugin files.
        /// </summary>
        public void GetAllIncludedManagedAssemblyNames(BuildTarget targetPlatform, List<string> assemblyNames)
        {
            if (assemblyNames is null)
                return;
            
            // get assembly names from the included assembly definitions
            Paths.Clear();
            GetIncludedAssemblyDefinitions(Paths);

            foreach (string path in Paths)
            {
                // parse the assembly definition (unfortunately this is the only way to fetch the assembly data)
                var assemblyDefinition = AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                var token = JToken.Parse(assemblyDefinition.text);
                
                // get the assembly name (this will be the name of the .dll file compiled by Unity)
                string name = token["name"]?.Value<string>();
                if (string.IsNullOrEmpty(name))
                    continue;
                
                // check if the given target platform is supported by the assembly
                SupportedBuildTargets.Clear();
                GetAssemblyDefinitionSupportedBuildTargets(token, SupportedBuildTargets);
                if (SupportedBuildTargets.Contains(targetPlatform))
                    assemblyNames.Add(name);
            }
            
            // get assembly names from the included managed plugins
            Paths.Clear();
            GetIncludedManagedPlugins(Paths);

            foreach (string path in Paths)
            {
                // get the plugin asset importer and check if it is a valid managed plugin supported on the given target platform
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (importer && !importer.isNativePlugin && importer.GetCompatibleWithPlatform(targetPlatform))
                {
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (!string.IsNullOrEmpty(name))
                        assemblyNames.Add(name);
                }   
            }
        }

        public List<string> GetIncludedAssemblyDefinitions()
        {
            var paths = new List<string>();
            GetIncludedAssemblyDefinitions(paths);
            return paths;
        }
        
        public List<string> GetIncludedManagedPlugins()
        {
            var paths = new List<string>();
            GetIncludedManagedPlugins(paths);
            return paths;
        }
        
        /// <summary>
        /// Populates the given list with all the paths for the currently included assembly definition assets (excludes will processed and filtered out from the result).
        /// Given paths are validated to be assembly definition assets but the supported platforms will not be taken into account (i.e.: it will return editor only assembly definitions).
        /// </summary>
        public void GetIncludedAssemblyDefinitions(List<string> paths)
        {
            if (paths is null)
                return;
            
            // find all assembly definition assets in included and excluded folders
            var includedGuids = FindAssets(ASSEMBLY_DEFINITION_ASSET_FILTER, folderIncludes);
            var excludedGuids = FindAssets(ASSEMBLY_DEFINITION_ASSET_FILTER, folderExcludes);
            
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
                
                paths.Add(path);
            }
        }
        
        /// <summary>
        /// Populates the given list with all the paths for the currently included managed plugins (excludes will processed and filtered out from the result).
        /// Given paths are validated to be managed plugins but the supported platforms will not be taken into account (i.e.: it will return editor only plugins).
        /// </summary>
        public void GetIncludedManagedPlugins(List<string> paths)
        {
            if (paths is null)
                return;
            
            // find all default assets in included and excluded folders (plugins doesn't have an specific asset type)
            var includedGuids = FindAssets(PLUGIN_FILTER, folderIncludes);
            var excludedGuids = FindAssets(PLUGIN_FILTER, folderExcludes);
            
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
                
                // since plugins have not an specific type, we need to check if this is actually a valid managed plugin by fetching the importer
                var importer = AssetImporter.GetAtPath(path) as PluginImporter;
                if (!importer || importer.isNativePlugin)
                    continue;
                
                paths.Add(path);
            }
        }
        
        // helper method similar to AssetDatabase.FindAssets but with a collection of DefaultAsset objects that should be valid folders
        // if the given collection is empty, an empty array will be returned (contrary to AssetDatabase.FindAssets which would search on all folders)
        private static string[] FindAssets(string filter, IEnumerable<DefaultAsset> folderAssets)
        {
            ValidFolders.Clear();
            
            foreach (DefaultAsset folderAsset in folderAssets)
            {
                string folder = AssetDatabase.GetAssetPath(folderAsset);
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    continue;
                
                ValidFolders.Add(folder);
            }
            
            if (ValidFolders.Count == 0)
                return Array.Empty<string>();
            
            return AssetDatabase.FindAssets(filter, ValidFolders.ToArray());
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
