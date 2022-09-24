using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Katas.UniMod.Editor
{
    [CustomEditor(typeof(ModConfig))]
    public sealed class ModConfigEditor : UnityEditor.Editor
    {
        private static readonly List<string> AssemblyNames = new();
        private static readonly List<string> ManagedPluginPaths = new();
        private static readonly StringBuilder MessageBuilder = new();
        
        private string _includesMessage;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not ModConfig config)
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
            CodeOptimization? cachedBuildMode = config.CachedBuildMode;
            string cachedOutputPath = config.CachedBuildOutputPath;
            if (cachedBuildMode is null || string.IsNullOrEmpty(cachedOutputPath))
                return;
            
            GUILayout.Space(16);
            if (GUILayout.Button("Rebuild"))
                config.BuildModWithGuiAsync(tryRebuild: true).Forget();
            EditorGUILayout.HelpBox($"\nRebuild parameters:\n\n\tBuild mode: {config.CachedBuildMode}\n\tOutput path: {config.CachedBuildOutputPath}\n", MessageType.Info, true);
        }

        private void UpdateIncludesMessage()
        {
            if (target is not ModConfig config)
                return;
            
            // get the assembly names included for the current target platform/configuration and display them in a help box
            AssemblyNames.Clear();
            AssemblyDefinitionIncludesUtility.ResolveIncludedSupportedAssemblyNames(config.assemblyDefinitions,
                EditorUserBuildSettings.activeBuildTarget, AssemblyNames);
            ManagedPluginPaths.Clear();
            ManagedPluginIncludesUtility.ResolveIncludedSupportedManagedPluginPaths(config.managedPlugins,
                EditorUserBuildSettings.activeBuildTarget, ManagedPluginPaths);
            
            // get all the .dll names in a single list, adding a prefix depending on the assembly type
            for (int i = 0; i < AssemblyNames.Count; ++i)
                AssemblyNames[i] = $"Scripting:\t{AssemblyNames[i]}";
            
            foreach (string path in ManagedPluginPaths)
            {
                string assemblyName = Path.GetFileNameWithoutExtension(path);
                
                if (!string.IsNullOrEmpty(assemblyName))
                    AssemblyNames.Add($"Plugin:\t\t{assemblyName}");
            }
            
            // sort the assemblies list and build the includes message for the inspector
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
