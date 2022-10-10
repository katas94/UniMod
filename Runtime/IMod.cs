using System;
using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    [Flags]
    public enum ModIncompatibilities
    {
        None          = 0,
        Other         = 1 << 0,
        UniModVersion = 1 << 1,
        Target        = 1 << 2,
        TargetVersion = 1 << 3,
        Platform      = 1 << 4
    }
    
    /// <summary>
    /// Represent an installed mod that can be loaded and started.
    /// </summary>
    public interface IMod
    {
        IModContext Context { get; }
        
        ModInfo Info { get; }
        ModIncompatibilities Incompatibilities { get; }
        bool IsLoaded { get; }
        IResourceLocator ResourceLocator { get; }
        IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        UniTask LoadAsync();
        UniTask<Sprite> LoadThumbnailAsync();
    }
}