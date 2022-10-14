using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModContext : IModLoadingContext, ILocalModInstaller, IModSourceGroup
    {
        IModdableApplication Application { get; }
        IReadOnlyList<IMod> Mods { get; }

        IMod GetMod(string modId);
        UniTask RefreshAsync();
    }
}