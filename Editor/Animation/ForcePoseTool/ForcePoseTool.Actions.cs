#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Object = UnityEngine.Object;

namespace MeyzsToolBag.Editor.Animation
{
    public static partial class ForcePoseTool
    {
        // --- Runtime actions / ops ---
        private static void SamplePose()
        {
            if (!AnimationMode.InAnimationMode() || targetObject == null || selectedClip == null) return;

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(
                targetObject,
                selectedClip,
                normalizedTime * selectedClip.length
            );
            AnimationMode.EndSampling();
            SceneView.RepaintAll();
        }

        private static void BakePose()
        {
            if (targetObject == null || selectedClip == null) return;

            var temp = Object.Instantiate(targetObject);
            temp.hideFlags = HideFlags.HideAndDontSave;
            if (temp.TryGetComponent<Animator>(out var anim)) anim.enabled = false;
            if (temp.TryGetComponent<RigBuilder>(out var rig)) rig.enabled = false;

            selectedClip.SampleAnimation(
                temp,
                normalizedTime * selectedClip.length
            );

            if (AnimationMode.InAnimationMode())
            {
                AnimationMode.StopAnimationMode();
                previewing = false;
            }

            CopyTransforms(temp.transform, targetObject.transform);
            Object.DestroyImmediate(temp);
        }

        private static void CopyPoseToClipboard()
        {
            var snap = CreateSnapshot(targetObject.transform);
            var data = new PresetData { name = "", snapshot = snap, normalizedTime = normalizedTime };
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(data, true);
        }

        private static void PastePoseFromClipboard()
        {
            try
            {
                var data = JsonUtility.FromJson<PresetData>(EditorGUIUtility.systemCopyBuffer);
                if (data?.snapshot == null) throw new Exception();
                ApplySnapshot(targetObject.transform, data.snapshot);
                normalizedTime = data.normalizedTime;
                if (previewing) SamplePose();
            }
            catch
            {
                Debug.LogError("Invalid clipboard pose data.");
            }
        }

        private static TransformSnapshot CreateSnapshot(UnityEngine.Transform root)
        {
            var s = new TransformSnapshot
            {
                name = root.name,
                localPosition = root.localPosition,
                localRotation = root.localRotation,
                localScale = root.localScale
            };
            for (int i = 0; i < root.childCount; i++)
                s.children.Add(CreateSnapshot(root.GetChild(i)));
            return s;
        }

        private static void ApplySnapshot(UnityEngine.Transform tgt, TransformSnapshot snap)
        {
            if (tgt.name != snap.name) return;
            Undo.RecordObject(tgt, "Apply Pose");
            tgt.localPosition = snap.localPosition;
            tgt.localRotation = snap.localRotation;
            tgt.localScale = snap.localScale;
            foreach (var c in snap.children)
                if (tgt.Find(c.name) is UnityEngine.Transform ct)
                    ApplySnapshot(ct, c);
        }

        private static void CopyTransforms(UnityEngine.Transform src, UnityEngine.Transform dst)
        {
            Undo.RecordObject(dst, "Bake Pose");
            dst.localPosition = src.localPosition;
            dst.localRotation = src.localRotation;
            dst.localScale = src.localScale;
            for (int i = 0; i < src.childCount; i++)
            {
                var s = src.GetChild(i);
                var d = dst.Find(s.name);
                if (d != null) CopyTransforms(s, d);
            }
        }
    }
}
#endif
