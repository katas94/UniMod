using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Helper class designed to create a distributable Addressables build with specific build/load paths that are used for the catalog and all
    /// the groups added to the builder. This class will not use any default Addressables settings from the project but will rather create clean
    /// settings without for the build. Any already existing groups added to the builder will be copied, so they will not be modified even if you
    /// use the returned IGroupBuilder to add new asset entries.
    /// </summary>
    public sealed partial class AddressablesBuilder : IDisposable
    {
        private const string TmpAaDataFolder = "Assets/__tmp_addressable_assets_data";
        private const string BundlesRelativePath = "Bundles";
        private const string CatalogSuffix = "assets";
        private const string CatalogLoadPathName = "Catalog.LoadPath";
        private const string CatalogBuildPathName = "Catalog.BuildPath";
        private const string BundlesLoadPathName = "Bundles.LoadPath";
        private const string BundlesBuildPathName = "Bundles.BuildPath";
        
        private readonly AddressableAssetSettings _settings;
        private readonly HashSet<GroupBuilder> _groupBuilders = new();
        private readonly bool _createdDefaultConfigFolder;
        
        private AddressableAssetSettings _previousDefaultSettings;
        private bool _isDisposed = true;

        public AddressablesBuilder()
        {
            if (AssetDatabase.IsValidFolder(TmpAaDataFolder))
            {
                IOUtils.DeleteDirectory(TmpAaDataFolder);
                AssetDatabase.Refresh();
            }
            
            // ideally we would set isPersisted to false, but you cannot even set the default settings for the build if it is not persisted (:
            _settings = AddressableAssetSettings.Create(TmpAaDataFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, false, true);
            
            // manually create a default group (we could just set createDefaultGroups parameter to true in the previous method but I want to avoid the built-in data group)
            AddressableAssetGroup defaultGroup = _settings.CreateGroup(AddressableAssetSettings.DefaultLocalGroupName, true, false, false, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            var schema = defaultGroup.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            _settings.DefaultGroup = defaultGroup;
            
            // manually create a default Addressable assets config folder if it doesn't exist so setting AddressableAssetSettingsDefaultObject.Settings won't complain....
            if (!AssetDatabase.IsValidFolder(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder))
            {
                Directory.CreateDirectory(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder);
                _createdDefaultConfigFolder = true;
                AssetDatabase.Refresh();
            }
            
            // save current default settings to restore them later
            _previousDefaultSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            AddressableAssetSettingsDefaultObject.Settings = _settings;
            
            _isDisposed = false;
        }

        /// <summary>
        /// Creates a group from the default packed assets template that will be included in the build. Use the returned IGroupBuilder instance
        /// to add any assets to the group.
        /// </summary>
        public IGroupBuilder CreateGroup(string groupName)
        {
            ThrowIfDisposed();
            var groupBuilder = new GroupBuilder(_settings, groupName);
            _groupBuilders.Add(groupBuilder);
            return groupBuilder;
        }

        /// <summary>
        /// Creates a group with the given schemas that will be included in the build. Use the returned IGroupBuilder instance
        /// to add any assets to the group.
        /// </summary>
        public IGroupBuilder CreateGroup(
            string groupName,
            List<AddressableAssetGroupSchema> schemasToCopy,
            params Type[] types)
        {
            ThrowIfDisposed();
            var groupBuilder = new GroupBuilder(_settings, groupName, schemasToCopy, types);
            _groupBuilders.Add(groupBuilder);
            return groupBuilder;
        }

        /// <summary>
        /// Includes the given group in the build. You can use the returned IGroupBuilder to add new asset entries that will be removed
        /// after the build (if you add assets directly to the group, they will stay after).
        /// </summary>
        public IGroupBuilder AddGroup(AddressableAssetGroup group)
        {
            ThrowIfDisposed();

            if (!group)
                throw new Exception("Tried to add a null or destroyed group");
            
            var groupBuilder = new GroupBuilder(_settings, group);
            _groupBuilders.Add(groupBuilder);
            return groupBuilder;
        }
        
        /// <summary>
        /// Includes the given group in the build. You can provide a collection of IGroupBuilder objects to receive the group builders
        /// created for each given group. You can then use each IGroupBuilder to add new asset entries that will be removed
        /// after the build (if you add assets directly to the group, they will stay after).
        /// </summary>
        public void AddGroups(IEnumerable<AddressableAssetGroup> groups, ICollection<IGroupBuilder> builders = null)
        {
            ThrowIfDisposed();
            
            if (groups is null)
                return;
            
            foreach (AddressableAssetGroup group in groups)
            {
                var groupBuilder = AddGroup(group);
                builders?.Add(groupBuilder);
            }
        }
        
        public void Dispose()
        {
            if (_isDisposed)
                return;
            
            // dispose groups
            foreach(GroupBuilder groupBuilder in _groupBuilders)
                groupBuilder.Dispose();

            // restore previous default settings
            if (_previousDefaultSettings)
                AddressableAssetSettingsDefaultObject.Settings = _previousDefaultSettings;
            
            // remove tmp settings
            IOUtils.DeleteDirectory(TmpAaDataFolder, true);
            
            // remove the default config folder if we had to create it
            if (_createdDefaultConfigFolder)
                IOUtils.DeleteDirectory(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, true);
            
            AssetDatabase.Refresh();
            
            if (_previousDefaultSettings)
            {
                // for some mysterious reason the newly created groups are being added to the previous default settings, just Addressables...
                UniModEditorUtility.RemoveAddressableSettingsGroupMissingReferences(_previousDefaultSettings);
                
                // refresh the groups editor through reflexion since it gets really buggy and also Addressables is completely incapable of doing it by itself :)
                // P.D.: did i say already that the Addressables design and implementation is disappointing to say the least ? :/
                UniModEditorUtility.ReloadAddressablesGroupsEditor();
            }

            _isDisposed = true;
        }

        /// <summary>
        /// Builds the created/added groups with the given build/load paths. Uses a temporary AddressableAssetsSettings object that
        /// will be automatically removed after the build. You can set a beforeBuildingCallback to perform any extra configuration
        /// in the temporary AddressableAssetSettings instance before the build. It disposed the builder automatically after the build
        /// </summary>
        public AddressablesPlayerBuildResult Build(string buildPath, string loadPath, Action<AddressableAssetSettings> beforeBuildingCallback = null)
        {
            ThrowIfDisposed();
            
            if (string.IsNullOrEmpty(buildPath))
                throw new Exception("The given build path is null or empty");
            if (string.IsNullOrEmpty(loadPath))
                throw new Exception("The given load path is null or empty");

            try
            {
                // invoke before the setup so critical settings cannot be overwritten
                beforeBuildingCallback?.Invoke(_settings);
                return SetupAndBuild(buildPath, loadPath);
            }
            finally
            {
                Dispose();
            }
        }

        private AddressablesPlayerBuildResult SetupAndBuild(string buildPath, string loadPath)
        {
            // get the custom build/load paths for bundles
            string bundlesBuildPath = Path.Combine(buildPath, BundlesRelativePath);
            string bundlesLoadPath = Path.Combine(loadPath, BundlesRelativePath);
            
            // setup profile variables for the build/load paths
            AddressableAssetProfileSettings profileSettings = _settings.profileSettings;
            string catalogBuildPathId = profileSettings.CreateValue(CatalogBuildPathName, buildPath);
            string catalogLoadPathId = profileSettings.CreateValue(CatalogLoadPathName, loadPath);
            string bundlesBuildPathId = profileSettings.CreateValue(BundlesBuildPathName, bundlesBuildPath);
            string bundlesLoadPathId = profileSettings.CreateValue(BundlesLoadPathName, bundlesLoadPath);

            // setup remote catalog settings
            _settings.OverridePlayerVersion = CatalogSuffix;
            _settings.BuildRemoteCatalog = true;
            _settings.RemoteCatalogBuildPath.SetVariableById(_settings, catalogBuildPathId);
            _settings.RemoteCatalogLoadPath.SetVariableById(_settings, catalogLoadPathId);

            // setup build/load paths for the groups
            foreach (GroupBuilder groupBuilder in _groupBuilders)
                groupBuilder.SetPathIds(bundlesBuildPathId, bundlesLoadPathId);

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            
            if (!string.IsNullOrEmpty(result.Error))
                return result;
            
            // rename catalog files to remove the suffix
            string outputCatalogPath = Path.Combine(buildPath, $"catalog_{CatalogSuffix}.json");
            string destinationCatalogPath = Path.Combine(buildPath, $"catalog.json");
            File.Move(outputCatalogPath, destinationCatalogPath);
            string outputCatalogHashPath = Path.Combine(buildPath, $"catalog_{CatalogSuffix}.hash");
            string destinationCatalogHashPath = Path.Combine(buildPath, $"catalog.hash");
            File.Move(outputCatalogHashPath, destinationCatalogHashPath);
            
            return result;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new Exception("The builder has been disposed but you are trying to access it");
        }
    }
}