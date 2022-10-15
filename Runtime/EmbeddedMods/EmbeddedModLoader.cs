using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    public class EmbeddedModLoader : IModLoader
    {
        public ModInfo Info { get; }
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public bool IsLoaded => true;
        public IResourceLocator ResourceLocator { get; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; }

        public EmbeddedModLoader()
        {
            
        }
        
        public UniTask LoadAsync(IModContext context, IMod mod)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<Texture2D> LoadThumbnailAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}