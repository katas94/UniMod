using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditorInternal;

namespace Katas.UniMod.Editor
{
    [CreateAssetMenu(fileName = "ModConfig", menuName = "UniMod/Mod Config")]
    public sealed class ModConfig : ScriptableObject
    {
        public EmbeddedModConfig linkedEmbeddedConfig;

        [Header("Configuration")][Space(5)]
        public string modId;
        public string modVersion;
        public string displayName;
        public string description;
        public Texture2D thumbnail;
        public ModStartup startup;
        public List<ModReference> dependencies;
        
        [Header("Target Application")][Space(5)]
        public string appId;
        public string appVersion;
        
        [Header("Content")][Space(5)]
        public List<AddressableAssetGroup> addressableGroups;
        public AssetIncludes<AssemblyDefinitionAsset> assemblyDefinitions;
        public AssetIncludes<DefaultAsset> managedPlugins;

        [Header("Build")][Space(5)]
        public ModBuilder builder;

        public bool ContainsAssets
        {
            get
            {
                if (startup)
                    return true;
                if (addressableGroups is null || addressableGroups.Count == 0)
                    return false;
                
                // it could be the case that the added groups are empty so they won't generate any asset bundles
                return GatherAllAssetEntries().Count > 0;
            }
        }
        
        public event Action IncludesModified;
        
        public void SyncEmbeddedConfig(EmbeddedModConfig embeddedConfig)
        {
            if (!embeddedConfig)
                return;
            
            embeddedConfig.modId = modId;
            embeddedConfig.modVersion = modVersion;
            embeddedConfig.displayName = displayName;
            embeddedConfig.description = description;
            embeddedConfig.thumbnail = thumbnail;
            embeddedConfig.startup = startup;
            embeddedConfig.dependencies = dependencies;
            embeddedConfig.appId = appId;
            embeddedConfig.appVersion = appVersion;
            SyncEmbeddedModAssets(embeddedConfig.assets);
            SyncEmbeddedModAssemblies(embeddedConfig.assemblies);
            
            EditorUtility.SetDirty(embeddedConfig);
        }

        public void SyncEmbeddedModAssets(List<EmbeddedModAsset> assets)
        {
            // add all assets from the included addressable groups
            List<AddressableAssetEntry> entries = GatherAllAssetEntries();
            assets.Clear();
            assets.AddRange(entries.Select(
                entry => new EmbeddedModAsset()
                {
                    guid = entry.guid,
                    labels = new List<string>(entry.labels)
                })
            );
        }

        public void SyncEmbeddedModAssemblies(List<EmbeddedModAssemblies> assemblies)
        {
            assemblies.Clear();
            var buildTargets = (BuildTarget[])Enum.GetValues(typeof(BuildTarget));
            using var _ = HashSetPool<string>.Get(out var namesSet);

            foreach (BuildTarget buildTarget in buildTargets)
            {
                if (!UniModEditorUtility.TryGetRuntimePlatformFromBuildTarget(buildTarget, out RuntimePlatform platform))
                    continue;
                
                // resolve all included assembly names for the given build target
                var names = new List<string>();
                
                // get managed plugin paths and transform them into the managed assembly name
                ManagedPluginIncludesUtility.ResolveIncludedSupportedManagedPluginPaths(managedPlugins, buildTarget, names);
                for (int i = 0; i < names.Count; ++i)
                    names[i] = AssemblyName.GetAssemblyName(names[i]).Name;
                
                // get the user defined assembly names
                AssemblyDefinitionIncludesUtility.ResolveIncludedSupportedAssemblyNames(assemblyDefinitions, buildTarget, names);
                
                if (names.Count == 0)
                    continue;
                
                // remove duplicates
                namesSet.Clear();
                namesSet.UnionWith(names);
                names.Clear();
                names.AddRange(namesSet);
                
                // add a new embedded mod assemblies instance with the results
                assemblies.Add(new EmbeddedModAssemblies()
                {
                    platform = platform,
                    names = names
                });
            }
        }

        private List<AddressableAssetEntry> GatherAllAssetEntries()
        {
            var entries = new List<AddressableAssetEntry>();
            
            foreach (AddressableAssetGroup group in addressableGroups)
                if (group)
                    group.GatherAllAssets(entries, true, true, true);
            
            return entries;
        }
        
#region VALIDATION
        private void OnValidate()
        {
            assemblyDefinitions.Validate();
            managedPlugins.Validate(IsManagedPlugin);
            
            if (assemblyDefinitions.Changed || managedPlugins.Changed)
                IncludesModified?.Invoke();
            
            SyncEmbeddedConfig(linkedEmbeddedConfig);
        }

        private static bool IsManagedPlugin(DefaultAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            
            if (!importer)
                return false;
            
            return !importer.isNativePlugin;
        }
#endregion
    }
}
