using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    public class EmbeddedMod : IMod
    {
        public ModInfo Info { get; }
        public bool IsLoaded => true;
        public bool ContainsAssets { get; }
        public bool ContainsAssemblies { get; }
        public IResourceLocator ResourceLocator { get; }
        public IReadOnlyList<Assembly> LoadedAssemblies { get; }

        public EmbeddedMod(ModInfo info)
        {
            
        }
        
        public UniTask LoadAsync(IModContext context)
        {
            throw new System.NotImplementedException();
        }

        public UniTask<Sprite> LoadThumbnailAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}