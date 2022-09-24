using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace Katas.UniMod.Editor
{
    public interface IModAssemblyBuilder
    {
        bool SupportsBuildTarget (BuildTarget buildTarget);
        UniTask BuildAssembliesAsync (IEnumerable<string> assemblyNames, IEnumerable<string> managedPluginPaths,
            CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder);
    }
}