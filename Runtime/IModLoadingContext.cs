using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// Contains the status of all mods in the context and knows how to load them in the proper order.
    /// </summary>
    public interface IModLoadingContext
    {
        IReadOnlyCollection<IModStatus> Statuses { get; }
        
        IModStatus GetStatus(IMod mod);
        IModStatus GetStatus(string modId);
        
        UniTask<bool> TryLoadAllModsAsync();
        UniTask<bool> TryLoadModsAsync(params IMod[] mods);
        UniTask<bool> TryLoadModsAsync(IEnumerable<IMod> mods);
        UniTask<bool> TryLoadModsAsync(params string[] modIds);
        UniTask<bool> TryLoadModsAsync(IEnumerable<string> modIds);
        UniTask<bool> TryLoadModAsync(IMod mod);
        UniTask<bool> TryLoadModAsync(string modId);
    }
}