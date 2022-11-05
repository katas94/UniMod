using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    /// <summary>
    /// A mod source can fetch and provide mod loaders.
    /// </summary>
    public interface IModSource
    {
        /// <summary>
        /// Fetches all available mod loaders from the source. Once fetching has occured, the available mods should stay the same
        /// until fetching is performed again.
        /// </summary>
        UniTask FetchAsync();
        
        UniTask GetAllIdsAsync(ICollection<string> results);
        UniTask<IModLoader> GetLoaderAsync(string id);
        UniTask GetLoadersAsync(IEnumerable<string> ids, ICollection<IModLoader> results);
        UniTask GetAllLoadersAsync(ICollection<IModLoader> results);
    }
}