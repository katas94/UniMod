using Cysharp.Threading.Tasks;

namespace Katas.Modman
{
    public interface IModService
    {
        UniTask RefreshModsFolderAsync ();
        UniTask LoadAllModsAsync ();
        UniTask LoadModAsync (string id);
    }
}