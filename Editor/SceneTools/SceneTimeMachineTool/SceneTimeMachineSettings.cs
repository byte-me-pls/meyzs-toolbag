#if UNITY_EDITOR
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    [CreateAssetMenu(fileName = "SceneTimeMachineSettings", menuName = "MeyzToolbag/Scene Tools/Scene Time Machine Settings")]
    public class SceneTimeMachineSettings : ScriptableObject
    {
        [Header("🕰️ Scene Time Machine (Non-Intrusive)")]
        [Tooltip("Enable or disable the tool. When enabled, autos (below) can run.")]
        public bool toolEnabled = true;

        [Header("Backup Settings")]
        [Tooltip("Where scene backups will be stored.")]
        public string backupFolder = "Assets/SceneBackups";

        [Tooltip("Interval in minutes between automatic snapshots.")]
        [Range(0.1f, 120f)]
        public float snapshotInterval = 5f;

        [Tooltip("Maximum snapshots to keep per scene.")]
        [Range(1, 200)]
        public int maxSnapshots = 20;

        [Tooltip("Backup root folder. If starts with 'Assets/', will be resolved under project. Recommended: Library/MeyzToolbag/SceneBackups")]
        public string backupRoot = "Library/MeyzToolbag/SceneBackups";

        [Header("📸 Auto Options (file copy only; no scene manipulation)")]
        [Tooltip("Automatically snapshot on scene save.")]
        public bool autoSnapshotOnSave = true;

        [Tooltip("Automatically snapshot on play mode enter (pre-play).")]
        public bool autoSnapshotOnPlay = true;

        [Tooltip("Automatically snapshot by interval.")]
        public bool autoSnapshotByInterval = false;

        [Header("🔧 Advanced")]
        [Tooltip("Include (copy) a limited set of scene dependencies beside the snapshot (pure IO).")]
        public bool includeDependencies = false;

        [Tooltip("Max dependency files to copy (to keep it light).")]
        [Range(0, 200)]
        public int maxDependencyCopy = 50;

        [Tooltip("Only copy dependency file names (no content). Keeps metadata small (metadata.json lists them).")]
        public bool dependenciesListOnly = true;

        [Tooltip("Maximum scene file size for snapshots (MB). 0 = no limit. Only warns; does not block.")]
        [Range(0f, 2048f)]
        public float maxSnapshotSizeMB = 200f;
    }
}
#endif
