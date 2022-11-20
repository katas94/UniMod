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
        [Tooltip(LinkedEmbeddedConfigTooltip)]
        public EmbeddedModConfig linkedEmbeddedConfig;

        [Header("Configuration")][Space(5)]
        [Tooltip(ModIdTooltip)]        public string modId;
        [Tooltip(ModVersionTooltip)]   public string modVersion;
        [Tooltip(DisplayNameTooltip)]  public string displayName;
        [Tooltip(DescriptionTooltip)]  public string description;
        [Tooltip(ThumbnailTooltip)]    public Texture2D thumbnail;
        [Tooltip(StartupTooltip)]      public ModStartup startup;
        [Tooltip(DependenciesTooltip)] public List<ModReference> dependencies;
        
        [Header("Target Application")][Space(5)]
        [Tooltip(AppIdTooltip)]      public string appId;
        [Tooltip(AppVersionTooltip)] public string appVersion;
        
        [Header("Content")][Space(5)]
        [Tooltip(AddressableGroupsTooltip)]   public List<AddressableAssetGroup> addressableGroups;
        [Tooltip(AssemblyDefinitionsTooltip)] public AssetIncludes<AssemblyDefinitionAsset> assemblyDefinitions;
        [Tooltip(ManagedPluginsTooltip)]      public AssetIncludes<DefaultAsset> managedPlugins;

        [Header("Build")][Space(5)]
        [Tooltip(BuilderTooltip)]
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

#region TOOLTIPS
        private const string LinkedEmbeddedConfigTooltip = "Optional: set an EmbeddedModConfig asset here so it is automatically updated with this config. Embedded configs contain runtime information so the mod can be included in a player build. Use the \"Create > UniMod > Embedded Mod Config\" menu to create one";
        private const string ModIdTooltip = "Required: a unique ID that represents this mod. Usually in the form of \"com.company.name\"";
        private const string ModVersionTooltip = "Required: the current version of the mod. It should use Semantic Versioning";
        private const string DisplayNameTooltip = "Recommended: the name of the mod ready for UI display";
        private const string DescriptionTooltip = "Recommended: a description of the mod ready for UI display";
        private const string ThumbnailTooltip = "Optional: the mod's thumbnail sprite to be included with the mod build";
        private const string StartupTooltip = "Optional: a reference to the mod's startup asset where you can define any custom initialization logic and configuration. An Addressables build is required to include the startup object, so if you want to have an assemblies only mod then use the ModStartup attribute in static methods instead";
        private const string DependenciesTooltip = "Optional: set here any dependencies to other mods. Each dependency must specify the ID and version of the mod so we can check at runtime if it is present. This mod won't load if any of the dependencies is missing in the host app";
        private const string AppIdTooltip = "Recommended: the unique ID of the host application that this mod is created for. If this is left empty this mod will be considered standalone, which means that it can be loaded by any project using UniMod that allows standalone mods";
        private const string AppVersionTooltip = "Recommended: the target version of the host application";
        private const string AddressableGroupsTooltip = "Optional: the addressable groups containing all the assets to include in this mod";
        private const string AssemblyDefinitionsTooltip = "Optional: includes/excludes of the script assemblies to include in this mod";
        private const string ManagedPluginsTooltip = "Optional: includes/excludes of the managed plugins to include in this mod";
        private const string BuilderTooltip = "Required: the builder asset used to build this mod. You will usually want to instantiate a local mod builder through the \"Create > UniMod > Local Mod Builder\" menu";
#endregion
    }
}
