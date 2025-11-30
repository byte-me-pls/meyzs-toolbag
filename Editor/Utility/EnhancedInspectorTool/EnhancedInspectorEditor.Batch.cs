#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Batch operations (reset/copy all) for the enhanced inspector editor.
    /// </summary>
    public partial class EnhancedInspectorEditor
    {
        private void DrawBatchControls()
        {
            GUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚡ Batch Operations", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("🔄 Reset All"))
            {
                if (EditorUtility.DisplayDialog("Reset All Properties",
                        "Are you sure you want to reset all properties to default values?", "Yes", "Cancel"))
                {
                    BatchResetAllProperties();
                }
            }

            if (GUILayout.Button("📋 Copy All"))
            {
                BatchCopyAllProperties();
                EditorUtility.DisplayDialog("Copy Complete", "All properties have been copied to clipboard.", "OK");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void BatchResetAllProperties()
        {
            Undo.RecordObject(target, "Batch Reset All Properties");

            var it = serializedObject.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "m_Script") continue;
                ResetPropertyToDefault(it);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private void BatchCopyAllProperties()
        {
            var it = serializedObject.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.name == "m_Script") continue;
                _actionsManager.CopyProperty(it);
            }
        }
    }
}
#endif