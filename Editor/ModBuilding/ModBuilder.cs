using UnityEngine;
using UnityEditor.Compilation;
using Cysharp.Threading.Tasks;

namespace Katas.UniMod.Editor
{
    public abstract class ModBuilder : ScriptableObject
    {
        /// <summary>
        /// Builds the mod with the specified parameters.
        /// </summary>
        public abstract UniTask BuildAsync (ModConfig config, CodeOptimization buildMode, string outputPath);
        
        /// <summary>
        /// Builds the mod for development with the specified parameters. The build artifacts will not be archived
        /// and you can selectively skip building assets or scripts.
        /// </summary>
        public abstract UniTask BuildForDevelopmentAsync (ModConfig config, CodeOptimization buildMode, string outputFolder,
            bool skipAssemblies = false, bool skipAssets = false);
    }
}
