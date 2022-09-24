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
        
        private readonly List<string> _paths = new();
        private readonly List<string> _tmpList = new();
        
        public bool SupportsBuildTarget (BuildTarget buildTarget)
            => true;
        
        public async UniTask BuildAssembliesAsync (ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            // get managed plugin paths
            _paths.Clear();
            ManagedPluginIncludesUtility.ResolveIncludedSupportedManagedPluginPaths(config.managedPlugins, buildTarget, _paths);
            
            // get user defined assembly names
            _tmpList.Clear();
            AssemblyDefinitionIncludesUtility.ResolveIncludedSupportedAssemblyNames(config.assemblyDefinitions, buildTarget, _tmpList);
            
            // get the paths to the precompiled user defined assemblies
            foreach (string name in _tmpList)
            {
                string path = Path.Combine(LibraryScriptAssembliesPath, name + ".dll");
                
                // if the path does not exist it may be that the user defined assembly has no scripts, so we can just skip it
                if (File.Exists(path))
                    _paths.Add(path);
            }
            
            // copy the assemblies to the output folder
            bool isDebugBuild = buildMode == CodeOptimization.Debug;
            await UniTask.WhenAll
            (
                _paths.Select
                (
                    path => ModBuilderUtility.CopyManagedAssemblyToOutputFolder(path, outputFolder, isDebugBuild)
                )
            );
        }
    }
}
