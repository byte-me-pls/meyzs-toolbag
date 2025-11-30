#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class RandomAreaDuplicationTool
    {
        private static void GenerateRandomArea(GameObject[] prototypes)
        {
            GameObject parent = null;
            if (createParentGroup)
            {
                string uniqueParentName = GetUniqueParentName(parentName);
                parent = new GameObject(uniqueParentName);
                Undo.RegisterCreatedObjectUndo(parent, "Random Area Parent");
            }

            var positions = GeneratePositions(center, radius, count, minDistance, avoidOverlap, seed);
            Debug.Log($"Generated {positions.Count}/{count} positions with min distance {minDistance}");

            System.Random rng = new System.Random(seed + 1000); // decouple from position RNG

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 pos = positions[i];
                Quaternion rot = Quaternion.identity;

                if (snapToSurface)
                {
                    if (Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out var hit, 200f, snapLayers))
                    {
                        pos = hit.point;
                        float slope = Vector3.Angle(Vector3.up, hit.normal);
                        if (alignToSlope && slope <= maxSlopeAngle)
                        {
                            rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
                            if (randomRotateOnSlope) rot *= Quaternion.Euler(0, (float)(rng.NextDouble() * 360), 0);
                        }
                        else if (alignToSurfaceNormal)
                        {
                            rot = Quaternion.LookRotation(Vector3.Cross(hit.normal, Vector3.up), hit.normal);
                        }
                    }
                }

                SpawnPrototype(pos, rot, i, parent, prototypes, rng);
            }

            if (positions.Count < count)
                Debug.LogWarning($"Only placed {positions.Count}/{count} items due to overlap constraints or area limitations.");
        }

        private static void SpawnPrototype(Vector3 pos, Quaternion rot, int idx, GameObject parent, GameObject[] prototypes, System.Random rng)
        {
            foreach (var orig in prototypes)
            {
                if (orig == null) continue;

                var copy = Object.Instantiate(orig);
                Undo.RegisterCreatedObjectUndo(copy, "Random Area Spawn");

                copy.transform.position = pos;
                copy.transform.rotation = rot;

                if (randomRotation)
                {
                    copy.transform.Rotate(new Vector3(
                        (float)(rng.NextDouble() * 2 - 1) * randomRotationRange.x,
                        (float)(rng.NextDouble() * 2 - 1) * randomRotationRange.y,
                        (float)(rng.NextDouble() * 2 - 1) * randomRotationRange.z));
                }

                if (randomScale)
                {
                    float s = randomScaleRange.x + (float)(rng.NextDouble() * (randomScaleRange.y - randomScaleRange.x));
                    copy.transform.localScale *= s;
                }

                copy.name = namingPattern.Replace("{name}", orig.name).Replace("{index}", (idx + 1).ToString("D2"));
                if (parent != null) copy.transform.SetParent(parent.transform);
            }
        }
    }
}
#endif
