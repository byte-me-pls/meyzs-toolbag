#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class GridDuplicationTool
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

            var referenceObj = objectsToUse[0];
            if (showGridHandle) DrawGridHandle(referenceObj);

            DrawGridPreview();
            sceneView.Repaint();
        }

        private static void DrawGridHandle(GameObject referenceObj)
        {
            if (!referenceObj) return;

            Vector3 gridCenter = referenceObj.transform.position;

            EditorGUI.BeginChangeCheck();

            // X spacing handle
            Vector3 xHandle = gridCenter + new Vector3(dataHolder.gridSpacing.x, 0, 0);
            float handleSizeX = HandleUtility.GetHandleSize(xHandle) * 0.15f;
            Vector3 newXHandle = Handles.FreeMoveHandle(xHandle, handleSizeX, Vector3.zero, Handles.CubeHandleCap);

            // Z spacing handle
            Vector3 zHandle = gridCenter + new Vector3(0, 0, dataHolder.gridSpacing.y);
            float handleSizeZ = HandleUtility.GetHandleSize(zHandle) * 0.1f;
            Vector3 newZHandle = Handles.FreeMoveHandle(zHandle, handleSizeZ, Vector3.zero, Handles.CubeHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataHolder, "Adjust Grid Spacing");

                Vector3 newXSpacing = newXHandle - gridCenter;
                Vector3 newZSpacing = newZHandle - gridCenter;

                // allow negative spacing
                dataHolder.gridSpacing = new Vector2(newXSpacing.x, newZSpacing.z);

                EditorWindow.focusedWindow?.Repaint();
                SceneView.RepaintAll();
            }

            // visuals
            Handles.color = Color.red;
            Handles.DrawLine(gridCenter, xHandle);
            Handles.Label(xHandle + Vector3.up * 0.3f, $"X: {dataHolder.gridSpacing.x:F1}", EditorStyles.boldLabel);

            Handles.color = Color.blue;
            Handles.DrawLine(gridCenter, zHandle);
            Handles.Label(zHandle + Vector3.up * 0.3f, $"Z: {dataHolder.gridSpacing.y:F1}", EditorStyles.boldLabel);
        }

        private static void DrawGridPreview()
        {
            Handles.color = new Color(0, 1, 1, 0.3f);
            var start = GetGridCenter();

            // lines
            for (int x = 0; x <= dataHolder.gridCount.x; x++)
            {
                Vector3 a = start + new Vector3(x * dataHolder.gridSpacing.x, 0, 0);
                Vector3 b = start + new Vector3(x * dataHolder.gridSpacing.x, 0, dataHolder.gridCount.y * dataHolder.gridSpacing.y);
                Handles.DrawLine(a, b);
            }
            for (int z = 0; z <= dataHolder.gridCount.y; z++)
            {
                Vector3 a = start + new Vector3(0, 0, z * dataHolder.gridSpacing.y);
                Vector3 b = start + new Vector3(dataHolder.gridCount.x * dataHolder.gridSpacing.x, 0, z * dataHolder.gridSpacing.y);
                Handles.DrawLine(a, b);
            }

            // points
            Handles.color = Color.blue;
            for (int x = 0; x < dataHolder.gridCount.x; x++)
            {
                for (int z = 0; z < dataHolder.gridCount.y; z++)
                {
                    Vector3 pos = start + new Vector3(x * dataHolder.gridSpacing.x, 0, z * dataHolder.gridSpacing.y);
                    if (dataHolder.alternateRows && (z % 2) == 1)
                        pos.x += dataHolder.gridSpacing.x * 0.5f;

                    if (snapToSurface && Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                        pos = hit.point;

                    float size = HandleUtility.GetHandleSize(pos) * 0.05f;
                    Handles.CubeHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
                }
            }
        }
    }
}
#endif
