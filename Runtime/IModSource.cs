using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModSource
    {
        UniTask FetchIdsAsync(ICollection<string> modIds);
        UniTask<IMod> FetchModAsync(string modId);
        UniTask FetchModsAsync(IEnumerable<string> modIds, ICollection<IMod> results);
        UniTask FetchAllModsAsync(ICollection<IMod> results);
    }
}