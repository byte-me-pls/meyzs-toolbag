#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Transform
{
    public static partial class PivotChangeTool
    {
        // Presets IO
        private static void LoadPresets()
        {
            pivotPresets.Clear();
            var raw = EditorPrefs.GetString(PRESETS_KEY, "");
            if (string.IsNullOrEmpty(raw)) return;

            var entries = raw.Split('|');
            foreach (var e in entries)
            {
                var parts = e.Split(';');
                if (parts.Length == 4)
                {
                    var preset = new PivotPreset(
                        parts[0],
                        ParseVector3(parts[1]),
                        (PivotPosition)System.Enum.Parse(typeof(PivotPosition), parts[2]),
                        bool.Parse(parts[3])
                    );
                    pivotPresets.Add(preset);
                }
            }
        }

        private static void SavePresets()
        {
            var entries = pivotPresets.Select(p => $"{p.name};{Vector3ToString(p.position)};{p.pivotType};{p.isWorldSpace}");
            EditorPrefs.SetString(PRESETS_KEY, string.Join("|", entries));
        }

        // Domain reload cleanup
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            lastSelection = null;
            cachedCombinedBounds = null;
        }

        // Public API
        public static void SetPivotToPosition(Vector3 worldPosition)
        {
            customPivotPos = worldPosition;
            if (!isPivotEditing) StartPivotEditing();
            SceneView.RepaintAll();
        }

        public static void SetPivotMode(PivotMode mode) => currentPivotMode = mode;

        public static void QuickApplyPivot(PivotPosition position) => ApplyPivotPosition(position);
    }
}
#endif