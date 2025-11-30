#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class GridDuplicationTool
    {
        private static List<GameObject> GenerateGridArray(GameObject[] originals)
        {
            var list = new List<GameObject>();
            var start = GetGridCenter();
            int index = 0;

            for (int x = 0; x < dataHolder.gridCount.x; x++)
            {
                for (int z = 0; z < dataHolder.gridCount.y; z++)
                {
                    if (x == 0 && z == 0) continue; // keep original spot free (opsiyonel davranış aynı bırakıldı)

                    Vector3 pos = start + new Vector3(x * dataHolder.gridSpacing.x, 0, z * dataHolder.gridSpacing.y);
                    if (dataHolder.alternateRows && (z % 2) == 1)
                        pos.x += dataHolder.gridSpacing.x * 0.5f;

                    foreach (var orig in originals)
                    {
                        if (!orig) continue;

                        var copy = Object.Instantiate(orig);
                        Undo.RegisterCreatedObjectUndo(copy, "Grid Duplicate");

                        // snapping & rotation
                        if (snapToSurface)
                        {
                            if (Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                            {
                                pos = hit.point;

                                Quaternion rotation = orig.transform.rotation;
                                float slope = Vector3.Angle(Vector3.up, hit.normal);

                                if (alignToSlope && slope <= maxSlopeAngle && slope > 0.1f)
                                {
                                    rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * orig.transform.rotation;
                                }
                                else if (alignToSurfaceNormal && !alignToSlope)
                                {
                                    Vector3 forward = Vector3.Cross(hit.normal, Vector3.up);
                                    if (forward.magnitude > 0.1f)
                                        rotation = Quaternion.LookRotation(forward, hit.normal);
                                }

                                copy.transform.rotation = rotation;
                            }
                        }

                        copy.transform.position = pos;
                        ApplyRandom(copy);
                        copy.name = FormatName(orig.name, index++);
                        list.Add(copy);
                    }
                }
            }
            return list;
        }

        private static Vector3 GetGridCenter()
        {
            if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0 && sourceObjects[0] != null)
                return sourceObjects[0].transform.position;

            return Selection.activeTransform?.position ?? Vector3.zero;
        }

        private static void ApplyRandom(GameObject go)
        {
            if (randomRotation)
                go.transform.Rotate(new Vector3(
                    Random.Range(-randomRotationRange.x, randomRotationRange.x),
                    Random.Range(-randomRotationRange.y, randomRotationRange.y),
                    Random.Range(-randomRotationRange.z, randomRotationRange.z)));

            if (randomScale)
            {
                float s = Random.Range(randomScaleRange.x, randomScaleRange.y);
                go.transform.localScale *= s;
            }
        }

        private static string FormatName(string original, int index)
        {
            return namingPattern.Replace("{name}", original)
                                .Replace("{index}", (index + 1).ToString("D2"));
        }
    }
}
#endif
