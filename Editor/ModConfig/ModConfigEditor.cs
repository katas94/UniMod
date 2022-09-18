using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Katas.Mango.Editor
{
    [CustomEditor(typeof(ModConfig))]
    public sealed class ModConfigEditor : UnityEditor.Editor
    {
        private static readonly List<string> AssemblyNames = new();
        private static readonly StringBuilder MessageBuilder = new();
        
        private string _includesMessage;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var config = target as ModConfig;
            if (config is null)
                return;
            
            // this prevents inspector lagging if there is a lot of included assemblies (since it would fetch them on each inspector frame update)
            config.IncludesModified -= UpdateIncludesMessage;
            config.IncludesModified += UpdateIncludesMessage;
            
            // display build assembly info and build button
            if (string.IsNullOrEmpty(_includesMessage))
                UpdateIncludesMessage();
            
            GUILayout.Space(8);
            if (GUILayout.Button("Build"))
                config.BuildModWithGuiAsync(tryRebuild: false).Forget();
            EditorGUILayout.HelpBox(_includesMessage, MessageType.Info, true);
            
            // display rebuild cached info and rebuild button (if there is any cached info)
            var cachedBuildMode = config.CachedBuildMode;
            var cachedOutputPath = config.CachedBuildOutputPath;
            if (cachedBuildMode is null || string.IsNullOrEmpty(cachedOutputPath))
                return;
            
            GUILayout.Space(16);
            if (GUILayout.Button("Rebuild"))
                config.BuildModWithGuiAsync(tryRebuild: true).Forget();
            EditorGUILayout.HelpBox($"\nRebuild parameters:\n\n\tBuild mode: {config.CachedBuildMode}\n\tOutput path: {config.CachedBuildOutputPath}\n", MessageType.Info, true);
        }

        private void UpdateIncludesMessage()
        {
            var config = target as ModConfig;
            if (config is null)
                return;
            
            // get the assembly names included for the current target platform/configuration and display them in a help box
            AssemblyNames.Clear();
            config.GetAllIncludes(EditorUserBuildSettings.activeBuildTarget, AssemblyNames);
            AssemblyNames.Sort();
            MessageBuilder.Clear();

            if (AssemblyNames.Count == 0)
            {
                MessageBuilder.Append("The current config doesn't include any managed assemblies");
            }
            else
            {
                MessageBuilder.Append("\nThe following managed assemblies will be included with the mod build:\n\n");
                
                foreach (string assemblyName in AssemblyNames)
                    MessageBuilder.Append($"\t{assemblyName}\n");
            }
            
            _includesMessage = MessageBuilder.ToString();
        }
    }
}
