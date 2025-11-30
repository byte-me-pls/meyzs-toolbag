#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class CircularDuplicationTool
    {
        private static List<GameObject> GenerateCircularArray(GameObject[] originals)
        {
            var list = new List<GameObject>();
            int index = 0;

            for (int i = 0; i < dataHolder.count; i++)
            {
                float t = dataHolder.count > 1 ? i / (float)(dataHolder.count - 1) : 0f;
                float angle = Mathf.Lerp(dataHolder.startAngle, dataHolder.endAngle, t) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 pos = dataHolder.center + dir * dataHolder.radius;

                foreach (var orig in originals)
                {
                    if (!orig) continue;

                    var copy = Object.Instantiate(orig);
                    Undo.RegisterCreatedObjectUndo(copy, "Circular Duplicate");

                    // snap / rotation
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
                                Vector3 fwd = Vector3.Cross(hit.normal, Vector3.up);
                                if (fwd.magnitude > 0.1f)
                                    rotation = Quaternion.LookRotation(fwd, hit.normal);
                            }

                            copy.transform.rotation = rotation;
                        }
                    }

                    copy.transform.position = pos;

                    if (dataHolder.faceCenter && (!snapToSurface || (!alignToSlope && !alignToSurfaceNormal)))
                    {
                        var lookDir = (dataHolder.center - pos).normalized;
                        if (lookDir != Vector3.zero)
                            copy.transform.rotation = Quaternion.LookRotation(lookDir);
                    }

                    ApplyRandom(copy);
                    copy.name = FormatName(orig.name, index++);
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
