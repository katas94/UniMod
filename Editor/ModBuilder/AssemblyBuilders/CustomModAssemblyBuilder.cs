using System.Collections.Generic;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Extend this class to create custom assembly builders that can be configured in the ModBuilder.
    /// </summary>
    public abstract class CustomModAssemblyBuilder : ScriptableObject, IModAssemblyBuilder
    {
        public abstract bool SupportsBuildTarget (BuildTarget buildTarget);
        public abstract UniTask BuildAssembliesAsync (IEnumerable<string> assemblyNames, IEnumerable<string> managedPluginPaths,
            CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder);
    }
}
