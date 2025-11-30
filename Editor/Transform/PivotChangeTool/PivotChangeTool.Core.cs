#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Transform
{
    public static partial class PivotChangeTool
    {
        // ===== Enums =====
        public enum PivotPosition
        {
            Center, Bottom, Top, Left, Right, Front, Back,
            BoundsMin, BoundsMax,
            BottomCenter, TopCenter, LeftCenter, RightCenter, FrontCenter, BackCenter
        }
        public enum PivotMode { Individual, Combined }
        private enum CoordinateSpace { World, Local }

        // ===== State =====
        private static bool isPivotEditing = false;
        private static Vector3 customPivotPos = Vector3.zero;
        private static Vector2 scrollPos = Vector2.zero;
        private static bool sceneGUIActive = false;
        private static PivotMode currentPivotMode = PivotMode.Individual;
        private static CoordinateSpace currentCoordinateSpace = CoordinateSpace.World;

        // Presets
        [System.Serializable]
        private struct PivotPreset
        {
            public string name;
            public Vector3 position;
            public PivotPosition pivotType;
            public bool isWorldSpace;

            public PivotPreset(string name, Vector3 pos, PivotPosition type, bool worldSpace)
            {
                this.name = name;
                this.position = pos;
                this.pivotType = type;
                this.isWorldSpace = worldSpace;
            }
        }
        private static readonly List<PivotPreset> pivotPresets = new List<PivotPreset>();
        private static bool showPresets = false;
        private static string newPresetName = "";

        // Advanced
        private static bool showAdvancedOptions = false;
        private static bool preserveChildPositions = true;
        private static bool updateColliders = true;
        private static bool createBackup = true;
        private static bool showPivotPreview = true;
        private static Color pivotPreviewColor = Color.yellow;

        // Visual
        private static bool showBounds = true;
        private static bool showOriginalPivot = true;
        private static float handleSize = 1f;

        // Perf cache
        private static GameObject[] lastSelection;
        private static Bounds? cachedCombinedBounds;

        // Keys
        private const string PRESETS_KEY = "PivotChangeTool.Presets";
        private const string SETTINGS_KEY = "PivotChangeTool.Settings";

        // ===== Core Apply APIs =====
        private static void ApplyPivotPosition(PivotPosition position)
        {
            var selected = Selection.gameObjects.Where(go => go.GetComponent<MeshFilter>() != null).ToArray();
            if (selected.Length == 0)
            {
                Debug.LogWarning("No objects with MeshFilter component selected!");
                return;
            }

            if (createBackup)
                Undo.RecordObjects(selected.Select(go => go.transform).ToArray(), $"Adjust Pivot to {position}");

            if (currentPivotMode == PivotMode.Combined)
                ApplyCombinedPivot(selected, position);
            else
                ApplyIndividualPivots(selected, position);

            Debug.Log($"Pivot adjusted for {selected.Length} objects to {position} ({currentPivotMode} mode)");
        }

        private static void ApplyIndividualPivots(GameObject[] objects, PivotPosition pos)
        {
            foreach (var go in objects)
            {
                var r = go.GetComponent<Renderer>();
                if (!r) continue;
                var p = GetPivotPosition(r.bounds, pos);
                AdjustPivot(go, p);
            }
        }

        private static void ApplyCombinedPivot(GameObject[] objects, PivotPosition pos)
        {
            var cb = GetCombinedBounds(objects);
            var p = GetPivotPosition(cb, pos);
            foreach (var go in objects) AdjustPivot(go, p);
        }

        private static Bounds GetCombinedBounds(GameObject[] objects)
        {
            if (lastSelection != null && lastSelection.SequenceEqual(objects) && cachedCombinedBounds.HasValue)
                return cachedCombinedBounds.Value;

            Bounds combined = new Bounds();
            bool first = true;

            foreach (var go in objects)
            {
                var r = go.GetComponent<Renderer>();
                if (!r) continue;

                if (first) { combined = r.bounds; first = false; }
                else combined.Encapsulate(r.bounds);
            }

            lastSelection = objects;
            cachedCombinedBounds = combined;
            return combined;
        }

        private static Vector3 GetPivotPosition(Bounds b, PivotPosition p)
        {
            switch (p)
            {
                default:
                case PivotPosition.Center:       return b.center;
                case PivotPosition.Bottom:       return new Vector3(b.center.x, b.min.y,    b.center.z);
                case PivotPosition.Top:          return new Vector3(b.center.x, b.max.y,    b.center.z);
                case PivotPosition.Left:         return new Vector3(b.min.x,    b.center.y, b.center.z);
                case PivotPosition.Right:        return new Vector3(b.max.x,    b.center.y, b.center.z);
                case PivotPosition.Front:        return new Vector3(b.center.x, b.center.y, b.max.z);
                case PivotPosition.Back:         return new Vector3(b.center.x, b.center.y, b.min.z);
                case PivotPosition.BottomCenter: return new Vector3(b.center.x, b.min.y,    b.center.z);
                case PivotPosition.TopCenter:    return new Vector3(b.center.x, b.max.y,    b.center.z);
                case PivotPosition.LeftCenter:   return new Vector3(b.min.x,    b.center.y, b.center.z);
                case PivotPosition.RightCenter:  return new Vector3(b.max.x,    b.center.y, b.center.z);
                case PivotPosition.FrontCenter:  return new Vector3(b.center.x, b.center.y, b.max.z);
                case PivotPosition.BackCenter:   return new Vector3(b.center.x, b.center.y, b.min.z);
                case PivotPosition.BoundsMin:    return b.min;
                case PivotPosition.BoundsMax:    return b.max;
            }
        }

        private static void ApplyCustomPivot()
        {
            var selected = Selection.gameObjects.Where(go => go.GetComponent<MeshFilter>() != null).ToArray();
            if (selected.Length == 0) return;

            if (createBackup)
                Undo.RecordObjects(selected.Select(go => go.transform).ToArray(), "Apply Custom Pivot");

            foreach (var go in selected) AdjustPivot(go, customPivotPos);
            Debug.Log($"Custom pivot applied to {selected.Length} objects");
        }

        // ===== Mesh / Colliders adjustment =====
        private struct ColliderInfo
        {
            public MeshCollider meshCollider;
            public BoxCollider boxCollider;
            public SphereCollider sphereCollider;
            public CapsuleCollider capsuleCollider;

            public Vector3 boxCenter, boxSize;
            public Vector3 sphereCenter;
            public float sphereRadius;
            public Vector3 capsuleCenter;
            public float capsuleRadius, capsuleHeight;
            public int capsuleDirection;
        }

        private static ColliderInfo StoreColliderInfo(GameObject go)
        {
            var info = new ColliderInfo
            {
                meshCollider    = go.GetComponent<MeshCollider>(),
                boxCollider     = go.GetComponent<BoxCollider>(),
                sphereCollider  = go.GetComponent<SphereCollider>(),
                capsuleCollider = go.GetComponent<CapsuleCollider>()
            };

            if (info.boxCollider) { info.boxCenter = info.boxCollider.center; info.boxSize = info.boxCollider.size; }
            if (info.sphereCollider) { info.sphereCenter = info.sphereCollider.center; info.sphereRadius = info.sphereCollider.radius; }
            if (info.capsuleCollider)
            {
                info.capsuleCenter = info.capsuleCollider.center;
                info.capsuleRadius = info.capsuleCollider.radius;
                info.capsuleHeight = info.capsuleCollider.height;
                info.capsuleDirection = info.capsuleCollider.direction;
            }
            return info;
        }

        private static void UpdateColliders(GameObject go, ColliderInfo info, Vector3 localOffset, Mesh newMesh)
        {
            if (info.meshCollider)
            {
                Undo.RecordObject(info.meshCollider, "Adjust Pivot Collider");
                info.meshCollider.sharedMesh = null;
                info.meshCollider.sharedMesh = newMesh;
            }
            if (info.boxCollider)
            {
                Undo.RecordObject(info.boxCollider, "Adjust Pivot Collider");
                info.boxCollider.center = info.boxCenter - localOffset;
            }
            if (info.sphereCollider)
            {
                Undo.RecordObject(info.sphereCollider, "Adjust Pivot Collider");
                info.sphereCollider.center = info.sphereCenter - localOffset;
            }
            if (info.capsuleCollider)
            {
                Undo.RecordObject(info.capsuleCollider, "Adjust Pivot Collider");
                info.capsuleCollider.center = info.capsuleCenter - localOffset;
            }
        }

        private static void AdjustPivot(GameObject go, Vector3 newPivotWorld)
        {
            var t = go.transform;
            var mf = go.GetComponent<MeshFilter>();
            if (!mf || !mf.sharedMesh) return;

            // Keep children world transforms
            var childData = new List<(UnityEngine.Transform child, Vector3 pos, Quaternion rot, Vector3 scale)>();
            if (preserveChildPositions)
            {
                for (int i = 0; i < t.childCount; i++)
                {
                    var c = t.GetChild(i);
                    childData.Add((c, c.position, c.rotation, c.lossyScale));
                }
            }

            // Colliders backup
            ColliderInfo colInfo = default;
            if (updateColliders) colInfo = StoreColliderInfo(go);

            // Compute offset (local)
            var localOffset = t.InverseTransformVector(newPivotWorld - t.position);

            // Duplicate mesh & offset vertices
            var src = mf.sharedMesh;
            var dst = Object.Instantiate(src);
            dst.name = src.name + "_PivotAdjusted";

            var verts = dst.vertices;
            for (int i = 0; i < verts.Length; i++) verts[i] -= localOffset;
            dst.vertices = verts;
            dst.RecalculateBounds();
            dst.RecalculateNormals();

            Undo.RecordObject(mf, "Adjust Pivot Mesh");
            mf.mesh = dst;

            // Move transform to new pivot
            t.position = newPivotWorld;

            // Update colliders
            if (updateColliders) UpdateColliders(go, colInfo, localOffset, dst);

            // Restore children
            if (preserveChildPositions)
            {
                foreach (var (child, pos, rot, _) in childData)
                {
                    child.position = pos;
                    child.rotation = rot;
                }
            }

            EditorUtility.SetDirty(go);
        }

        // ===== Misc utils =====
        private static Vector3 SnapToGrid(Vector3 pos)
        {
            float snap = EditorSnapSettings.move.x;
            if (snap > 0)
            {
                pos.x = Mathf.Round(pos.x / snap) * snap;
                pos.y = Mathf.Round(pos.y / snap) * snap;
                pos.z = Mathf.Round(pos.z / snap) * snap;
            }
            return pos;
        }

        private static void SnapToNearestVertex()
        {
            var selected = Selection.gameObjects;
            if (selected.Length == 0) return;

            Vector3 best = customPivotPos;
            float bestDist = float.MaxValue;

            foreach (var go in selected)
            {
                var mf = go.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;

                var mesh = mf.sharedMesh;
                var verts = mesh.vertices;
                var tr = go.transform;

                for (int i = 0; i < verts.Length; i++)
                {
                    var wv = tr.TransformPoint(verts[i]);
                    float d = Vector3.Distance(customPivotPos, wv);
                    if (d < bestDist) { bestDist = d; best = wv; }
                }
            }

            customPivotPos = best;
            SceneView.RepaintAll();
        }

        private static Vector3 ParseVector3(string s)
        {
            var p = s.Split(',');
            return new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
        }
        private static string Vector3ToString(Vector3 v) => $"{v.x},{v.y},{v.z}";
    }
}
#endif
