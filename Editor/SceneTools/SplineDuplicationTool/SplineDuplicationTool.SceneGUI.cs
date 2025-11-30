#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SplineDuplicationTool
    {
        private static void OnSceneGUI(SceneView sceneView)
        {
            if (!sceneGUIActive) return;

            // path lines
            Handles.color = Color.yellow;
            for (int i = 1; i < splinePoints.Count; i++)
                Handles.DrawLine(splinePoints[i - 1], splinePoints[i]);
            if (loop && splinePoints.Count > 2)
                Handles.DrawLine(splinePoints[^1], splinePoints[0]);

            // points (bigger)
            for (int i = 0; i < splinePoints.Count; i++)
            {
                Handles.color = (i == selectedPoint) ? Color.red : Color.cyan;
                float size = HandleUtility.GetHandleSize(splinePoints[i]) * 0.12f;
                if (Handles.Button(splinePoints[i], Quaternion.identity, size, size, Handles.SphereHandleCap))
                    selectedPoint = i;

                if (editMode && i == selectedPoint)
                {
                    EditorGUI.BeginChangeCheck();
                    var np = Handles.PositionHandle(splinePoints[i], Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(dataHolder, "Move Spline Point");
                        splinePoints[i] = np;
                    }
                }

                Handles.color = Color.white;
                Handles.Label(splinePoints[i] + Vector3.up * 0.5f, $"P{i}", EditorStyles.boldLabel);
            }

            // preview samples (blue squares + links)
            var samples = SampleSpline();
            Handles.color = Color.blue;
            for (int i = 0; i < samples.Count; i++)
            {
                float size = HandleUtility.GetHandleSize(samples[i].position) * 0.08f;
                Handles.CubeHandleCap(0, samples[i].position, Quaternion.identity, size, EventType.Repaint);

                Handles.color = Color.white;
                Handles.Label(samples[i].position + Vector3.up * 0.3f, $"{i + 1}", EditorStyles.boldLabel);

                if (i > 0)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawLine(samples[i - 1].position, samples[i].position);
                }
            }

            // highlight sources
            if (useLockedSources && sourceObjects != null)
            {
                foreach (var src in sourceObjects)
                {
                    if (!src) continue;
                    Handles.color = Color.red;
                    float osize = HandleUtility.GetHandleSize(src.transform.position) * 0.15f;
                    Handles.CubeHandleCap(0, src.transform.position, Quaternion.identity, osize, EventType.Repaint);
                    Handles.Label(src.transform.position + Vector3.up * 1f, "ORIG", EditorStyles.boldLabel);
                }
            }

            HandleInput();
            sceneView.Repaint();
        }

        private static void HandleInput()
        {
            var e = Event.current;
            if (!editMode || e.type != EventType.MouseDown || e.button != 0) return;

            var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 pos;
            if (snapToSurface && Physics.Raycast(ray, out var hit, Mathf.Infinity, snapLayers))
                pos = hit.point;
            else if (new Plane(Vector3.up, 0).Raycast(ray, out var d))
                pos = ray.GetPoint(d);
            else return;

            if (e.control && selectedPoint >= 0)
            {
                Undo.RecordObject(dataHolder, "Remove Spline Point");
                splinePoints.RemoveAt(selectedPoint);
                selectedPoint = -1;
            }
            else
            {
                Undo.RecordObject(dataHolder, "Add Spline Point");
                splinePoints.Add(pos);
            }
            e.Use();
        }
    }
}
#endif
