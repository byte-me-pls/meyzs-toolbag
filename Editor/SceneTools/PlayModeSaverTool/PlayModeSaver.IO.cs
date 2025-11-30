#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        // File operations
        private static void SaveSnapshotsToDisk()
        {
            try
            {
                if (!Directory.Exists(snapshotsDirectory))
                    Directory.CreateDirectory(snapshotsDirectory);

                string filename = $"PlayModeSnapshots_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
                string filepath = Path.Combine(snapshotsDirectory, filename);

                var data = new SnapshotContainer { snapshots = snapshots.ToArray(), timestamp = System.DateTime.Now.ToString() };
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(filepath, json);

                ShowToast($"Saved {snapshots.Count} snapshots to {filename}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save snapshots: {e.Message}");
            }
        }

        private static void LoadSnapshots()
        {
            try
            {
                if (!Directory.Exists(snapshotsDirectory))
                    return;

                var files = Directory.GetFiles(snapshotsDirectory, "PlayModeSnapshots_*.json");
                if (files.Length == 0)
                    return;

                var latestFile = files.OrderByDescending(f => File.GetCreationTime(f)).First();
                string json = File.ReadAllText(latestFile);

                var data = JsonUtility.FromJson<SnapshotContainer>(json);
                if (data?.snapshots != null)
                {
                    snapshots = data.snapshots.ToList();
                    ShowToast($"Loaded {snapshots.Count} snapshots from disk");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load snapshots: {e.Message}");
            }
        }

        private static void ClearSnapshots()
        {
            snapshots.Clear();
            currentDiff.Clear();
            selectedSnapshot = null;
            ShowToast("Cleared all snapshots");
        }

        // Dialog and UI methods
        private static void ShowExitDialog()
        {
            if (snapshots.Count == 0) return;

            int choice = EditorUtility.DisplayDialogComplex(
                "Play Mode Changes Detected",
                $"You have {snapshots.Count} snapshots from Play Mode.\n\nWhat would you like to do?",
                "Apply Latest", "Review Changes", "Discard All");

            switch (choice)
            {
                case 0: // Apply Latest
                    if (snapshots.Count > 0)
                        ApplySnapshot(snapshots.Last());
                    break;
                case 1: // Review Changes
                    break;
                case 2: // Discard All
                    ClearSnapshots();
                    break;
            }
        }

        private static void PromoteToCheckpoint(PlayModeSnapshot snapshot)
        {
            string newCheckpointName = snapshot.name;
            if (ShowTextInputDialog("Create Checkpoint", "Enter checkpoint name:", ref newCheckpointName))
            {
                snapshot.name = newCheckpointName;
                snapshot.isCheckpoint = true;
                ShowToast($"Created checkpoint '{snapshot.name}'");
            }
        }

        private static void RenameCheckpoint(PlayModeSnapshot snapshot)
        {
            string tempName = snapshot.name;
            if (ShowTextInputDialog("Rename Checkpoint", "Enter new name:", ref tempName))
            {
                snapshot.name = tempName;
                ShowToast($"Renamed checkpoint to '{snapshot.name}'");
            }
        }
    }
}
#endif
