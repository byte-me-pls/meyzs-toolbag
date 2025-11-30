#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Transform
{
    public static partial class PivotChangeTool
    {
        private static void LoadSettings()
        {
            var s = EditorPrefs.GetString(SETTINGS_KEY, "");
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    var p = s.Split('|');
                    if (p.Length >= 8)
                    {
                        preserveChildPositions = bool.Parse(p[0]);
                        updateColliders        = bool.Parse(p[1]);
                        createBackup           = bool.Parse(p[2]);
                        showPivotPreview       = bool.Parse(p[3]);
                        showBounds             = bool.Parse(p[4]);
                        showOriginalPivot      = bool.Parse(p[5]);
                        handleSize             = float.Parse(p[6]);
                        ColorUtility.TryParseHtmlString(p[7], out pivotPreviewColor);
                    }
                }
                catch { /* defaults */ }
            }

            LoadPresets();
        }

        private static void SaveSettings()
        {
            var data = string.Join("|", new string[]
            {
                preserveChildPositions.ToString(),
                updateColliders.ToString(),
                createBackup.ToString(),
                showPivotPreview.ToString(),
                showBounds.ToString(),
                showOriginalPivot.ToString(),
                handleSize.ToString(),
                ColorUtility.ToHtmlStringRGBA(pivotPreviewColor)
            });
            EditorPrefs.SetString(SETTINGS_KEY, data);
        }
    }
}
#endif