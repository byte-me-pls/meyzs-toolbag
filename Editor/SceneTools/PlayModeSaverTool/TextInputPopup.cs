#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    // Simple text input popup window
    public class TextInputPopup : EditorWindow
    {
        public string message = "";
        public string inputText = "";
        private bool result = false;

        public bool ShowModalUtility()
        {
            ShowUtility();

            var rect = new Rect(Screen.width * 0.5f - 200, Screen.height * 0.5f - 50, 400, 100);
            position = rect;

            return result;
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField(message);
            GUILayout.Space(5);

            GUI.SetNextControlName("InputField");
            inputText = EditorGUILayout.TextField(inputText);

            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("InputField");
            }

            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("OK") || (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown))
            {
                result = !string.IsNullOrEmpty(inputText);
                Close();
            }

            if (GUILayout.Button("Cancel") || (Event.current.keyCode == KeyCode.Escape && Event.current.type == EventType.KeyDown))
            {
                result = false;
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif