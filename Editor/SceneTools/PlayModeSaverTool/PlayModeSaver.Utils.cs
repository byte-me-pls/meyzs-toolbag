#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        // Utility methods
        private static string GetGameObjectPath(GameObject go)
        {
            if (go.transform.parent == null)
                return go.name;
            return GetGameObjectPath(go.transform.parent.gameObject) + "/" + go.name;
        }

        private static Texture2D MakeColorTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void ShowToast(string message)
        {
            if (settings?.showToastNotifications == true)
            {
                Debug.Log($"PlayModeSaver: {message}");
            }
        }

        // Text input dialog
        private static bool ShowTextInputDialog(string title, string message, ref string text)
        {
            var popup = ScriptableObject.CreateInstance<TextInputPopup>();
            popup.titleContent = new GUIContent(title);
            popup.message = message;
            popup.inputText = text;

            var result = popup.ShowModalUtility();
            text = popup.inputText;

            return result;
        }

        // Shortcut handlers
        [MenuItem("MeyzToolbag/PlayMode/Quick Save Snapshot %&s")]
        public static void QuickSaveShortcut()
        {
            if (Application.isPlaying)
            {
                CaptureSnapshot($"Quick Save {System.DateTime.Now:HH:mm:ss}");
            }
            else
            {
                Debug.LogWarning("PlayModeSaver: Quick save only works in Play Mode");
            }
        }
    }
}
#endif