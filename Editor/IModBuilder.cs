using Cysharp.Threading.Tasks;

namespace ModmanEditor
{
    public interface IModBuilder
    {
        UniTask BuildAsync();
    }
}