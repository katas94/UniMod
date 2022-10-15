using UnityEngine;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    [CustomEditor(typeof(EmbeddedModConfig))]
    public class EmbeddedModConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI ()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"This config is automatically updated by a linked {nameof(ModConfig)} asset", MessageType.Info);
            EditorGUILayout.Space();

            GUI.enabled = false;
            base.OnInspectorGUI();
        }
    }
}


