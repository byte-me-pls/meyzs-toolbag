#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SceneTimeMachineTool
    {
        private static void TakeManualSnapshot(string sceneUnityPath)
        {
            TakeSnapshot(sceneUnityPath, SnapshotType.Manual, "Manual snapshot");
        }

        private static void TakeMilestoneSnapshot(string sceneUnityPath)
        {
            var s = TakeSnapshot(sceneUnityPath, SnapshotType.Milestone, "Milestone snapshot");
            if (s != null) { s.isPinned = true; SaveSnapshotMetadataIO(s); }
        }

        private static SceneSnapshot TakeSnapshot(string sceneUnityPath, SnapshotType type, string description)
        {
            if (string.IsNullOrEmpty(sceneUnityPath))
            {
                Debug.LogWarning("[SceneTimeMachine] Save your scene before snapshot.");
                return null;
            }
            string srcSceneAbs = UnityToFull(sceneUnityPath);
            if (!File.Exists(srcSceneAbs))
            {
                Debug.LogWarning("[SceneTimeMachine] Scene file not found on disk.");
                return null;
            }

            try
            {
                EnsureBackupFoldersAbs(sceneUnityPath, out string destDirAbs, out string destSceneAbs, out string destMetaAbs);

                File.Copy(srcSceneAbs, destSceneAbs, true);
                string srcMetaAbs = srcSceneAbs + ".meta";
                if (File.Exists(srcMetaAbs)) File.Copy(srcMetaAbs, destMetaAbs, true);

                var snapshot = new SceneSnapshot
                {
                    path = destDirAbs,
                    sceneName = Path.GetFileNameWithoutExtension(sceneUnityPath),
                    timestamp = DateTime.Now,
                    fileSize = new FileInfo(destSceneAbs).Length,
                    description = description,
                    isAutoSnapshot = type == SnapshotType.AutoSave || type == SnapshotType.AutoInterval || type == SnapshotType.PlayMode,
                    isPinned = type == SnapshotType.Milestone,
                    type = type,
                    unityVersion = Application.unityVersion,
                    author = Environment.UserName
                };

                AnalyzeSceneCount(srcSceneAbs, snapshot);

                if (settings.includeDependencies)
                {
                    if (settings.dependenciesListOnly)
                    {
                        var deps = AssetDatabase.GetDependencies(sceneUnityPath, true)
                            .Where(d => d != sceneUnityPath && d.StartsWith("Assets/"))
                            .Take(settings.maxDependencyCopy)
                            .ToList();
                        snapshot.dependencies = deps;
                    }
                    else
                    {
                        CopyDependenciesIO(sceneUnityPath, destDirAbs, settings.maxDependencyCopy);
                        var deps = AssetDatabase.GetDependencies(sceneUnityPath, true)
                            .Where(d => d != sceneUnityPath && d.StartsWith("Assets/"))
                            .Take(20)
                            .ToList();
                        snapshot.dependencies = deps;
                    }
                }

                if (settings.maxSnapshotSizeMB > 0 &&
                    snapshot.fileSize > settings.maxSnapshotSizeMB * 1024 * 1024)
                {
                    Debug.LogWarning($"[SceneTimeMachine] Snapshot exceeds size limit: {FormatBytes(snapshot.fileSize)}");
                }

                SaveSnapshotMetadataIO(snapshot);
                CleanupOldSnapshotsIO(snapshot.sceneName);
                RefreshSnapshotsIO();

                Debug.Log($"[SceneTimeMachine] Snapshot created (non-intrusive): {snapshot.type} - {snapshot.sceneName}");
                return snapshot;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SceneTimeMachine] Snapshot failed: {e.Message}");
                return null;
            }
        }
    }
}
#endif
