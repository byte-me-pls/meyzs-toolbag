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
        private static void DrawHeader()
        {
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(TOOL_NAME, headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);

            float viewW = EditorGUIUtility.currentViewWidth - 40f;
            bool isWide = viewW > 500f;
            bool isMedium = viewW > 300f && viewW <= 500f;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (isWide)
            {
                EditorGUILayout.BeginHorizontal();
                var statusStyle = settings.toolEnabled ? greenStyle : redStyle;
                EditorGUILayout.LabelField($"Status: {(settings.toolEnabled ? "🟢 Active" : "🔴 Disabled")}", statusStyle, GUILayout.Width(120));

                if (TryGetActiveSceneUnityPath(out var sceneUnityPath))
                {
                    string sceneName = Trunc(Path.GetFileNameWithoutExtension(sceneUnityPath), 15);
                    EditorGUILayout.LabelField($"Scene: {sceneName}", GUILayout.Width(150));

                    int count = snapshots.Count(s => s.sceneName == Path.GetFileNameWithoutExtension(sceneUnityPath));
                    EditorGUILayout.LabelField($"Snapshots: {count}", GUILayout.Width(100));

                    if (settings.toolEnabled && settings.autoSnapshotByInterval && nextSnapshotTime > EditorApplication.timeSinceStartup)
                    {
                        double remaining = nextSnapshotTime - EditorApplication.timeSinceStartup;
                        EditorGUILayout.LabelField($"Next: {TimeSpan.FromSeconds(remaining):mm\\:ss}");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Scene: None (save scene to enable)", yellowStyle);
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (isMedium)
            {
                EditorGUILayout.BeginHorizontal();
                var statusStyle = settings.toolEnabled ? greenStyle : redStyle;
                EditorGUILayout.LabelField($"{(settings.toolEnabled ? "🟢" : "🔴")} {(settings.toolEnabled ? "Active" : "Disabled")}", statusStyle);

                if (TryGetActiveSceneUnityPath(out var sceneUnityPath))
                {
                    string sceneName = Trunc(Path.GetFileNameWithoutExtension(sceneUnityPath), 12);
                    EditorGUILayout.LabelField($"Scene: {sceneName}");
                }
                else
                {
                    EditorGUILayout.LabelField("No Scene", yellowStyle);
                }
                EditorGUILayout.EndHorizontal();

                if (TryGetActiveSceneUnityPath(out sceneUnityPath))
                {
                    EditorGUILayout.BeginHorizontal();
                    int count = snapshots.Count(s => s.sceneName == Path.GetFileNameWithoutExtension(sceneUnityPath));
                    EditorGUILayout.LabelField($"Snapshots: {count}");

                    if (settings.toolEnabled && settings.autoSnapshotByInterval && nextSnapshotTime > EditorApplication.timeSinceStartup)
                    {
                        double remaining = nextSnapshotTime - EditorApplication.timeSinceStartup;
                        EditorGUILayout.LabelField($"Next: {TimeSpan.FromSeconds(remaining):mm\\:ss}");
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                var statusStyle = settings.toolEnabled ? greenStyle : redStyle;
                EditorGUILayout.LabelField(settings.toolEnabled ? "🟢" : "🔴", statusStyle, GUILayout.Width(20));

                if (TryGetActiveSceneUnityPath(out var sceneUnityPath))
                {
                    string sceneName = Trunc(Path.GetFileNameWithoutExtension(sceneUnityPath), 10);
                    EditorGUILayout.LabelField(sceneName);

                    int count = snapshots.Count(s => s.sceneName == Path.GetFileNameWithoutExtension(sceneUnityPath));
                    EditorGUILayout.LabelField($"({count})", GUILayout.Width(30));
                }
                else
                {
                    EditorGUILayout.LabelField("No Scene", yellowStyle);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawQuickActions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🚀 Quick Actions:", EditorStyles.boldLabel);

            float viewW = EditorGUIUtility.currentViewWidth;
            float pad = 36f;
            float colW = Mathf.Max(120f, (viewW - pad) * 0.5f);
            GUILayoutOption halfW = GUILayout.Width(colW);

            bool hasScene = TryGetActiveSceneUnityPath(out var activePath);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!hasScene || !settings.toolEnabled))
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("📸 Take Snapshot", GUILayout.Height(30), halfW))
                    TakeManualSnapshot(activePath);
                GUI.backgroundColor = prev;

                if (GUILayout.Button("⭐ Milestone", GUILayout.Height(30), halfW))
                    TakeMilestoneSnapshot(activePath);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 Refresh", GUILayout.Height(26), halfW))
                RefreshSnapshotsIO();

            if (GUILayout.Button("🗑️ Cleanup", GUILayout.Height(26), halfW))
                ShowCleanupDialog();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📊 Analytics", GUILayout.Height(26), halfW))
                ShowAnalytics();

            if (GUILayout.Button("⚙️ Settings", GUILayout.Height(26), halfW))
                showSettings = !showSettings;
            EditorGUILayout.EndHorizontal();

            if (!hasScene)
                EditorGUILayout.HelpBox("💡 Save your scene to enable snapshots.", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private static void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            showStatistics = EditorGUILayout.Foldout(showStatistics, "📊 Statistics:", true, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (!showStatistics)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            UpdateStatistics();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"Total Snapshots: {totalSnapshots}");
            EditorGUILayout.LabelField($"Storage Used: {FormatBytes(totalStorageUsed)}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            var auto = snapshots.Count(s => s.isAutoSnapshot);
            var manual = snapshots.Count - auto;
            EditorGUILayout.LabelField($"Auto: {auto} | Manual: {manual}");
            var pinned = snapshots.Count(s => s.isPinned);
            EditorGUILayout.LabelField($"Pinned: {pinned}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawViewControls()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🎛️ View Controls:", EditorStyles.boldLabel);

            float viewW = EditorGUIUtility.currentViewWidth - 40f;
            bool isWide = viewW > 550f;
            bool isMedium = viewW > 350f && viewW <= 550f;

            if (isWide)
            {
                EditorGUILayout.BeginHorizontal();
                float labelWidth = 40f;
                float enumWidth = Mathf.Max(80f, (viewW - 200f) / 3f);

                EditorGUILayout.LabelField("View:", GUILayout.Width(labelWidth));
                viewMode = (ViewMode)EditorGUILayout.EnumPopup(viewMode, GUILayout.Width(enumWidth));

                EditorGUILayout.LabelField("Filter:", GUILayout.Width(45));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(enumWidth));

                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(enumWidth));

                GUILayout.Space(5);
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.MinWidth(60));

                if (GUILayout.Button("Clear", GUILayout.Width(45)))
                {
                    searchFilter = "";
                    filterMode = FilterMode.All;
                }
                EditorGUILayout.EndHorizontal();
            }
            else if (isMedium)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("View:", GUILayout.Width(40));
                viewMode = (ViewMode)EditorGUILayout.EnumPopup(viewMode, GUILayout.MinWidth(80));

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Filter:", GUILayout.Width(45));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.MinWidth(80));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.MinWidth(80));

                GUILayout.Space(10);
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.MinWidth(50));

                if (GUILayout.Button("Clear", GUILayout.Width(45)))
                {
                    searchFilter = "";
                    filterMode = FilterMode.All;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("View:", GUILayout.Width(35));
                viewMode = (ViewMode)EditorGUILayout.EnumPopup(viewMode);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Filter:", GUILayout.Width(35));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Sort:", GUILayout.Width(35));
                sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
                searchFilter = EditorGUILayout.TextField(searchFilter);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    searchFilter = "";
                    filterMode = FilterMode.All;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawSnapshotList()
        {
            var list = GetFilteredAndSortedSnapshots();

            EditorGUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();
            var rect = GUILayoutUtility.GetRect(new GUIContent($"📁 Snapshots ({list.Count}):"), EditorStyles.boldLabel);
            bool clicked = Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition);
            if (clicked) { showSnapshotList = !showSnapshotList; Event.current.Use(); }

            string arrow = showSnapshotList ? "▼" : "▶";
            GUI.Label(rect, $"{arrow} 📁 Snapshots ({list.Count}):", EditorStyles.boldLabel);

            if (showSnapshotList)
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("▲", EditorStyles.miniButton, GUILayout.Width(20))) snapshotListHeight = Mathf.Min(snapshotListHeight + 50f, 600f);
                if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(20))) snapshotListHeight = Mathf.Max(snapshotListHeight - 50f, 100f);
            }
            EditorGUILayout.EndHorizontal();

            if (!showSnapshotList) { EditorGUILayout.EndVertical(); return; }

            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox(snapshots.Count == 0 ? "No snapshots yet. Take your first snapshot!" :
                    "No snapshots match current filter.", MessageType.Info);
                EditorGUILayout.EndVertical(); return;
            }

            switch (viewMode)
            {
                case ViewMode.List:     DrawListView(list); break;
                case ViewMode.Timeline: DrawTimelineView(list); break;
                case ViewMode.Compact:  DrawCompactView(list); break;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawListView(System.Collections.Generic.List<SceneSnapshot> list)
        {
            float viewW = EditorGUIUtility.currentViewWidth - 40f;
            bool isWide = viewW > 500f;
            bool isMedium = viewW > 350f && viewW <= 500f;

            float pinWidth = 25f;
            float actionsWidth = isWide ? 80f : 60f;
            float typeWidth = isWide ? 40f : 0f;
            float sizeWidth = isWide ? 60f : (isMedium ? 50f : 0f);

            float remainingWidth = viewW - pinWidth - actionsWidth - typeWidth - sizeWidth - 20f;
            float sceneWidth = Mathf.Max(80f, remainingWidth * 0.4f);
            float timeWidth = Mathf.Max(80f, remainingWidth * 0.6f);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("📌", GUILayout.Width(pinWidth));
            GUILayout.Label("Scene", GUILayout.Width(sceneWidth));
            GUILayout.Label("Time", GUILayout.Width(timeWidth));
            if (isWide) GUILayout.Label("T", GUILayout.Width(typeWidth));
            if (sizeWidth > 0) GUILayout.Label("Size", GUILayout.Width(sizeWidth));
            GUILayout.Label("Actions", GUILayout.Width(actionsWidth));
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Height(snapshotListHeight));
            foreach (var s in list)
                DrawSnapshotItemRow(s, isWide, sceneWidth, timeWidth, typeWidth, sizeWidth, actionsWidth);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSnapshotItemRow(SceneSnapshot s, bool isWide, float sceneWidth, float timeWidth, float typeWidth, float sizeWidth, float actionsWidth)
        {
            bool isSelected = selectedSnapshot == s;
            EditorGUILayout.BeginHorizontal(isSelected ? EditorStyles.helpBox : GUIStyle.none);

            bool wasPinned = s.isPinned;
            s.isPinned = EditorGUILayout.Toggle(s.isPinned, GUILayout.Width(25));
            if (s.isPinned != wasPinned) SaveSnapshotMetadataIO(s);

            GUILayout.Label(Trunc(s.sceneName, 15), GUILayout.Width(sceneWidth));

            var timeStyle = s.isAutoSnapshot ? EditorStyles.label : boldStyle;
            GUILayout.Label(GetRelativeTimeString(s.timestamp), timeStyle, GUILayout.Width(timeWidth));

            if (isWide) GUILayout.Label(GetSnapshotTypeIcon(s.type), GUILayout.Width(typeWidth));

            if (sizeWidth > 0)
            {
                var sizeStyle = s.fileSize > 1024 * 1024 ? yellowStyle : EditorStyles.label;
                string sizeText = FormatBytes(s.fileSize);
                if (!isWide && sizeText.Contains(" ")) sizeText = sizeText.Replace(" ", "");
                GUILayout.Label(sizeText, sizeStyle, GUILayout.Width(sizeWidth));
            }

            EditorGUILayout.BeginHorizontal(GUILayout.Width(actionsWidth));
            float bs = isWide ? 20f : 18f;

            if (GUILayout.Button("📋", GUILayout.Width(bs), GUILayout.Height(bs)))
                selectedSnapshot = selectedSnapshot == s ? null : s;

            if (GUILayout.Button("📁", GUILayout.Width(bs), GUILayout.Height(bs)))
                RevealInExplorer(s.path);

            if (!s.isPinned && GUILayout.Button("🗑️", GUILayout.Width(bs), GUILayout.Height(bs)))
                DeleteSnapshotIO(s);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTimelineView(System.Collections.Generic.List<SceneSnapshot> list)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Height(snapshotListHeight));

            var groups = list.GroupBy(x => x.timestamp.Date).OrderByDescending(g => g.Key);
            foreach (var g in groups)
            {
                EditorGUILayout.LabelField(g.Key.ToString("yyyy-MM-dd"), EditorStyles.boldLabel);
                foreach (var s in g.OrderByDescending(x => x.timestamp))
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    EditorGUILayout.LabelField($"  {s.timestamp:HH:mm}", GUILayout.Width(60));
                    EditorGUILayout.LabelField(Trunc(s.sceneName, 18), GUILayout.Width(130));
                    EditorGUILayout.LabelField(GetSnapshotTypeIcon(s.type), GUILayout.Width(35));
                    if (GUILayout.Button("Details", GUILayout.Width(70))) selectedSnapshot = s;
                    if (GUILayout.Button("Folder", GUILayout.Width(60))) RevealInExplorer(s.path);
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawCompactView(System.Collections.Generic.List<SceneSnapshot> list)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false, GUILayout.Height(snapshotListHeight));

            foreach (var s in list)
            {
                EditorGUILayout.BeginHorizontal();
                string label = $"{GetSnapshotTypeIcon(s.type)} {s.sceneName} - {GetRelativeTimeString(s.timestamp)}";
                if (GUILayout.Button(label, EditorStyles.label)) selectedSnapshot = s;
                if (GUILayout.Button("Folder", GUILayout.Width(60))) RevealInExplorer(s.path);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSnapshotDetails()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📋 Snapshot Details:", EditorStyles.boldLabel);
            if (GUILayout.Button("❌", GUILayout.Width(24))) selectedSnapshot = null;
            EditorGUILayout.EndHorizontal();

            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos, GUILayout.Height(140));
            EditorGUILayout.LabelField($"Scene: {selectedSnapshot.sceneName}");
            EditorGUILayout.LabelField($"Timestamp: {selectedSnapshot.timestamp:yyyy-MM-dd HH:mm:ss}");
            EditorGUILayout.LabelField($"Type: {selectedSnapshot.type}");
            EditorGUILayout.LabelField($"Size: {FormatBytes(selectedSnapshot.fileSize)}");
            if (selectedSnapshot.sceneObjectCount > 0) EditorGUILayout.LabelField($"Objects: {selectedSnapshot.sceneObjectCount}");
            if (!string.IsNullOrEmpty(selectedSnapshot.unityVersion)) EditorGUILayout.LabelField($"Unity: {selectedSnapshot.unityVersion}");
            if (!string.IsNullOrEmpty(selectedSnapshot.author)) EditorGUILayout.LabelField($"Author: {selectedSnapshot.author}");
            if (!string.IsNullOrEmpty(selectedSnapshot.tags)) EditorGUILayout.LabelField($"Tags: {selectedSnapshot.tags}");

            if (selectedSnapshot.dependencies != null && selectedSnapshot.dependencies.Count > 0)
            {
                EditorGUILayout.LabelField("Dependencies (first):", EditorStyles.boldLabel);
                foreach (var d in selectedSnapshot.dependencies.Take(10))
                    EditorGUILayout.LabelField($"• {d}", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Description:", GUILayout.Width(90));
            selectedSnapshot.description = EditorGUILayout.TextField(selectedSnapshot.description);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Tags:", GUILayout.Width(90));
            selectedSnapshot.tags = EditorGUILayout.TextField(selectedSnapshot.tags);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("💾 Save Changes"))
                SaveSnapshotMetadataIO(selectedSnapshot);

            EditorGUILayout.EndVertical();
        }

        private static void DrawSettings()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚙️ Settings:", EditorStyles.boldLabel);
            if (GUILayout.Button("❌", GUILayout.Width(24))) showSettings = false;
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginChangeCheck();
            settings.toolEnabled = EditorGUILayout.ToggleLeft("🟢 Tool Enabled", settings.toolEnabled);
            settings.snapshotInterval = EditorGUILayout.Slider("⏰ Interval (min)", settings.snapshotInterval, 0.1f, 120f);
            settings.maxSnapshots = EditorGUILayout.IntSlider("📊 Max Snapshots", settings.maxSnapshots, 1, 200);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📁 Backup Root:", GUILayout.Width(100));
            settings.backupRoot = EditorGUILayout.TextField(settings.backupRoot);
            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                string selectedPath = ShowFolderPicker();
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    settings.backupRoot = selectedPath;
                    EditorUtility.SetDirty(settings);
                }
            }
            EditorGUILayout.EndHorizontal();

            string resolvedPath = ToAbsoluteBackupRoot(settings.backupRoot);
            EditorGUILayout.LabelField($"Resolved: {GetDisplayPath(resolvedPath)}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quick presets:", EditorStyles.miniLabel, GUILayout.Width(90));
            if (GUILayout.Button("Library", EditorStyles.miniButton, GUILayout.Width(60)))
            { settings.backupRoot = "Library/MeyzToolbag/SceneBackups"; EditorUtility.SetDirty(settings); }
            if (GUILayout.Button("Assets", EditorStyles.miniButton, GUILayout.Width(60)))
            { settings.backupRoot = "Assets/SceneBackups"; EditorUtility.SetDirty(settings); }
            if (GUILayout.Button("Desktop", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                settings.backupRoot = System.IO.Path.Combine(desktopPath, "Unity Scene Backups").Replace("\\", "/");
                EditorUtility.SetDirty(settings);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Auto Options:", EditorStyles.boldLabel);
            settings.autoSnapshotOnSave = EditorGUILayout.ToggleLeft("💾 On Scene Save", settings.autoSnapshotOnSave);
            settings.autoSnapshotOnPlay = EditorGUILayout.ToggleLeft("▶️ On Play Mode", settings.autoSnapshotOnPlay);
            settings.autoSnapshotByInterval = EditorGUILayout.ToggleLeft("⏱️ By Interval", settings.autoSnapshotByInterval);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Advanced:", EditorStyles.boldLabel);
            settings.includeDependencies = EditorGUILayout.ToggleLeft("🔗 Include Dependencies", settings.includeDependencies);
            using (new EditorGUI.DisabledScope(!settings.includeDependencies))
            {
                settings.dependenciesListOnly = EditorGUILayout.ToggleLeft("📝 List Names Only (no copy)", settings.dependenciesListOnly);
                settings.maxDependencyCopy = EditorGUILayout.IntSlider("📦 Max Dep Copies", settings.maxDependencyCopy, 0, 200);
            }
            settings.maxSnapshotSizeMB = EditorGUILayout.FloatField("📏 Max Size (MB, warn)", settings.maxSnapshotSizeMB);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(settings);
                ApplyEnableStateSubscriptions(settings.toolEnabled);
                ScheduleNextTick();
            }

            EditorGUILayout.EndVertical();
        }

        // --- UI helpers ---
        private static string Trunc(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max - 3) + "...";

        private static string ShowFolderPicker()
        {
            string currentPath = ToAbsoluteBackupRoot(settings.backupRoot);
            if (!System.IO.Directory.Exists(currentPath))
            {
                try { System.IO.Directory.CreateDirectory(currentPath); }
                catch { currentPath = ProjectRoot; }
            }

            string selectedPath = EditorUtility.OpenFolderPanel("Select Backup Folder", currentPath, "");
            if (string.IsNullOrEmpty(selectedPath)) return null;

            selectedPath = selectedPath.Replace("\\", "/");
            string relativePath = MakeRelativeToProject(selectedPath);
            return relativePath ?? selectedPath;
        }

        private static string MakeRelativeToProject(string absolutePath)
        {
            try
            {
                string projectRoot = ProjectRoot;
                if (absolutePath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string relative = absolutePath.Substring(projectRoot.Length);
                    if (relative.StartsWith("/")) relative = relative.Substring(1);
                    return relative;
                }
            }
            catch { }
            return null;
        }

        private static string GetDisplayPath(string path) => path.Length > 60 ? "..." + path.Substring(path.Length - 57) : path;
    }
}
#endif
