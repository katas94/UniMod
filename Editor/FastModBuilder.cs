using System.IO;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ModmanEditor
{
    /// <summary>
    /// Uses the editor precompiled assemblies. It is quicker since those assemblies should be allways up to
    /// date so there is no need to compile.
    /// 
    /// This builder should support any platform, but any platform-specific mod builder should be preferred.
    /// </summary>
    public class FastModBuilder : ModBuilder
    {
        public FastModBuilder (ModDefinition definition, CodeOptimization buildTarget, string outputPath)
            : base(definition, buildTarget, outputPath) { }

        protected override UniTask RegisterAssemblies (string[] assemblyNames)
        {
            // use the editor precompiled assemblies from the Library/ScriptAssemblies folder
            foreach (string assembly in assemblyNames)
                RegisterAssembly(Path.Combine("Library/ScriptAssemblies", assembly + ".dll"), false);

            return default;
        }

        protected override bool SupportsPlatform (RuntimePlatform platform)
            => true;
    }
}
