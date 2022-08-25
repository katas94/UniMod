using Cysharp.Threading.Tasks;

namespace Modman
{
    public interface IModService
    {
        UniTask RefreshModsFolder ();
        UniTask LoadAllMods ();
        UniTask LoadMod (string id);
    }
}