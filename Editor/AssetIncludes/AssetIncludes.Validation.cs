using System;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    public partial struct AssetIncludes<T>
    {
        /// <summary>
        /// Whether this instance changed after the last Validate call.
        /// </summary>
        public bool Changed { get; private set; }
        
        private AssetListValidator<DefaultAsset> _folderIncludesValidator;
        private AssetListValidator<DefaultAsset> _folderExcludesValidator;
        private AssetListValidator<T> _assetIncludesValidator;
        private AssetListValidator<T> _assetExcludesValidator;
        private bool _lastIncludeAssetsFolderValue;
        
        /// <summary>
        /// Should be called in the OnValidate Unity method.
        /// </summary>
        public void Validate(Func<T, bool> assetValidator = null)
        {
            // initialize list validators if they are not yet
            _folderIncludesValidator ??= new AssetListValidator<DefaultAsset>(folderIncludes, IsFolder);
            _folderExcludesValidator ??= new AssetListValidator<DefaultAsset>(folderExcludes, IsFolder);
            _assetIncludesValidator ??= new AssetListValidator<T>(assetIncludes);
            _assetExcludesValidator ??= new AssetListValidator<T>(assetExcludes);
            
            // validate lists
            _folderIncludesValidator.Validate();
            _folderExcludesValidator.Validate();
            assetValidator ??= IsValidAsset;
            _assetIncludesValidator.Validate(assetValidator);
            _assetExcludesValidator.Validate(assetValidator);
            
            // check if the includes/excludes were modified
            Changed = _lastIncludeAssetsFolderValue != includeAssetsFolder
                || _folderIncludesValidator.ListChanged
                || _assetIncludesValidator.ListChanged
                || _folderExcludesValidator.ListChanged
                || _assetExcludesValidator.ListChanged;
            
            _lastIncludeAssetsFolderValue = includeAssetsFolder;
        }

        private static bool IsFolder(DefaultAsset asset)
        {
            int guid = asset.GetInstanceID();
            string path = AssetDatabase.GetAssetPath(guid);
            return AssetDatabase.IsValidFolder(path);
        }
        
        private static bool IsValidAsset(T asset)
            => true;
    }
}