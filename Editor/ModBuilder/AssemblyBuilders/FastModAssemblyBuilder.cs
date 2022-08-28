using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace ModmanEditor
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
    public class FastModAssemblyBuilder : IModAssemblyBuilder
    {
        public const string LibraryScriptAssembliesPath = "Library/ScriptAssemblies";
        
        private readonly List<string> _paths = new();
        private readonly List<string> _tmpList = new();
        
        public bool SupportsBuildTarget (BuildTarget buildTarget)
            => true;
        
        public UniTask<string[]> BuildAssembliesAsync (ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget)
        {
            // get managed plugin paths
            _paths.Clear();
            config.GetIncludedManagedPlugins(buildTarget, _paths);
            
            // get user defined assembly names
            _tmpList.Clear();
            config.GetIncludedAssemblies(buildTarget, _tmpList);
            
            // get the paths to the precompiled assemblies
            foreach (string name in _tmpList)
            {
                string path = Path.Combine(LibraryScriptAssembliesPath, name + ".dll");
                
                if (File.Exists(path))
                    _paths.Add(path);
            }
                
            var result = UniTask.FromResult(_paths.ToArray());
            _paths.Clear();
            return result;
        }
    }
}
