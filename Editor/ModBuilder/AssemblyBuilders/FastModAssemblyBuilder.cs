using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.ModmanEditor
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
        
        public async UniTask BuildAssembliesAsync (ModConfig config, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            // get managed plugin paths
            _paths.Clear();
            config.GetIncludedManagedPlugins(buildTarget, _paths);
            
            // get user defined assembly names
            _tmpList.Clear();
            config.GetIncludedAssemblies(buildTarget, _tmpList);
            
            // get the paths to the precompiled user defined assemblies
            foreach (string name in _tmpList)
            {
                string path = Path.Combine(LibraryScriptAssembliesPath, name + ".dll");
                
                // if the path does not exist it may be that the user defined assembly has no scripts, so we can just skip it
                if (File.Exists(path))
                    _paths.Add(path);
            }
            
            // copy the assemblies to the output folder
            try
            {
                await UniTask.WhenAll(_paths.Select(path => CopyAssemblyToOutputFolder(path, outputFolder, buildMode)));
                await UniTask.SwitchToMainThread();
            }
            catch (Exception)
            {
                // the copying method executes in background threads so lets make sure to return to the main thread before throwing
                await UniTask.SwitchToMainThread();
                throw;
            }
        }
        
        private async UniTask CopyAssemblyToOutputFolder (string path, string outputFolder, CodeOptimization buildMode)
        {
            await UniTask.SwitchToThreadPool();
            
            if (string.IsNullOrEmpty(outputFolder) || !Directory.Exists(outputFolder))
                throw new Exception("The given output folder is null/empty or it does not exist");
            
            // check if the assembly file exists
            if (!File.Exists(path))
                throw new FileNotFoundException($"Could not find the assembly file at \"{path}\"");
            
            // get the src/dst paths for both the assembly and its pdb (debugging) file
            string fileName = Path.GetFileName(path);
            string dllSrcPath = path;
            string dllDestPath = Path.Combine(outputFolder, fileName);
            string pdbSrcPath = Path.ChangeExtension(dllSrcPath, ".pdb");
            string pdbDestPath = Path.ChangeExtension(dllDestPath, ".pdb");

            // check if the assembly is a valid net managed assembly
            if (!DefaultModBuilder.IsManagedAssembly(dllSrcPath))
                throw new Exception($"\"{dllSrcPath}\" is not a managed assembly");
            
            File.Copy(dllSrcPath, dllDestPath, true);

            // copy pdb file only if in debug mode and the file exists. Some assemblies may not come with a pdb file so we are not requiring it
            if (buildMode == CodeOptimization.Debug && File.Exists(pdbSrcPath))
                File.Copy(pdbSrcPath, pdbDestPath, true);
        }
    }
}
