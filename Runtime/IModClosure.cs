using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// A mod closure is a closed group of mods that can have dependencies with each other. A mod closure is also responsible
    /// of loading a mod with its dependencies in the correct order.
    /// </summary>
    public interface IModClosure
    {
        IReadOnlyCollection<IMod> Mods { get; }
        
        IMod GetMod(string id);
        
        UniTask<bool> TryLoadAllModsAsync();
        UniTask<bool> TryLoadModsAsync(params string[] ids);
        UniTask<bool> TryLoadModsAsync(IEnumerable<string> ids);
        UniTask<bool> TryLoadModAsync(string id);
    }
}