using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Base class for platform specific assembly builders that will produce the final assemblies with the correct scripting defines and build configuration.
    /// <br/><br/>
    /// Extend this class if you want to build mods with assemblies for custom platforms. You will need to override the <see cref="GetAssembliesFolderFromBuildAsync"/>
    /// method to provide a final path from the build containing the managed assemblies. It has been designed this way since Unity produces build artifacts
    /// differently depending on the target platform. See the <see cref="StandaloneAssemblyBuilder"/> or <see cref="AndroidAssemblyBuilder"/> as examples
    /// of how to extend this class. You can then create a <see cref="CustomAssemblyBuilder"/> implementation to be able to create builder assets that
    /// you can set to a <see cref="LocalModBuilder"/> asset.
    /// </summary>
    public abstract class FinalAssemblyBuilder : IAssemblyBuilder
    {
        public abstract bool SupportsBuildTarget(BuildTarget buildTarget);
        protected abstract UniTask<string> GetAssembliesFolderFromBuildAsync(BuildTarget buildTarget, string buildFolder);
        
        public async UniTask BuildAssembliesAsync(IEnumerable<string> assemblyNames, CodeOptimization buildMode, BuildTarget buildTarget, string outputFolder)
        {
            if (!SupportsBuildTarget(buildTarget))
                throw new Exception($"This assembly builder doesn't support the given build target: {buildTarget}");
            
            // create a tmp folder for the build output
            string tmpFolder = IOUtils.CreateTmpFolder();
            string buildFolder = Path.Combine(tmpFolder, "build");
            bool isDebugBuild = buildMode is CodeOptimization.Debug;

            try
            {
                DoScriptsOnlyPlayerBuild(buildTarget, buildFolder, isDebugBuild);
                
                // get the platform specific folder where all managed assemblies were built
                string assembliesFolder = await GetAssembliesFolderFromBuildAsync(buildTarget, buildFolder);
                
                // copy all the built assemblies to the output folder
                await UniTaskUtility.WhenAll(
                    assemblyNames.Select(name =>
                    {
                        string assemblyPath = Path.Combine(assembliesFolder, name + ".dll");
                        return UniModEditorUtility.CopyManagedAssemblyAsync(assemblyPath, outputFolder, isDebugBuild);
                    })
                );
            }
            catch (Exception exception)
            {
                throw new Exception("Failed to build assemblies", exception);
            }
            finally
            {
                IOUtils.DeleteDirectory(tmpFolder);
            }
        }

        private static void DoScriptsOnlyPlayerBuild(BuildTarget buildTarget, string outputFolder, bool developmentBuild)
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
            
            // IL2CPP scripting backend is not currently supported by UniMod
            ScriptingImplementation scriptingBackend = PlayerSettings.GetScriptingBackend(targetGroup);
            if (scriptingBackend is ScriptingImplementation.IL2CPP)
                throw new Exception("The IL2CPP scripting backend is currently not supported by UniMod, please switch to the Mono backend in PlayerSettings and try again");
            
            BuildOptions options = BuildOptions.BuildScriptsOnly | BuildOptions.CleanBuildCache;
            if (developmentBuild)
                options |= BuildOptions.Development;
            
            var buildPlayerOptions = new BuildPlayerOptions()
            {
                scenes = Array.Empty<string>(),
                locationPathName = outputFolder,
                assetBundleManifestPath = null,
                targetGroup = targetGroup,
                target = buildTarget,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = options,
                extraScriptingDefines = Array.Empty<string>(),
            };
            
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}
