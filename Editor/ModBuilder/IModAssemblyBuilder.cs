using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace ModmanEditor
{
    public interface IModAssemblyBuilder
    {
        bool SupportsBuildTarget (BuildTarget buildTarget);
        UniTask<string[]> BuildAssembliesAsync (ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget);
    }
}