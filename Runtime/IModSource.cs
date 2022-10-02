using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModSource
    {
        UniTask FetchAsync();
        
        UniTask GetAllIdsAsync(ICollection<string> results);
        UniTask<IMod> GetModAsync(string modId);
        UniTask GetModsAsync(IEnumerable<string> modIds, ICollection<IMod> results);
        UniTask GetAllModsAsync(ICollection<IMod> results);
    }
}