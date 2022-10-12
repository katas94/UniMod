using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModLoader
    {
        IReadOnlyCollection<IModLoadingInfo> AllLoadingInfo { get; }
        
        IModLoadingInfo GetLoadingInfo(IMod mod);
        IModLoadingInfo GetLoadingInfo(string modId);
        
        UniTask<bool> TryLoadAllModsAsync();
        UniTask<bool> TryLoadModsAndDependenciesAsync(params IMod[] mods);
        UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<IMod> mods);
        UniTask<bool> TryLoadModsAndDependenciesAsync(params string[] modIds);
        UniTask<bool> TryLoadModsAndDependenciesAsync(IEnumerable<string> modIds);
        UniTask<bool> TryLoadModAndDependenciesAsync(IMod mod);
        UniTask<bool> TryLoadModAndDependenciesAsync(string modId);
    }
}