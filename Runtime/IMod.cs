using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.Mango
{
    public enum ModStatus
    {
        Ok,
        MissingDependencies,
        UnsupportedApp,
        UnsupportedAppVersion,
        UnsupportedPlatform,
        UnsupportedMangoVersion
    }
    
    /// <summary>
    /// Represent an installed mod that can be loaded and started.
    /// </summary>
    public interface IMod
    {
        ModInfo Info { get; }
        ModStatus Status { get; }
        bool IsLoaded { get; }
        IResourceLocator ResourceLocator { get; }
        IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        UniTask LoadAsync(bool loadAssemblies);
        UniTask<bool> UninstallAsync();
        UniTask<Sprite> LoadThumbnailAsync();
    }
}