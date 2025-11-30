#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class CircularDuplicationTool
    {
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!sceneGUIActive) return;

            var currentSelection = Selection.gameObjects;
            var valid = System.Array.FindAll(currentSelection, go => go && !IsGeneratedObject(go));

            GameObject[] objectsToUse = null;
            if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0) objectsToUse = sourceObjects;
            else if (valid.Length > 0) objectsToUse = valid;

            if (objectsToUse == null || objectsToUse.Length == 0) return;

            if (showCenterHandle)  DrawCenterHandle();
            if (showRadiusHandle)  DrawRadiusHandle();

            DrawCircularPreview();
            sceneView.Repaint();
        }

        private static void DrawCenterHandle()
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.PositionHandle(dataHolder.center, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataHolder, "Adjust Center Handle");
                dataHolder.center = newCenter;
                centerManuallySet = true;
                EditorWindow.focusedWindow?.Repaint();
                SceneView.RepaintAll();
            }

            Handles.color = Color.cyan;
            float size = HandleUtility.GetHandleSize(dataHolder.center) * 0.1f;
            Handles.CubeHandleCap(0, dataHolder.center, Quaternion.identity, size, EventType.Repaint);
            Handles.Label(dataHolder.center + Vector3.up * 0.5f, "CENTER", EditorStyles.boldLabel);
        }

        private static void DrawRadiusHandle()
        {
            float startRad = dataHolder.startAngle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(startRad), 0, Mathf.Sin(startRad));
            Vector3 pt = dataHolder.center + dir * dataHolder.radius;

            EditorGUI.BeginChangeCheck();
            float hsize = HandleUtility.GetHandleSize(pt) * 0.1f;
            Vector3 newPt = Handles.FreeMoveHandle(pt, hsize, Vector3.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataHolder, "Adjust Radius Handle");
                float newRadius = Vector3.Distance(dataHolder.center, newPt);
                dataHolder.radius = Mathf.Max(0.1f, newRadius);
                EditorWindow.focusedWindow?.Repaint();
                SceneView.RepaintAll();
            }

            Handles.color = Color.cyan;
            Handles.DrawLine(dataHolder.center, pt);
            Handles.Label(pt + Vector3.up * 0.3f, $"R: {dataHolder.radius:F1}", EditorStyles.boldLabel);
        }

        private static void DrawCircularPreview()
        {
            Handles.color = new Color(1, 1, 0, 0.3f);
            Handles.DrawWireDisc(dataHolder.center, Vector3.up, dataHolder.radius);

            if (dataHolder.startAngle != 0 || dataHolder.endAngle != 360)
            {
                Handles.color = Color.yellow;
                Vector3 startDir = new Vector3(Mathf.Cos(dataHolder.startAngle * Mathf.Deg2Rad), 0,
                    Mathf.Sin(dataHolder.startAngle * Mathf.Deg2Rad));
                Vector3 endDir = new Vector3(Mathf.Cos(dataHolder.endAngle * Mathf.Deg2Rad), 0,
                    Mathf.Sin(dataHolder.endAngle * Mathf.Deg2Rad));

                Handles.DrawLine(dataHolder.center, dataHolder.center + startDir * dataHolder.radius);
                Handles.DrawLine(dataHolder.center, dataHolder.center + endDir * dataHolder.radius);
                Handles.DrawWireArc(dataHolder.center, Vector3.up, startDir,
                    dataHolder.endAngle - dataHolder.startAngle, dataHolder.radius);
            }

            Handles.color = Color.blue;
            for (int i = 0; i < dataHolder.count; i++)
            {
                float t = dataHolder.count > 1 ? i / (float)(dataHolder.count - 1) : 0f;
                float angle = Mathf.Lerp(dataHolder.startAngle, dataHolder.endAngle, t) * Mathf.Deg2Rad;
                Vector3 pos = dataHolder.center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dataHolder.radius;

                if (snapToSurface && Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                    pos = hit.point;

                float sz = HandleUtility.GetHandleSize(pos) * 0.05f;
                Handles.CubeHandleCap(0, pos, Quaternion.identity, sz, EventType.Repaint);
                Handles.Label(pos + Vector3.up * 0.3f, $"{i}", EditorStyles.boldLabel);
            }
        }
    }
}
#endif
