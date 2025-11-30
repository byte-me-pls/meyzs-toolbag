#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Transform
{
    public static partial class PivotChangeTool
    {
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!sceneGUIActive) return;

            var selected = Selection.gameObjects;
            if (selected.Length == 0) return;

            if (showBounds) DrawObjectBounds(selected);
            if (showOriginalPivot) DrawOriginalPivots(selected);
            if (isPivotEditing) DrawPivotEditingGUI();
            if (showPivotPreview && !isPivotEditing) DrawPivotPreview(selected);
        }

        private static void DrawObjectBounds(GameObject[] objects)
        {
            Handles.color = Color.cyan;
            foreach (var go in objects)
            {
                var r = go.GetComponent<Renderer>();
                if (!r) continue;
                Handles.DrawWireCube(r.bounds.center, r.bounds.size);
            }

            if (currentPivotMode == PivotMode.Combined && objects.Length > 1)
            {
                Handles.color = Color.magenta;
                var cb = GetCombinedBounds(objects);
                Handles.DrawWireCube(cb.center, cb.size);
            }
        }

        private static void DrawOriginalPivots(GameObject[] objects)
        {
            Handles.color = Color.red;
            foreach (var go in objects)
            {
                var p = go.transform.position;
                Handles.DrawWireDisc(p, Vector3.up, 0.1f * handleSize);
                Handles.DrawWireDisc(p, Vector3.right, 0.1f * handleSize);
                Handles.DrawWireDisc(p, Vector3.forward, 0.1f * handleSize);
                Handles.Label(p + Vector3.up * 0.2f, "Original", EditorStyles.miniLabel);
            }
        }

        private static void DrawPivotEditingGUI()
        {
            EditorGUI.BeginChangeCheck();

            Handles.color = pivotPreviewColor;
            var size = HandleUtility.GetHandleSize(customPivotPos) * 0.15f * handleSize;
            var newPos = Handles.PositionHandle(customPivotPos, Quaternion.identity);

            Handles.SphereHandleCap(0, customPivotPos, Quaternion.identity, size, EventType.Repaint);

            Handles.color = Color.red;   Handles.DrawLine(customPivotPos, customPivotPos + Vector3.right   * size * 2);
            Handles.color = Color.green; Handles.DrawLine(customPivotPos, customPivotPos + Vector3.up      * size * 2);
            Handles.color = Color.blue;  Handles.DrawLine(customPivotPos, customPivotPos + Vector3.forward * size * 2);

            Handles.color = Color.white;
            var labelStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Color.white } };
            Handles.Label(customPivotPos + Vector3.up * size * 3, $"New Pivot\n{customPivotPos:F2}", labelStyle);

            if (EditorGUI.EndChangeCheck()) { customPivotPos = newPos; SceneView.RepaintAll(); }
            HandleSceneViewShortcuts();
        }

        private static void DrawPivotPreview(GameObject[] objects)
        {
            var evt = Event.current;
            if (evt.type != EventType.MouseMove && evt.type != EventType.Repaint) return;

            Handles.color = new Color(pivotPreviewColor.r, pivotPreviewColor.g, pivotPreviewColor.b, 0.5f);

            if (currentPivotMode == PivotMode.Combined && objects.Length > 1)
            {
                var b = GetCombinedBounds(objects);
                DrawPivotPositionPreviews(b);
            }
            else
            {
                foreach (var go in objects)
                {
                    var r = go.GetComponent<Renderer>();
                    if (r) DrawPivotPositionPreviews(r.bounds);
                }
            }
        }

        private static void DrawPivotPositionPreviews(Bounds b)
        {
            var positions = new[]
            {
                GetPivotPosition(b, PivotPosition.Center),
                GetPivotPosition(b, PivotPosition.Bottom),
                GetPivotPosition(b, PivotPosition.Top),
                GetPivotPosition(b, PivotPosition.Left),
                GetPivotPosition(b, PivotPosition.Right),
                GetPivotPosition(b, PivotPosition.Front),
                GetPivotPosition(b, PivotPosition.Back),
            };

            var size = HandleUtility.GetHandleSize(b.center) * 0.05f;
            for (int i = 0; i < positions.Length; i++)
                Handles.SphereHandleCap(0, positions[i], Quaternion.identity, size, EventType.Repaint);
        }

        private static void HandleSceneViewShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown) return;

            switch (e.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    ApplyCustomPivot();
                    isPivotEditing = false;
                    e.Use();
                    break;
                case KeyCode.Escape:
                    isPivotEditing = false;
                    e.Use();
                    break;
                case KeyCode.G:
                    if (e.control)
                    {
                        customPivotPos = SnapToGrid(customPivotPos);
                        e.Use();
                    }
                    break;
                case KeyCode.V:
                    if (e.control)
                    {
                        SnapToNearestVertex();
                        e.Use();
                    }
                    break;
            }
        }
    }
}
#endif
