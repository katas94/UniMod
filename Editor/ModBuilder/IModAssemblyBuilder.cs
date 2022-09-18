using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace Katas.Mango.Editor
{
    public interface IModAssemblyBuilder
    {
        bool SupportsBuildTarget (BuildTarget buildTarget);
        UniTask BuildAssembliesAsync (ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder);
    }
}