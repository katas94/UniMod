using System.IO;
using System.IO.Compression;
using Cysharp.Threading.Tasks;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Assembly builder for the Android platform.
    /// </summary>
    public sealed class AndroidAssemblyBuilder : FinalAssemblyBuilder
    {
        public static readonly AndroidAssemblyBuilder Instance = new();
        
        private AndroidAssemblyBuilder() { }
        
        public override bool SupportsBuildTarget(BuildTarget buildTarget)
            => buildTarget is BuildTarget.Android;

        protected override UniTask<string> GetAssembliesFolderFromBuildAsync(BuildTarget buildTarget, string buildFolder)
        {
            // Android builds produce a single archive file that we must extract so we can access the assemblies folder
            string parentFolder = Directory.GetParent(buildFolder)?.FullName;
            ZipFile.ExtractToDirectory(buildFolder, Path.Combine(parentFolder));
            return UniTask.FromResult(Path.Combine(parentFolder, "assets", "bin", "Data", "Managed"));
        }
    }
}
