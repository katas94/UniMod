using System;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    public sealed partial class ModConfig
    {
        public event Action IncludesModified;
        
        private AssetListValidator<DefaultAsset> _folderIncludesValidator;
        private AssetListValidator<DefaultAsset> _managedPluginIncludesValidator;
        private AssetListValidator<DefaultAsset> _folderExcludesValidator;
        private AssetListValidator<DefaultAsset> _managedPluginExcludesValidator;
        private bool _lastIncludeAssetsFolderValue;

        private void OnValidate()
        {
            _folderIncludesValidator ??= new AssetListValidator<DefaultAsset>(folderIncludes, IsFolder);
            _managedPluginIncludesValidator ??= new AssetListValidator<DefaultAsset>(managedPluginIncludes, IsManagedPlugin);
            _folderExcludesValidator ??= new AssetListValidator<DefaultAsset>(folderExcludes, IsFolder);
            _managedPluginExcludesValidator ??= new AssetListValidator<DefaultAsset>(managedPluginExcludes, IsManagedPlugin);
            
            _folderIncludesValidator.Validate();
            _managedPluginIncludesValidator.Validate();
            _folderExcludesValidator.Validate();
            _managedPluginExcludesValidator.Validate();
            
            // check if the includes/excludes were modified
            bool includesModified = _lastIncludeAssetsFolderValue != includeAssetsFolder
                || _folderIncludesValidator.ListChanged
                || _managedPluginIncludesValidator.ListChanged
                || _folderExcludesValidator.ListChanged
                || _managedPluginExcludesValidator.ListChanged;
            
            _lastIncludeAssetsFolderValue = includeAssetsFolder;
            
            // fire event if so
            if (includesModified)
                IncludesModified?.Invoke();
        }

        private static bool IsFolder(DefaultAsset asset)
        {
            int guid = asset.GetInstanceID();
            string path = AssetDatabase.GetAssetPath(guid);
            return AssetDatabase.IsValidFolder(path);
        }
        
        private static bool IsManagedPlugin(DefaultAsset asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            
            if (!importer)
                return false;
            
            return !importer.isNativePlugin;
        }
    }
}
