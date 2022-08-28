using Cysharp.Threading.Tasks;

namespace Modman
{
    public interface IModService
    {
        UniTask RefreshModsFolderAsync ();
        UniTask LoadAllModsAsync ();
        UniTask LoadModAsync (string id);
    }
}