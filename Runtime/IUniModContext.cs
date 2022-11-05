using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// The UniMod runtime context for managing mods. There should only be one context at runtime.
    /// </summary>
    public interface IUniModContext : IModHost, IModClosure, ILocalModInstaller, IModSourceGroup
    {
        /// <summary>
        /// Automatically installs any mods added to the local installation folder, fetches from all sources and rebuilds the mod closure.
        /// It does not load the mods.
        /// </summary>
        UniTask RefreshAsync();
    }
}