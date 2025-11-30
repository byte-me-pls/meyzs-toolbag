#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class LinearDuplicationTool
    {
        private static List<GameObject> GenerateLinearArray(GameObject[] originals)
        {
            var list = new List<GameObject>();

            for (int i = 1; i <= dataHolder.count; i++)
            {
                foreach (var orig in originals)
                {
                    if (!orig) continue;

                    var copy = Object.Instantiate(orig);
                    Undo.RegisterCreatedObjectUndo(copy, "Linear Duplicate");

                    Vector3 off = dataHolder.useLocalSpace
                        ? orig.transform.TransformDirection(dataHolder.offset * i)
                        : dataHolder.offset * i;

                    Vector3 targetPos = orig.transform.position + off;

                    // snapping / rotation
                    if (snapToSurface)
                    {
                        if (Physics.Raycast(targetPos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                        {
                            targetPos = hit.point;

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

                    copy.transform.position = targetPos;
                    ApplyRandom(copy);
                    copy.name = FormatName(orig.name, i - 1);
                    list.Add(copy);
                }
            }

            return list;
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
