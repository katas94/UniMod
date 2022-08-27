using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ModmanEditor
{
    [CustomEditor(typeof(ModConfig))]
    public class ModConfigEditor : Editor
    {
        private static readonly List<string> AssemblyNames = new List<string>();
        private static StringBuilder MessageBuilder = new StringBuilder();
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var config = target as ModConfig;
            if (config is null)
                return;
            
            // get the assembly names included for the current target platform/configuration and display them in a help box
            AssemblyNames.Clear();
            config.GetAllIncludedManagedAssemblyNames(EditorUserBuildSettings.activeBuildTarget, AssemblyNames);
            AssemblyNames.Sort();
            MessageBuilder.Clear();

            if (AssemblyNames.Count == 0)
            {
                MessageBuilder.Append("The current config doesn't include any managed assemblies");
            }
            else
            {
                MessageBuilder.Append("The following managed assemblies will be included with the mod build:\n\n");
                
                foreach (string assemblyName in AssemblyNames)
                    MessageBuilder.Append($"\t{assemblyName}\n");
            }
            
            GUILayout.Space(8);
            EditorGUILayout.HelpBox(MessageBuilder.ToString(), MessageType.Info, true);
            
            // display the tool buttons
            GUILayout.Space(8);
            if (GUILayout.Button("Build mod"))
                ModUtils.BuildModWithGuiAsync(config).Forget();
        }
    }
}
