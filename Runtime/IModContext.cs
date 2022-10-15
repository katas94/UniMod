using Cysharp.Threading.Tasks;

namespace Katas.UniMod
{
    public interface IModContext : IModClosure, ILocalModInstaller, IModSourceGroup
    {
        string ApplicationId { get; }
        string ApplicationVersion { get; }

        UniTask RefreshAsync();
    }
}