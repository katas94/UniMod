using UnityEngine;
using UnityEditor;

namespace Katas.UniMod.Editor
{
    [CustomEditor(typeof(EmbeddedModConfig))]
    public sealed class EmbeddedModConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI ()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox($"Embedded configs are automatically updated by a linked {nameof(ModConfig)} asset", MessageType.Info);
            EditorGUILayout.Space();

            GUI.enabled = false;
            base.OnInspectorGUI();
        }
    }
}


