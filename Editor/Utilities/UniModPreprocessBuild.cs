using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Katas.UniMod.Editor
{
    public sealed class UniModPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;
        
        public void OnPreprocessBuild(BuildReport report)
        {
            // ensure that all embedded mod configs are properly updated before performing a player build
            UniModEditorUtility.RefreshAndSaveAllEmbeddedModConfigs();
        }
    }
}