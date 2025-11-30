#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class RandomAreaDuplicationTool
    {
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!sceneGUIActive) return;

            // keep preview fresh
            UpdatePreviewPositions();

            // area circle
            Handles.color = new Color(0, 1, 1, 0.3f);
            Handles.DrawWireDisc(center, Vector3.up, radius);

            // center handle
            EditorGUI.BeginChangeCheck();
            Vector3 newCenter = Handles.PositionHandle(center, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                center = newCenter;
                centerManuallySet = true;
                lastRadius = -1;
                SceneView.RepaintAll();
            }

            // radius handle
            EditorGUI.BeginChangeCheck();
            Vector3 radiusPoint = center + Vector3.right * radius;
            float handleSize = HandleUtility.GetHandleSize(radiusPoint) * 0.1f;
            Vector3 newRadiusPoint = Handles.FreeMoveHandle(radiusPoint, handleSize, Vector3.zero, Handles.DotHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                radius = Mathf.Max(0.1f, Vector3.Distance(center, newRadiusPoint));
                lastRadius = -1;
                SceneView.RepaintAll();
            }

            // radius line
            Handles.color = Color.yellow;
            Handles.DrawLine(center, radiusPoint);

            // preview points
            if (previewPositions.Count > 0)
            {
                for (int i = 0; i < previewPositions.Count; i++)
                {
                    Vector3 previewPos = previewPositions[i];

                    if (snapToSurface && Physics.Raycast(previewPos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                        previewPos = hit.point;

                    bool hasConflict = false;
                    if (avoidOverlap && minDistance > 0.01f)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (Vector3.Distance(previewPos, previewPositions[j]) < minDistance) { hasConflict = true; break; }
                        }
                    }

                    Handles.color = hasConflict ? Color.red : Color.green;
                    float previewSize = HandleUtility.GetHandleSize(previewPos) * 0.05f;
                    Handles.SphereHandleCap(0, previewPos, Quaternion.identity, previewSize, EventType.Repaint);

                    if (i < 5 && avoidOverlap && minDistance > 0.01f)
                    {
                        Handles.color = new Color(1, 0.5f, 0, 0.1f);
                        Handles.DrawWireDisc(previewPos, Vector3.up, minDistance);
                    }
                }

                // info
                Handles.color = Color.white;
                string info = $"Points: {previewPositions.Count}/{count}";
                if (avoidOverlap)
                {
                    float averageDistance = 0f; int distanceCount = 0;
                    for (int i = 0; i < previewPositions.Count; i++)
                        for (int j = i + 1; j < previewPositions.Count; j++)
                        { averageDistance += Vector3.Distance(previewPositions[i], previewPositions[j]); distanceCount++; }
                    if (distanceCount > 0)
                    {
                        averageDistance /= distanceCount;
                        info += $"\nAvg Distance: {averageDistance:F1}";
                        info += $"\nMin Distance: {minDistance:F1}";
                    }
                }
                Handles.Label(center + Vector3.up * 2, info, EditorStyles.boldLabel);
            }

            sceneView.Repaint();
        }
    }
}
#endif
