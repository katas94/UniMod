using Cysharp.Threading.Tasks;

namespace Modman
{
    public interface IModInstance
    {
        ModInfo Info { get; }
        
        UniTask RunAsync();
    }
}