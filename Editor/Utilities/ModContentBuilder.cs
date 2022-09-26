using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Helper class to perform Addressable content builds for mods. Using this class you can easily add assets to the build
    /// that will not show up on Addressables after the build is finished. It will also setup all the necessary settings for
    /// the build and will restore the previous settings after the build.
    /// </summary>
    public sealed class ModContentBuilder
    {
        private const string ModProfileName = "__unimod_profile";
        
        private AddressableAssetSettings _settings;
        private readonly List<string> _addedGuids = new();

        /// <summary>
        /// Adds the given asset for the content build. You can optionally set the address manually.
        /// </summary>
        public void AddAsset(Object asset, string address = null)
        {
            if (!asset)
                return;
            
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                throw new Exception($"Could not get the asset GUID from the AssetDatabase: {asset}");
            
            Initialize();
            AddressableAssetEntry entry = _settings.CreateOrMoveEntry(guid, _settings.DefaultGroup, true, false);
            if (address is not null)
                entry.address = address;
            
            _addedGuids.Add(guid);
        }

        /// <summary>
        /// Builds the mod content for the given mod ID into the given output folder. All assets added until this point
        /// will be built and cleared after, which means that you can reuse this instance to add new assets and perform
        /// another content build.
        /// </summary>
        public AddressablesPlayerBuildResult BuildContent(string modId, string outputFolder)
        {
            if (string.IsNullOrEmpty(modId))
                throw new Exception("The given mod ID is null or empty");
            if (string.IsNullOrEmpty(outputFolder))
                throw new Exception("The given output folder is null or empty");
            
            Initialize();
            AddressableAssetProfileSettings profileSettings = _settings.profileSettings;
            string profileId = null;
            
            // cache previous settings so we can restore them after the build
            string previousOverridePlayerVersion = _settings.OverridePlayerVersion;
            bool previousBuildRemoteCatalog = _settings.BuildRemoteCatalog;
            ProfileValueReference previousRemoteCatalogBuildPath = _settings.RemoteCatalogBuildPath;
            ProfileValueReference previousRemoteCatalogLoadPath = _settings.RemoteCatalogLoadPath;
            string previousProfileId = _settings.activeProfileId;

            try
            {
                string loadPath = UniModSpecification.GetAddressablesModLoadPath(modId);
                return SetupAndBuildAddressables(profileSettings, outputFolder, loadPath, out profileId);
            }
            finally
            {
                // restore settings
                _settings.OverridePlayerVersion = previousOverridePlayerVersion;
                _settings.BuildRemoteCatalog = previousBuildRemoteCatalog;
                _settings.RemoteCatalogBuildPath = previousRemoteCatalogBuildPath;
                _settings.RemoteCatalogLoadPath = previousRemoteCatalogLoadPath;
                _settings.activeProfileId = previousProfileId;
                
                // remove the mod profile and all added entries
                profileSettings.RemoveProfile(profileId);
                
                foreach (string guid in _addedGuids)
                    _settings.RemoveAssetEntry(guid);
                
                _addedGuids.Clear();
            }
        }

        private AddressablesPlayerBuildResult SetupAndBuildAddressables(AddressableAssetProfileSettings profileSettings,
            string buildPath, string loadPath, out string profileId)
        {
            // prepare the settings for the build
            _settings.OverridePlayerVersion = UniModSpecification.CatalogName;
            _settings.BuildRemoteCatalog = true;
            _settings.RemoteCatalogBuildPath = _settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().BuildPath;
            _settings.RemoteCatalogLoadPath = _settings.DefaultGroup.GetSchema<BundledAssetGroupSchema>().LoadPath;

            // clean any previous mod profile, create a new one and set it as the active profile
            profileSettings.RemoveProfile(profileSettings.GetProfileId(ModProfileName));
            profileId = profileSettings.AddProfile(ModProfileName, null);
            _settings.activeProfileId = profileId;

            // setup profile local paths
            profileSettings.SetValue(profileId, "Local.BuildPath", buildPath);
            profileSettings.SetValue(profileId, "Local.LoadPath", loadPath);

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            return result;
        }
        
        private void Initialize()
        {
            if (_settings is not null)
                return;
            
            // check if Addressables is initialized within the project and initialize them if not
            if (!AddressableAssetSettingsDefaultObject.SettingsExists)
                AddressableAssetSettingsDefaultObject.Settings = AddressableAssetSettings.Create(AddressableAssetSettingsDefaultObject.kDefaultConfigFolder, AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, true, true);

            _settings = AddressableAssetSettingsDefaultObject.Settings;
        }
    }
}