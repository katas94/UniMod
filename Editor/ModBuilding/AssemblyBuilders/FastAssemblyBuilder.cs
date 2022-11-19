using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Uses the editor precompiled assemblies under the Library/ScriptAssemblies folder. It is quicker since no build is required.
    /// <br/><br/>
    /// Please note that since this uses the editor's precompiled assemblies, the scripting defines will differ from the ones used in
    /// a final platform specific build. For example, UNITY_EDITOR will be defined so any script trying to access Editor only features
    /// may cause errors if testing the assemblies in a player build.
    /// <br/><br/>
    /// Use this builder for fast iteration.
    /// </summary>
    public sealed class FastAssemblyBuilder : IAssemblyBuilder
    {
        public static readonly FastAssemblyBuilder Instance = new();
        
        private const string LibraryScriptAssembliesPath = "Library/ScriptAssemblies";
        
        private FastAssemblyBuilder() { }
        
        public bool SupportsBuildTarget(BuildTarget buildTarget)
            => true;
        
        public UniTask BuildAssembliesAsync(IEnumerable<string> assemblyNames, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            bool isDebugBuild = buildMode is CodeOptimization.Debug;
            return UniTaskUtility.WhenAll(
                assemblyNames.Select(name => CopyAssembly(name, outputFolder, isDebugBuild))
            );
        }

        private static UniTask CopyAssembly(string assemblyName, string outputFolder, bool isDebugBuild)
        {
            string path = Path.Combine(LibraryScriptAssembliesPath, assemblyName + ".dll");
            return UniModEditorUtility.CopyManagedAssemblyAsync(path, outputFolder, isDebugBuild);
        }
    }
}
