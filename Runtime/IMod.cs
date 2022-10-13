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
        Other         = 1 << 0, // can be used by custom IMod implementations to set a mod incompatible because of other reasons
        UniModVersion = 1 << 1,
        Target        = 1 << 2,
        TargetVersion = 1 << 3,
        Platform      = 1 << 4
    }
    
    /// <summary>
    /// Represents a mod instance available at runtime. The mod can be loaded at any time. Please not that the mod implementation should
    /// not check its own compatibility on the LoadAsync method, so if you don't check the incompatibilities, calling LoadAsync will try to
    /// load the mod anyways. This is done on purpose so you can try to force a mod to load even though its marked as incompatible.
    /// </summary>
    public interface IMod
    {
        ModInfo Info { get; }
        ModIncompatibilities Incompatibilities { get; }
        bool IsLoaded { get; }
        IResourceLocator ResourceLocator { get; }
        IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        UniTask LoadAsync(IModContext context);
        UniTask<Sprite> LoadThumbnailAsync();
    }
}