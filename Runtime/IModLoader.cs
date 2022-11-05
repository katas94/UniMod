using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    /// <summary>
    /// A mod loader contains information about a mod and provides a method to load its contents and run the mod startup.
    /// </summary>
    public interface IModLoader
    {
        ModInfo Info { get; }
        string Source { get; }
        bool ContainsAssets { get; }
        bool ContainsAssemblies { get; }
        bool IsLoaded { get; }
        IResourceLocator ResourceLocator { get; }
        IReadOnlyList<Assembly> LoadedAssemblies { get; }

        UniTask LoadAsync(IMod mod);
        UniTask<Sprite> GetThumbnailAsync();
    }
}