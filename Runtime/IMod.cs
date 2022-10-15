using System.Collections.Generic;
using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Katas.UniMod
{
    /// <summary>
    /// Represents a mod that is loaded or can be loaded. It contains all the mod information including its dependencies and any
    /// issues that can prevent the mod from loading. Loading a IMod instance directly will try to force load the mod by skipping
    /// any checks for issues or missing/unsupported dependencies, so it is recommended to load the mods from a mod closure or context
    /// instead.
    /// </summary>
    public interface IMod
    {
        string Id { get; }
        string Version { get; }
        string DisplayName { get; }
        string Description { get; }
        bool ContainsAssets { get; }
        bool ContainsAssemblies { get; }
        ModTargetInfo Target { get; }
        
        /// <summary>
        /// Issues that can cause the mod to not load properly.
        /// </summary>
        ModIssues Issues { get; }
        
        /// <summary>
        /// Mod's direct dependencies (doesn't include missing dependencies for obvious reasons).
        /// </summary>
        IReadOnlyCollection<IMod> Dependencies { get; }
        
        /// <summary>
        /// Mod's missing direct dependencies.
        /// </summary>
        IReadOnlyCollection<ModReference> MissingDependencies { get; }
        
        /// <summary>
        /// Whether the mod is currently loaded or not.
        /// </summary>
        bool IsLoaded { get; }
        
        /// <summary>
        /// Resource locator for all the mod loaded assets (will have no key if the mod is not loaded).
        /// </summary>
        IResourceLocator ResourceLocator { get; }
        
        /// <summary>
        /// Contains all the assemblies loaded by the mod (will be empty if the mod is not loaded).
        /// </summary>
        IReadOnlyList<Assembly> LoadedAssemblies { get; }
        
        /// <summary>
        /// Loads the mod. This method will not perform any checks regarding issues or missing dependencies, so you should call it only if
        /// you want to try a forced load. To properly load a mod with automatic dependency loading and issues check use a mod context or
        /// a mod closure implementation.
        /// </summary>
        UniTask LoadAsync(IModContext context);
        
        /// <summary>
        /// Loads the mod's thumbnail as a texture object. Each call should return a new texture object so you should destroy it when no longer using it.
        /// </summary>
        UniTask<Texture2D> LoadThumbnailAsync();
        
        /// <summary>
        /// Populates the results collection with the dependencies causing the specified issues. Issues can include multiple flags.
        /// Not all issues are related with dependencies, like the ones regarding application support, those will never produce results.
        /// </summary>
        void GetDependenciesRelatedToIssues(ModIssues issues, ICollection<IMod> results);
    }
}