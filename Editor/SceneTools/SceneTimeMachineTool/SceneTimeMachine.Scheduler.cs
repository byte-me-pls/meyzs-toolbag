#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SceneTimeMachineTool
    {
        private static void ApplyEnableStateSubscriptions(bool enable)
        {
            EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            if (!enable) return;

            if (settings.autoSnapshotOnSave)
                EditorSceneManager.sceneSaved += OnSceneSaved;

            if (settings.autoSnapshotByInterval)
                EditorApplication.update += OnEditorUpdate;

            if (settings.autoSnapshotOnPlay)
                EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void ScheduleNextTick()
        {
            double sec = Mathf.Max(0.1f, settings.snapshotInterval) * 60.0;
            nextSnapshotTime = EditorApplication.timeSinceStartup + sec;
        }

        private static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            if (!settings.toolEnabled || string.IsNullOrEmpty(scene.path)) return;
            TakeSnapshot(scene.path, SnapshotType.AutoSave, "Auto-save backup");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange st)
        {
            if (!settings.toolEnabled) return;
            if (st == PlayModeStateChange.ExitingEditMode)
            {
                if (TryGetActiveSceneUnityPath(out var path))
                    TakeSnapshot(path, SnapshotType.PlayMode, "Pre-play backup");
            }
        }

        private static void OnEditorUpdate()
        {
            if (!settings.toolEnabled || !settings.autoSnapshotByInterval) return;

            if (EditorApplication.timeSinceStartup >= nextSnapshotTime)
            {
                if (TryGetActiveSceneUnityPath(out var path))
                    TakeSnapshot(path, SnapshotType.AutoInterval, "Auto-interval backup");

                ScheduleNextTick();
            }
        }
    }
}
#endif
