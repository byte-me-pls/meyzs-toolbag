#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SplineDuplicationTool
    {
        private struct SplineSample { public Vector3 position; public Quaternion rotation; }

        // Sampling & math
        private static List<SplineSample> SampleSpline()
        {
            var samples = new List<SplineSample>();
            float length = CalculateLength();
            if (spacing <= 0 || splinePoints.Count < 2) return samples;

            int steps = Mathf.CeilToInt(length / spacing);
            for (int i = 0; i <= steps; i++)
            {
                float dist = Mathf.Min(i * spacing, length);
                var pos = GetPoint(dist);
                var rot = alignToPath ? GetTangent(dist) : Quaternion.identity;

                if (snapToSurface && Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                {
                    pos = hit.point;

                    if (alignToSlope)
                    {
                        float slope = Vector3.Angle(Vector3.up, hit.normal);
                        if (slope <= maxSlopeAngle && slope > 0.1f)
                        {
                            var slopeRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            if (alignToPath)
                            {
                                var pathRot = GetTangent(dist);
                                rot = slopeRot * pathRot;
                            }
                            else rot = slopeRot;
                        }
                    }
                    else if (alignToSurfaceNormal && !alignToPath)
                    {
                        Vector3 forward = Vector3.Cross(hit.normal, Vector3.up);
                        if (forward.magnitude > 0.1f)
                            rot = Quaternion.LookRotation(forward, hit.normal);
                    }
                }

                samples.Add(new SplineSample { position = pos, rotation = rot });
            }
            return samples;
        }

        private static float CalculateLength()
        {
            float len = 0;
            for (int i = 1; i < splinePoints.Count; i++)
                len += Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
            if (loop && splinePoints.Count > 1)
                len += Vector3.Distance(splinePoints[^1], splinePoints[0]);
            return len;
        }

        private static Vector3 GetPoint(float distance)
        {
            if (splinePoints.Count < 2) return Vector3.zero;
            float traveled = 0;
            for (int i = 1; i < splinePoints.Count; i++)
            {
                float seg = Vector3.Distance(splinePoints[i - 1], splinePoints[i]);
                if (traveled + seg >= distance)
                    return Vector3.Lerp(splinePoints[i - 1], splinePoints[i], (distance - traveled) / seg);
                traveled += seg;
            }
            return loop ? splinePoints[0] : splinePoints[^1];
        }

        private static Quaternion GetTangent(float distance)
        {
            var p1 = GetPoint(distance);
            var p2 = GetPoint(distance + 0.01f);
            var dir = (p2 - p1).normalized;
            return dir != Vector3.zero ? Quaternion.LookRotation(dir) : Quaternion.identity;
        }

        // Presets
        private static void CreateStraightLine()
        {
            var center = GetSourceCenter();
            splinePoints.Add(center + Vector3.left * spacing * 2);
            splinePoints.Add(center + Vector3.right * spacing * 2);
        }

        private static void CreateCirclePath()
        {
            var center = GetSourceCenter();
            int segments = Mathf.Max(3, Mathf.FloorToInt(360f / spacing));
            float radius = spacing * segments / (2 * Mathf.PI);
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                splinePoints.Add(center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius);
            }
            loop = true;
        }

        private static void CreateSCurve()
        {
            var center = GetSourceCenter();
            splinePoints.Add(center + new Vector3(-spacing * 2, 0, -spacing));
            splinePoints.Add(center + new Vector3(-spacing, 0, spacing));
            splinePoints.Add(center + new Vector3(spacing, 0, -spacing));
            splinePoints.Add(center + new Vector3(spacing * 2, 0, spacing));
        }

        private static Vector3 GetSourceCenter()
        {
            if (useLockedSources && sourceObjects != null && sourceObjects.Length > 0 && sourceObjects[0] != null)
                return sourceObjects[0].transform.position;

            var currentSelection = Selection.gameObjects;
            if (currentSelection.Length > 0)
                return currentSelection[0].transform.position;

            return Vector3.zero;
        }

        // Generation
        private static List<GameObject> GenerateSplineArray(GameObject[] originals)
        {
            var samples = SampleSpline();
            var list = new List<GameObject>();

            for (int i = 1; i < samples.Count; i++)
            {
                var sample = samples[i];
                float t = i / (float)(samples.Count - 1);

                foreach (var orig in originals)
                {
                    if (!orig) continue;

                    var copy = Object.Instantiate(orig);
                    Undo.RegisterCreatedObjectUndo(copy, "Spline Duplicate");

                    // scale
                    copy.transform.localScale = orig.transform.localScale * scaleCurve.Evaluate(t);
                    if (randomScale)
                    {
                        float s = Random.Range(randomScaleRange.x, randomScaleRange.y);
                        copy.transform.localScale *= s;
                    }

                    // rotation
                    copy.transform.rotation = sample.rotation;
                    if (randomRotation)
                        copy.transform.Rotate(
                            Random.Range(-randomRotationRange.x, randomRotationRange.x),
                            Random.Range(-randomRotationRange.y, randomRotationRange.y),
                            Random.Range(-randomRotationRange.z, randomRotationRange.z),
                            Space.Self
                        );

                    // position + snap
                    copy.transform.position = new Vector3(sample.position.x, sample.position.y + 100f, sample.position.z);
                    Vector3 bottomOffset = GetObjectBottomOffset(copy);

                    if (snapToSurface && Physics.Raycast(copy.transform.position, Vector3.down, out var hit2, Mathf.Infinity, snapLayers))
                    {
                        copy.transform.position = hit2.point + bottomOffset;

                        if (alignToSlope)
                        {
                            float slope = Vector3.Angle(Vector3.up, hit2.normal);
                            if (slope <= maxSlopeAngle && slope > 0.1f)
                            {
                                var slopeRot = Quaternion.FromToRotation(Vector3.up, hit2.normal);
                                if (alignToPath) copy.transform.rotation = slopeRot * sample.rotation;
                                else            copy.transform.rotation = slopeRot * orig.transform.rotation;
                            }
                        }
                        else if (alignToSurfaceNormal && !alignToPath)
                        {
                            Vector3 forward = Vector3.Cross(hit2.normal, Vector3.up);
                            if (forward.magnitude > 0.1f)
                                copy.transform.rotation = Quaternion.LookRotation(forward, hit2.normal);
                        }
                    }
                    else
                    {
                        copy.transform.position = sample.position + bottomOffset;
                    }

                    copy.name = FormatName(orig.name, i - 1);
                    list.Add(copy);
                }
            }
            return list;
        }

        private static Vector3 GetObjectBottomOffset(GameObject obj)
        {
            var bounds = new Bounds();
            bool has = false;

            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!has) { bounds = r.bounds; has = true; }
                else bounds.Encapsulate(r.bounds);
            }
            if (!has)
            {
                var cols = obj.GetComponentsInChildren<Collider>();
                foreach (var c in cols)
                {
                    if (!has) { bounds = c.bounds; has = true; }
                    else bounds.Encapsulate(c.bounds);
                }
            }

            if (has)
            {
                float dist = obj.transform.position.y - bounds.min.y;
                return Vector3.up * dist;
            }
            return Vector3.zero;
        }
    }
}
#endif
