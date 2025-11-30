#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Audio Bank Organizer - IMGUI UI rendering.
    /// </summary>
    public static partial class AudioBankOrganizerTool
    {
        public static void Draw()
        {
            try
            {
                InitStyles();
                DrawHeaderAndHints();
                DrawScanControls();

                if (!dataLoaded)
                {
                    DrawScanProgress();
                    return;
                }

                DrawStatistics();
                DrawFiltersAndSort();
                DrawAudioClipsList();
                DrawBatchActions();
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Audio Bank Organizer error: {e.Message}", MessageType.Error);
                Debug.LogError(e);
            }
        }

        // ---------- Styles ----------

        private static void InitStyles()
        {
            if (redStyle != null) return;
            redStyle   = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };
            greenStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.2f, 0.8f, 0.2f) } };
            yellowStyle= new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1.0f, 0.75f, 0.2f) } };
        }

        // ---------- Header & Tips ----------

        private static void DrawHeaderAndHints()
        {
            GUILayout.Space(8);
            var header = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("🎵 Advanced Audio Bank Organizer", header);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox(
                    "Best accuracy with 'Force Text' serialization (Edit > Project Settings > Editor). " +
                    "Binary/Mixed assets may not be fully searchable by GUID.",
                    MessageType.Info);
            }
        }

        // ---------- Scan Controls ----------

        private static void DrawScanControls()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🔍 Scan Controls:", EditorStyles.boldLabel);

            if (!isScanning)
            {
                EditorGUILayout.BeginHorizontal();
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button(dataLoaded ? "🔄 Refresh Scan" : "🚀 Start Full Scan", GUILayout.Height(28)))
                    StartScan();
                GUI.backgroundColor = prev;

                if (GUILayout.Button("📁 Scan Selected Folder", GUILayout.Height(28)))
                    StartFolderScan();
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(4);

                int columns = EditorGUIUtility.currentViewWidth < 600 ? 1 : 2;
                int toggleCount = 0;

                EditorGUILayout.BeginHorizontal();
                DrawScanToggle(ref toggleCount, columns, "Include Packages/", ref includePackages, 160);
                DrawScanToggle(ref toggleCount, columns, "Name-based heuristic", ref nameHeuristicSearch, 170);
                DrawScanToggle(ref toggleCount, columns, "Auto-optimize after scan", ref autoOptimizeOnScan, 200);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var prev = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("❌ Cancel Scan", GUILayout.Height(28)))
                    CancelScan();
                GUI.backgroundColor = prev;
            }

            EditorGUILayout.EndVertical();
        }

        private static void DrawScanToggle(ref int count, int columns, string label, ref bool value, float width)
        {
            value = EditorGUILayout.ToggleLeft(label, value, GUILayout.Width(width));
            count++;
            if (count % columns == 0)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }

        // ---------- Progress ----------

        private static void DrawScanProgress()
        {
            if (!isScanning)
            {
                EditorGUILayout.HelpBox(
                    "Click 'Start Full Scan' to analyze your audio bank.\n" +
                    "• Finds all AudioClips\n" +
                    "• Reads import settings (including platform overrides)\n" +
                    "• Builds GUID index streaming (low memory)\n" +
                    "• Detects usage and optimization opportunities",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("📊 Scanning...", EditorStyles.boldLabel);

            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.ProgressBar(rect, scanProgress, $"{scanStatus} {(int)(scanProgress * 100)}%");

            string phaseText = phase switch
            {
                0 => "Finding AudioClips",
                1 => "Analyzing clip properties",
                2 => "Collecting project files",
                3 => "Building GUID index (streaming)",
                4 => "Binding usages",
                _ => "Finalizing"
            };
            EditorGUILayout.LabelField($"Phase: {phaseText}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        // ---------- Statistics ----------

        private static void DrawStatistics()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📊 Statistics:", EditorStyles.boldLabel);
            if (GUILayout.Button(showStatistics ? "🔽" : "🔼", GUILayout.Width(28)))
                showStatistics = !showStatistics;
            EditorGUILayout.EndHorizontal();

            if (!showStatistics || audioClips.Count == 0)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            long  totalSize     = audioClips.Sum(c => c.fileSize);
            float totalDuration = audioClips.Sum(c => c.duration);
            int   used          = audioClips.Count(c => c.isUsed);
            int   unused        = audioClips.Count - used;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"📁 Total Clips: {audioClips.Count}");
            EditorGUILayout.LabelField($"💾 Total Size: {FmtBytes(totalSize)}");
            EditorGUILayout.LabelField($"⏱️ Total Duration: {FmtTime(totalDuration)}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"✅ Used: {used}",   greenStyle);
            EditorGUILayout.LabelField($"❌ Unused: {unused}", unused > 0 ? redStyle : greenStyle);
            EditorGUILayout.LabelField(
                $"💡 Potential Savings (Unused): {FmtBytes(audioClips.Where(c => !c.isUsed).Sum(c => c.fileSize))}",
                yellowStyle);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            int largeFiles  = audioClips.Count(c => c.fileSize  > LARGE_FILE_THRESHOLD);
            int longClips   = audioClips.Count(c => c.duration  > LONG_DURATION_THRESHOLD);
            int uncompressed= audioClips.Count(c => c.compressionFormat == AudioCompressionFormat.PCM);

            if (largeFiles + longClips + uncompressed > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("⚠️ Opportunities:", EditorStyles.boldLabel);
                if (largeFiles  > 0) EditorGUILayout.LabelField($"• Large files (> {FmtBytes(LARGE_FILE_THRESHOLD)}): {largeFiles}", yellowStyle);
                if (longClips   > 0) EditorGUILayout.LabelField($"• Long clips (> {LONG_DURATION_THRESHOLD:F0}s): {longClips}",        yellowStyle);
                if (uncompressed> 0) EditorGUILayout.LabelField($"• Uncompressed (PCM): {uncompressed}",                               yellowStyle);
            }

            EditorGUILayout.EndVertical();
        }

        // ---------- Filters & Sorting ----------

        private static void DrawFiltersAndSort()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🔧 Filters & Sorting:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            filterMode   = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(140));
            if (filterMode == FilterMode.SpecificFolder)
            {
                EditorGUILayout.LabelField("📁", GUILayout.Width(18));
                folderFilter = EditorGUILayout.TextField(folderFilter);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Sort:", GUILayout.Width(40));
            sortMode = (SortMode)EditorGUILayout.EnumPopup(sortMode, GUILayout.Width(160));
            if (GUILayout.Button(sortAscending ? "🔼" : "🔽", GUILayout.Width(30)))
                sortAscending = !sortAscending;

            if (GUILayout.Button("❌ Unused", GUILayout.Width(90))) filterMode = FilterMode.Unused;
            if (GUILayout.Button("📏 Large",  GUILayout.Width(90))) filterMode = FilterMode.LargeFiles;
            if (GUILayout.Button("🗑️ Clear",  GUILayout.Width(90)))
            {
                filterMode   = FilterMode.All;
                searchFilter = "";
                folderFilter = "";
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ---------- List ----------

        private static void DrawAudioClipsList()
        {
            var list = GetFilteredAndSortedClips();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField($"🎵 Audio Clips ({list.Count}):", EditorStyles.boldLabel);

            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox("No clips match the current filter.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(260));

            int columns = EditorGUIUtility.currentViewWidth < 600 ? 1 : 2;
            int index = 0;

            while (index < list.Count)
            {
                EditorGUILayout.BeginHorizontal();

                for (int col = 0; col < columns && index < list.Count; col++)
                {
                    var info = list[index];

                    EditorGUILayout.BeginVertical(info.isUsed ? GUI.skin.box : EditorStyles.helpBox, GUILayout.ExpandWidth(true));

                    var nameStyle = info.isUsed ? EditorStyles.boldLabel : redStyle;
                    if (GUILayout.Button(info.clip.name, nameStyle))
                        EditorGUIUtility.PingObject(info.clip);

                    GUILayout.Space(2);

                    var sizeStyle = info.fileSize > LARGE_FILE_THRESHOLD ? yellowStyle : EditorStyles.label;
                    GUILayout.Label($"Size: {FmtBytes(info.fileSize)}", sizeStyle);

                    var durStyle = info.duration > LONG_DURATION_THRESHOLD ? yellowStyle : EditorStyles.label;
                    GUILayout.Label($"Duration: {info.duration:F1}s", durStyle);

                    GUILayout.Label($"Freq: {info.frequency / 1000}k");

                    var fmtStyle = info.compressionFormat == AudioCompressionFormat.PCM ? yellowStyle : EditorStyles.label;
                    GUILayout.Label($"Format: {info.compressionFormat}", fmtStyle);

                    var usedStyle = info.isUsed ? greenStyle : redStyle;
                    GUILayout.Label(info.isUsed ? $"Used ({info.usedInFiles.Count})" : "Unused", usedStyle);

                    GUILayout.Space(4);

                    if (GUILayout.Button("📋 Details")) ShowClipDetails(info);
                    if (GUILayout.Button("💡 Tips"))    ShowOptimizationSuggestions(info);

                    if (!info.isUsed)
                    {
                        var prev = GUI.backgroundColor;
                        GUI.backgroundColor = new Color(1f, .4f, .4f);
                        if (GUILayout.Button("🗑 Trash"))
                        {
                            if (EditorUtility.DisplayDialog("Move to Trash?", $"Move '{info.clip.name}' to Trash?", "Yes", "No"))
                                TrashClip(info);
                        }
                        GUI.backgroundColor = prev;
                    }
                    else
                    {
                        if (GUILayout.Button("🔎 Refs"))
                            ShowRefsPopup(info);
                    }

                    EditorGUILayout.EndVertical();
                    index++;
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(6);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        // ---------- Batch Actions ----------

        private static void DrawBatchActions()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("🛠️ Batch Actions:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, .4f, .4f);
            if (GUILayout.Button("🗑️ Delete All Unused (Trash)", GUILayout.Height(26)))
                TrashAllUnused();
            GUI.backgroundColor = prev;

            GUI.backgroundColor = new Color(1f, .85f, .4f);
            if (GUILayout.Button("🔧 Optimize All Large/PCM", GUILayout.Height(26)))
                OptimizeAllLargeFiles();
            GUI.backgroundColor = prev;

            if (GUILayout.Button("📊 Export Report (CSV)", GUILayout.Height(26)))
                ExportReport();

            if (GUILayout.Button("💾 Backup Audio Bank", GUILayout.Height(26)))
                BackupAudioBank();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // ---------- Small popups (UI-owned) ----------

        private static void ShowClipDetails(AudioClipInfo c)
        {
            string details =
                $"🎵 {c.clip.name}\n" +
                $"Path: {c.path}\n" +
                $"Size: {FmtBytes(c.fileSize)}\n" +
                $"Duration: {FmtTime(c.duration)}\n" +
                $"Frequency: {c.frequency} Hz, Channels: {c.channels}\n" +
                $"Default: {c.compressionFormat} / Q:{c.quality:F2} / {c.loadType}\n" +
                $"Last Modified: {c.lastModified:yyyy-MM-dd HH:mm}\n" +
                $"Folder: {c.folderName}\n" +
                $"Used: {(c.isUsed ? "Yes" : "No")} ({c.usedInFiles.Count})\n";

            if (c.platformSettings.Count > 0)
            {
                details += "\nOverrides:\n";
                foreach (var kv in c.platformSettings)
                {
                    var s = kv.Value;
                    details += $"  • {kv.Key}: {s.compressionFormat} / Q:{s.quality:F2} / {s.loadType}\n";
                }
            }

            if (c.usedInFiles.Count > 0)
            {
                details += "\nUsed in (first 10):\n";
                foreach (var f in c.usedInFiles.Take(10)) details += $"  - {f}\n";
                if (c.usedInFiles.Count > 10) details += $"  ... +{c.usedInFiles.Count - 10} more\n";
            }

            EditorUtility.DisplayDialog("Audio Clip Details", details, "OK");
        }

        private static void ShowRefsPopup(AudioClipInfo c)
        {
            string msg = c.usedInFiles.Count == 0 ? "No references." : string.Join("\n", c.usedInFiles.Take(50));
            EditorUtility.DisplayDialog("References", msg, "OK");
        }
    }
}
#endif
