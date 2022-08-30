using Cysharp.Threading.Tasks;
using UnityEngine;

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
        string Path { get; }
        ModInfo Info { get; }
        ModStatus Status { get; }
        bool IsContentLoaded { get; }
        bool AreAssembliesLoaded { get; }
        
        UniTask LoadContentAsync();
        UniTask LoadAssembliesAsync();
        UniTask StartAsync();
        UniTask<Sprite> LoadThumbnailAsync();
        void UnloadThumbnail();
    }
}