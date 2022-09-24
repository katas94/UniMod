using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Allows you to configure includes/excludes for any type of asset. You can resolve the includes using the AssetIncludesUtility class.
    /// </summary>
    [Serializable]
    public partial struct AssetIncludes<T>
        where T : Object
    {
        public bool includeAssetsFolder;
        [Space(5)]
        public List<DefaultAsset> folderIncludes;
        public List<DefaultAsset> folderExcludes;
        [Space(5)]
        public List<T> assetIncludes;
        public List<T> assetExcludes;

        private void OnValidate()
        {
            Debug.Log("test");
        }
    }
}