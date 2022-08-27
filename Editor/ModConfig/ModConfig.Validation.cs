using UnityEditor;

namespace ModmanEditor
{
    public partial class ModConfig
    {
        private AssetListValidator<DefaultAsset> _folderIncludesValidator;
        private AssetListValidator<DefaultAsset> _managedPluginIncludesValidator;
        private AssetListValidator<DefaultAsset> _folderExcludesValidator;
        private AssetListValidator<DefaultAsset> _managedPluginExcludesValidator;

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
