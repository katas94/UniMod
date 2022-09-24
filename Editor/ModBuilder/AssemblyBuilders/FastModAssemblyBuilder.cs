using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Uses the editor precompiled assemblies under the Library/ScriptAssemblies folder. It is quicker since no build is required.
    /// 
    /// This builder supports any platform since all assemblies are managed. Still I'm not sure of the exact
    /// build parameters used by Unity for these assemblies. I think they are always compiled in debug mode
    /// so it is preferable to use a platform specific assemblies builder.
    ///
    /// Use this builder for fast iteration.
    /// </summary>
    public sealed class FastModAssemblyBuilder : IModAssemblyBuilder
    {
        private const string LibraryScriptAssembliesPath = "Library/ScriptAssemblies";
        
        public bool SupportsBuildTarget (BuildTarget buildTarget)
            => true;
        
        public async UniTask BuildAssembliesAsync (IEnumerable<string> assemblyNames, IEnumerable<string> managedPluginPaths,
            CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            bool isDebugBuild = buildMode == CodeOptimization.Debug;
            IEnumerable<string> paths = GetAllAssemblyPaths(assemblyNames, managedPluginPaths);
            await UniTask.WhenAll
            (
                paths.Select
                (
                    // copy each assembly to the output folder
                    path => ModBuilderUtility.CopyManagedAssemblyToOutputFolder(path, outputFolder, isDebugBuild)
                )
            );
        }

        private static IEnumerable<string> GetAllAssemblyPaths(IEnumerable<string> assemblyNames, IEnumerable<string> managedPluginPaths)
        {
            // return the paths to the precompiled user defined assemblies
            foreach (string name in assemblyNames)
            {
                string path = Path.Combine(LibraryScriptAssembliesPath, name + ".dll");
                
                // if the path does not exist it may be that the user defined assembly has no scripts, so we can just skip it
                if (File.Exists(path))
                    yield return path;
            }
            
            // return the paths to the managed plugins
            foreach(string path in managedPluginPaths)
                yield return path;
        }
    }
}
