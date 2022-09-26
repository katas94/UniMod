using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModRegistry
    {
        int ModCount { get; }
        IEnumerable<string> ModIds { get; }
        IEnumerable<IMod> Mods { get; }
        
        IMod GetMod(string modId);
        bool TryRegisterMod(IMod mod, out string error);
        bool TryUnregisterMod(string modId, out string error);
        UniTask RefreshAsync();
    }
}