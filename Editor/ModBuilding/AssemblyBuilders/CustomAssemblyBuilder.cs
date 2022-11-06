using System.Collections.Generic;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Extend this class to create custom assembly builders.
    /// </summary>
    public abstract class CustomAssemblyBuilder : ScriptableObject, IAssemblyBuilder
    {
        public abstract bool SupportsBuildTarget(BuildTarget buildTarget);
        public abstract UniTask BuildAssembliesAsync(IEnumerable<string> assemblyNames, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder);
    }
}
