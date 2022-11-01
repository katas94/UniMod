using System;
using System.Collections.Generic;
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
        
        public bool ContainsAssets => startup || (addressableGroups is not null && addressableGroups.Count > 0);
        
#region VALIDATION
        public event Action IncludesModified;
        
        private void OnValidate()
        {
            assemblyDefinitions.Validate();
            managedPlugins.Validate(IsManagedPlugin);
            
            if (assemblyDefinitions.Changed || managedPlugins.Changed)
                IncludesModified?.Invoke();
            
            UpdateLinkedEmbeddedConfig();
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

        private void UpdateLinkedEmbeddedConfig()
        {
            if (!linkedEmbeddedConfig)
                return;
            
            linkedEmbeddedConfig.modId = modId;
            linkedEmbeddedConfig.modVersion = modVersion;
            linkedEmbeddedConfig.displayName = displayName;
            linkedEmbeddedConfig.description = description;
            linkedEmbeddedConfig.startup = startup;
            linkedEmbeddedConfig.dependencies = dependencies;
            linkedEmbeddedConfig.appId = appId;
            linkedEmbeddedConfig.appVersion = appVersion;
            UpdateEmbeddedModAssemblies(linkedEmbeddedConfig.assemblies);
            
            EditorUtility.SetDirty(linkedEmbeddedConfig);
        }

        private void UpdateEmbeddedModAssemblies(List<EmbeddedModAssemblies> assemblies)
        {
            assemblies.Clear();
            var buildTargets = Enum.GetValues(typeof(BuildTarget)) as IEnumerable<BuildTarget>;
            if (buildTargets is null)
                return;
            
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
    }
}
