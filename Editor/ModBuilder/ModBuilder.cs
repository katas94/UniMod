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
    }
}
