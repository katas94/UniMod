using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.Modman
{
    public enum ModStatus
    {
        Ok,
        MissingDependencies,
        UnsupportedApp,
        UnsupportedAppVersion,
        UnsupportedPlatform,
        UnsupportedModmanVersion
    }
    
    /// <summary>
    /// Represent an installed mod that can be loaded and started.
    /// </summary>
    public interface IMod
    {
        ModInfo Info { get; }
        ModStatus Status { get; }
        bool IsLoaded { get; }
        bool AreAssembliesLoaded { get; }
        IResourceLocator ResourceLocator { get; }
        IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        UniTask LoadAsync(bool loadAssemblies);
        UniTask UninstallAsync();
        UniTask<Sprite> LoadThumbnailAsync();
    }
}