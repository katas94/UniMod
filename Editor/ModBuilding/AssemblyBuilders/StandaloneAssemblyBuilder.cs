using System.IO;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Assembly builder for all the standalone platforms.
    /// </summary>
    public sealed class StandaloneAssemblyBuilder : FinalAssemblyBuilder
    {
        public static readonly StandaloneAssemblyBuilder Instance = new();
        
        private StandaloneAssemblyBuilder() { }
        
        public override bool SupportsBuildTarget(BuildTarget buildTarget)
            => buildTarget is
                BuildTarget.StandaloneWindows or
                BuildTarget.StandaloneWindows64 or
                BuildTarget.StandaloneLinux64 or
                BuildTarget.StandaloneOSX;

        protected override UniTask<string> GetAssembliesFolderFromBuildAsync(BuildTarget buildTarget, string buildFolder)
        {
            if (buildTarget is BuildTarget.StandaloneOSX)
                return UniTask.FromResult(Path.Combine(buildFolder + ".app", "Contents", "Resources", "Data", "Managed"));
            
            // standalone windows and linux builds produce the same folder structure for the managed assemblies
            return UniTask.FromResult(Path.Combine(buildFolder + "_Data", "Managed"));
        }
    }
}
