#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class LinearDuplicationTool
    {
        // Scene View preview with blue square handles
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

            if (showOffsetHandle)
                DrawOffsetHandle(referenceObj);

            foreach (var orig in objectsToUse)
            {
                if (!orig) continue;

                for (int i = 1; i <= dataHolder.count; i++)
                {
                    Vector3 off = dataHolder.useLocalSpace
                        ? orig.transform.TransformDirection(dataHolder.offset * i)
                        : dataHolder.offset * i;

                    Vector3 previewPos = orig.transform.position + off;

                    if (snapToSurface && Physics.Raycast(previewPos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                        previewPos = hit.point;

                    // preview point
                    Handles.color = Color.blue;
                    float size = HandleUtility.GetHandleSize(previewPos) * 0.08f;
                    Handles.CubeHandleCap(0, previewPos, Quaternion.identity, size, EventType.Repaint);

                    // connection
                    if (i == 1)
                    {
                        Handles.color = Color.yellow;
                        Handles.DrawLine(orig.transform.position, previewPos);
                    }
                    else
                    {
                        Vector3 prevOff = dataHolder.useLocalSpace
                            ? orig.transform.TransformDirection(dataHolder.offset * (i - 1))
                            : dataHolder.offset * (i - 1);
                        Vector3 prevPos = orig.transform.position + prevOff;

                        if (snapToSurface && Physics.Raycast(prevPos + Vector3.up * 100f, Vector3.down, out var prevHit, 200f, snapLayers))
                            prevPos = prevHit.point;

                        Handles.color = Color.cyan;
                        Handles.DrawLine(prevPos, previewPos);
                    }

                    Handles.color = Color.white;
                    Handles.Label(previewPos + Vector3.up * 0.5f, $"{i}", EditorStyles.boldLabel);
                }

                // original marker
                Handles.color = Color.red;
                float origSize = HandleUtility.GetHandleSize(orig.transform.position) * 0.12f;
                Handles.CubeHandleCap(0, orig.transform.position, Quaternion.identity, origSize, EventType.Repaint);
                Handles.Label(orig.transform.position + Vector3.up * 1f, "ORIG", EditorStyles.boldLabel);
            }

            sceneView.Repaint();
        }

        private static void DrawOffsetHandle(GameObject referenceObj)
        {
            if (!referenceObj) return;

            Vector3 handlePos = referenceObj.transform.position + (dataHolder.useLocalSpace
                ? referenceObj.transform.TransformDirection(dataHolder.offset)
                : dataHolder.offset);

            // arrow
            Handles.color = Color.magenta;
            Handles.DrawLine(referenceObj.transform.position, handlePos);
            Vector3 dir = (handlePos - referenceObj.transform.position).normalized;
            float arrowSize = HandleUtility.GetHandleSize(handlePos) * 0.2f;
            Handles.ArrowHandleCap(0, handlePos, Quaternion.LookRotation(dir), arrowSize, EventType.Repaint);

            // interactive handle
            EditorGUI.BeginChangeCheck();
            Quaternion rot = dataHolder.useLocalSpace ? referenceObj.transform.rotation : Quaternion.identity;
            float hSize = HandleUtility.GetHandleSize(handlePos) * 0.15f;
            Vector3 newHandlePos = Handles.FreeMoveHandle(handlePos, hSize, Vector3.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(dataHolder, "Adjust Offset Handle");
                Vector3 newOffsetWorld = newHandlePos - referenceObj.transform.position;
                dataHolder.offset = dataHolder.useLocalSpace
                    ? referenceObj.transform.InverseTransformDirection(newOffsetWorld)
                    : newOffsetWorld;

                EditorWindow.focusedWindow?.Repaint();
                SceneView.RepaintAll();
            }

            Handles.Label(handlePos + Vector3.up * 0.3f,
                $"Offset: {dataHolder.offset.x:F1}, {dataHolder.offset.y:F1}, {dataHolder.offset.z:F1}",
                EditorStyles.boldLabel);
        }
    }
}
#endif
