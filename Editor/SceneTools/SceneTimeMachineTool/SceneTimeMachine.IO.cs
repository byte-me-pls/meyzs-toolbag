#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class SceneTimeMachineTool
    {
        private static string ToAbsoluteBackupRoot(string configuredRoot)
        {
            if (string.IsNullOrEmpty(configuredRoot))
                return Path.Combine(ProjectRoot, "Library/MeyzToolbag/SceneBackups").Replace("\\", "/");

            string norm = configuredRoot.Replace("\\", "/").Trim();
            if (Path.IsPathRooted(norm)) return norm;
            return Path.Combine(ProjectRoot, norm).Replace("\\", "/");
        }

        private static string UnityToFull(string unityPath)
        {
            var p = unityPath.Replace("\\", "/");
            if (!p.StartsWith("Assets/")) return p;
            return Path.Combine(ProjectRoot, p).Replace("\\", "/");
        }

        private static void EnsureBackupFoldersAbs(string sceneUnityPath, out string destDirAbs, out string destSceneAbs, out string destMetaAbs)
        {
            string rootAbs = ToAbsoluteBackupRoot(settings.backupRoot);
            string sceneName = Path.GetFileNameWithoutExtension(sceneUnityPath);
            string sceneFolder = Path.Combine(rootAbs, sceneName);
            string timeFolder = Path.Combine(sceneFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff"));
            Directory.CreateDirectory(timeFolder);

            destDirAbs   = timeFolder.Replace("\\", "/");
            destSceneAbs = Path.Combine(destDirAbs, Path.GetFileName(sceneUnityPath)).Replace("\\", "/");
            destMetaAbs  = destSceneAbs + ".meta";
        }

        private static void AnalyzeSceneCount(string sceneAbsPath, SceneSnapshot snapshot)
        {
            try
            {
                int count = 0;
                foreach (var line in File.ReadLines(sceneAbsPath))
                    if (line.IndexOf("--- !u!1 &", StringComparison.Ordinal) >= 0) count++;
                snapshot.sceneObjectCount = count;
            }
            catch { }
        }

        private static void CopyDependenciesIO(string sceneUnityPath, string destDirAbs, int limit)
        {
            try
            {
                var deps = AssetDatabase.GetDependencies(sceneUnityPath, true);
                string depDir = Path.Combine(destDirAbs, "dependencies").Replace("\\", "/");
                Directory.CreateDirectory(depDir);

                int copied = 0;
                foreach (var dep in deps)
                {
                    if (dep == sceneUnityPath) continue;
                    if (!dep.StartsWith("Assets/")) continue;

                    string src = UnityToFull(dep);
                    if (!File.Exists(src)) continue;

                    string dst = Path.Combine(depDir, Path.GetFileName(dep)).Replace("\\", "/");
                    File.Copy(src, dst, true);

                    string srcMeta = src + ".meta";
                    if (File.Exists(srcMeta)) File.Copy(srcMeta, dst + ".meta", true);

                    copied++;
                    if (copied >= Mathf.Max(0, limit)) break;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneTimeMachine] Failed to copy dependencies: {e.Message}");
            }
        }

        private static void SaveSnapshotMetadataIO(SceneSnapshot snapshot)
        {
            try
            {
                string metadataPath = Path.Combine(snapshot.path, "snapshot_metadata.json").Replace("\\", "/");
                string json = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(metadataPath, json, Encoding.UTF8);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneTimeMachine] Failed to save metadata: {e.Message}");
            }
        }

        private static SceneSnapshot LoadSnapshotMetadataIO(string snapshotDirAbs, string sceneNameFromFolder)
        {
            try
            {
                string metadataPath = Path.Combine(snapshotDirAbs, "snapshot_metadata.json");
                if (File.Exists(metadataPath))
                {
                    string json = File.ReadAllText(metadataPath, Encoding.UTF8);
                    var s = JsonUtility.FromJson<SceneSnapshot>(json);
                    s.path = snapshotDirAbs.Replace("\\", "/");
                    if (string.IsNullOrEmpty(s.sceneName)) s.sceneName = sceneNameFromFolder;

                    if (string.IsNullOrEmpty(s.timestampString))
                    {
                        string folderName = Path.GetFileName(snapshotDirAbs);
                        if (DateTime.TryParseExact(folderName, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                         || DateTime.TryParseExact(folderName, "yyyy-MM-dd_HH-mm-ss-fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                            s.timestamp = dt;
                        else
                            s.timestamp = Directory.GetCreationTime(snapshotDirAbs);
                    }
                    return s;
                }
            }
            catch { }
            return null;
        }

        private static void CleanupOldSnapshotsIO(string sceneName)
        {
            string rootAbs = ToAbsoluteBackupRoot(settings.backupRoot);
            string sceneFolder = Path.Combine(rootAbs, sceneName).Replace("\\", "/");
            if (!Directory.Exists(sceneFolder)) return;

            var dirs = Directory.GetDirectories(sceneFolder).OrderBy(d => d, StringComparer.Ordinal).ToList();

            while (dirs.Count > settings.maxSnapshots)
            {
                string oldest = dirs[0];
                var m = LoadSnapshotMetadataIO(oldest, sceneName);
                if (m?.isPinned == true) { dirs.RemoveAt(0); continue; }

                try { Directory.Delete(oldest, true); }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SceneTimeMachine] Failed to delete old snapshot: {e.Message}");
                    break;
                }
                dirs.RemoveAt(0);
            }
        }

        private static void RefreshSnapshotsIO()
        {
            snapshots.Clear();
            if (!TryGetActiveSceneUnityPath(out var currentUnity)) return;

            string name = Path.GetFileNameWithoutExtension(currentUnity);
            string rootAbs = ToAbsoluteBackupRoot(settings.backupRoot);
            string sceneFolder = Path.Combine(rootAbs, name).Replace("\\", "/");
            if (!Directory.Exists(sceneFolder)) return;

            var dirs = Directory.GetDirectories(sceneFolder);
            foreach (var dir in dirs.OrderByDescending(d => Path.GetFileName(d), StringComparer.Ordinal))
            {
                var meta = LoadSnapshotMetadataIO(dir, name);
                if (meta == null)
                {
                    string stamp = Path.GetFileName(dir);
                    DateTime dt;
                    long size = 0;
                    string sceneFile = Directory.GetFiles(dir, "*.unity", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (sceneFile != null) size = new FileInfo(sceneFile).Length;

                    if (!DateTime.TryParseExact(stamp, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                    {
                        if (!DateTime.TryParse(stamp.Replace("_", " ").Replace("-", ":"), out dt))
                            dt = Directory.GetCreationTime(dir);
                    }

                    meta = new SceneSnapshot
                    {
                        path = dir.Replace("\\", "/"),
                        sceneName = name,
                        timestamp = dt,
                        fileSize = size,
                        description = "Legacy snapshot",
                        isAutoSnapshot = true,
                        isPinned = false,
                        type = SnapshotType.AutoInterval,
                        unityVersion = Application.unityVersion,
                        author = Environment.UserName
                    };
                }
                snapshots.Add(meta);
            }
        }

        private static void DeleteSnapshotIO(SceneSnapshot snapshot)
        {
            if (!EditorUtility.DisplayDialog("Delete Snapshot",
                    $"Delete snapshot from {snapshot.timestamp:yyyy-MM-dd HH:mm}?", "Delete", "Cancel"))
                return;

            try
            {
                Directory.Delete(snapshot.path, true);
                RefreshSnapshotsIO();
                if (selectedSnapshot == snapshot) selectedSnapshot = null;
                Debug.Log($"Deleted snapshot: {snapshot.sceneName} @ {snapshot.timestamp}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete snapshot: {e.Message}");
            }
        }

        private static void ShowCleanupDialog()
        {
            int choice = EditorUtility.DisplayDialogComplex("Cleanup Snapshots",
                "Choose cleanup method:", "Clean All (keep pinned)", "Delete Old (30d+)", "Cancel");
            switch (choice)
            {
                case 0: PerformFullCleanupIO(); break;
                case 1: PerformOldCleanupIO(); break;
            }
        }

        private static void PerformFullCleanupIO()
        {
            if (!TryGetActiveSceneUnityPath(out var currentUnity)) return;
            string name = Path.GetFileNameWithoutExtension(currentUnity);

            string rootAbs = ToAbsoluteBackupRoot(settings.backupRoot);
            string folder = Path.Combine(rootAbs, name).Replace("\\", "/");
            if (!Directory.Exists(folder)) return;

            int deleted = 0;
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var s = LoadSnapshotMetadataIO(dir, name);
                if (s == null || s.isPinned) continue;
                try { Directory.Delete(dir, true); deleted++; } catch { }
            }

            RefreshSnapshotsIO();
            EditorUtility.DisplayDialog("Cleanup Complete", $"Deleted {deleted} snapshots (kept pinned ones).", "OK");
        }

        private static void PerformOldCleanupIO()
        {
            if (!TryGetActiveSceneUnityPath(out var currentUnity)) return;
            string name = Path.GetFileNameWithoutExtension(currentUnity);

            string rootAbs = ToAbsoluteBackupRoot(settings.backupRoot);
            string folder = Path.Combine(rootAbs, name).Replace("\\", "/");
            if (!Directory.Exists(folder)) return;

            int deleted = 0;
            var cutoff = DateTime.Now.AddDays(-30);
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var s = LoadSnapshotMetadataIO(dir, name);
                if (s == null || s.isPinned) continue;
                if (s.timestamp < cutoff)
                {
                    try { Directory.Delete(dir, true); deleted++; } catch { }
                }
            }

            RefreshSnapshotsIO();
            EditorUtility.DisplayDialog("Cleanup Complete", $"Deleted {deleted} snapshots older than 30 days.", "OK");
        }

        private static void ShowAnalytics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("📈 SCENE TIME MACHINE ANALYTICS (Non-Intrusive) 📈");
            sb.AppendLine("═══════════════════════════════════════");
            sb.AppendLine($"Total Snapshots: {snapshots.Count}");
            sb.AppendLine($"Total Storage: {FormatBytes(totalStorageUsed)}");
            if (snapshots.Count > 0)
            {
                sb.AppendLine($"Average Size: {FormatBytes(totalStorageUsed / Math.Max(1, snapshots.Count))}");

                var byScene = snapshots.GroupBy(s => s.sceneName);
                sb.AppendLine("\nBY SCENE:");
                foreach (var g in byScene.OrderByDescending(g => g.Count()))
                {
                    long sum = g.Sum(x => x.fileSize);
                    sb.AppendLine($"• {g.Key}: {g.Count()} ({FormatBytes(sum)})");
                }

                var byType = snapshots.GroupBy(s => s.type);
                sb.AppendLine("\nBY TYPE:");
                foreach (var g in byType.OrderByDescending(g => g.Count()))
                    sb.AppendLine($"• {g.Key}: {g.Count()}");
            }

            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Analytics", "Report generated in Console.", "OK");
        }
    }
}
#endif
