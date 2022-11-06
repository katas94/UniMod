using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace Katas.UniMod.Editor
{
    public interface IAssemblyBuilder
    {
        bool SupportsBuildTarget(BuildTarget buildTarget);
        
        /// <summary>
        /// Builds the given user defined assemblies into the output folder. The assembly names collection must contain the names of user defined assemblies
        /// within the project.
        /// </summary>
        UniTask BuildAssembliesAsync(IEnumerable<string> assemblyNames, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder);
    }
}