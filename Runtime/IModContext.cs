using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModContext : IModLoader, ILocalModInstaller, IModSourceGroup, IModCompatibilityChecker
    {
        IReadOnlyList<IMod> Mods { get; }

        IMod GetMod(string modId);
        UniTask RefreshAsync();
    }
}