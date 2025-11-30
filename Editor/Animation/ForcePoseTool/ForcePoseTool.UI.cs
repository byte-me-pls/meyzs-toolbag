#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Animation
{
    public static partial class ForcePoseTool
    {
        // --- UI ---
        public static void Draw()
        {
            if (!presetsLoaded)
            {
                LoadPresets();
                presetsLoaded = true;
            }

            // Header
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Force Pose Preview", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);

            // Target object field
            EditorGUI.BeginChangeCheck();
            targetObject = (GameObject)EditorGUILayout.ObjectField("Target Object", targetObject, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                previewing = false;
                normalizedTime = 0f;
                hasOriginalSnapshot = false;
                if (targetObject != null)
                {
                    originalSnapshot = CreateSnapshot(targetObject.transform);
                    hasOriginalSnapshot = true;
                }
            }

            if (targetObject == null)
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Select a Target Object to enable pose tools.", MessageType.Info);
                return;
            }

            if (!targetObject.GetComponent<Animator>())
            {
                GUILayout.Space(5);
                EditorGUILayout.HelpBox("Animator Component Undetected.", MessageType.Info);
                return;
            }

            // Clip + time
            selectedClip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", selectedClip, typeof(AnimationClip), false);
            GUILayout.Space(6);
            normalizedTime = EditorGUILayout.Slider("Normalized Time", normalizedTime, 0f, 1f);
            GUILayout.Space(6);

            // Preview / Bake
            GUI.enabled = selectedClip != null;
            if (!previewing)
            {
                if (GUILayout.Button("Start Preview"))
                {
                    AnimationMode.StartAnimationMode();
                    previewing = true;
                    SamplePose();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Bake Pose")) BakePose();
                if (GUILayout.Button("Stop Preview"))
                {
                    AnimationMode.StopAnimationMode();
                    previewing = false;
                }
                EditorGUILayout.EndHorizontal();

                if (previewing && AnimationMode.InAnimationMode())
                    SamplePose();
            }
            GUI.enabled = true;

            // Reset
            GUILayout.Space(6);
            GUI.enabled = hasOriginalSnapshot;
            if (GUILayout.Button("↺ Reset Pose"))
            {
                if (previewing)
                {
                    AnimationMode.StopAnimationMode();
                    previewing = false;
                }

                ApplySnapshot(targetObject.transform, originalSnapshot);
                normalizedTime = 0f;
                Debug.Log("Pose reset to original.");
            }
            GUI.enabled = true;

            // Copy / Paste
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📋 Copy Pose"))  CopyPoseToClipboard();
            if (GUILayout.Button("📥 Paste Pose")) PastePoseFromClipboard();
            EditorGUILayout.EndHorizontal();

            // Preset select/delete
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (presets.Count > 0)
            {
                EditorGUI.BeginChangeCheck();
                var names = presets.ConvertAll(p => p.name).ToArray();
                selectedPresetIndex = EditorGUILayout.Popup("Select Preset", selectedPresetIndex, names);
                GUILayout.Space(5);
                if (EditorGUI.EndChangeCheck())
                {
                    var pd = presets[selectedPresetIndex];
                    ApplySnapshot(targetObject.transform, pd.snapshot);
                    normalizedTime = pd.normalizedTime;
                    if (previewing) SamplePose();
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete Preset"))
                {
                    presets.RemoveAt(selectedPresetIndex);
                    selectedPresetIndex = Mathf.Clamp(selectedPresetIndex, 0, presets.Count - 1);
                    SavePresets();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No presets saved.", MessageType.Info);
            }
            EditorGUILayout.EndVertical();

            // Preset create
            GUILayout.Space(10);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Pose Presets Library", EditorStyles.boldLabel);
            GUILayout.Space(5);
            newPresetName = EditorGUILayout.TextField("New Preset Name", newPresetName);
            GUILayout.Space(5);
            GUI.enabled = !string.IsNullOrWhiteSpace(newPresetName);
            if (GUILayout.Button("Save Preset"))
            {
                presets.Add(new PresetData
                {
                    name = newPresetName.Trim(),
                    snapshot = CreateSnapshot(targetObject.transform),
                    normalizedTime = normalizedTime
                });
                newPresetName = "";
                selectedPresetIndex = presets.Count - 1;
                SavePresets();
            }
            GUI.enabled = true;
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);
        }
    }
}
#endif
