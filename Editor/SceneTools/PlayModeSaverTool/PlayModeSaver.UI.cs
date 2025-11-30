#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.SceneTools
{
    public static partial class PlayModeSaverTool
    {
        private static void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string status = Application.isPlaying ? "▶ PLAY MODE" : "⏹ EDIT MODE";
            var statusStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Application.isPlaying ? Color.green : Color.gray }
            };
            EditorGUILayout.LabelField(status, statusStyle, GUILayout.Width(100));

            EditorGUILayout.LabelField($"Snapshots: {snapshots.Count}", GUILayout.Width(100));

            if (Application.isPlaying && settings?.autoSaveEnabled == true)
            {
                double nextSave = settings.autoSaveInterval - (EditorApplication.timeSinceStartup - lastAutoSaveTime);
                EditorGUILayout.LabelField($"Next auto-save: {Mathf.Max(0, (int)nextSave)}s", GUILayout.Width(120));
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawQuickActions()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || settings == null);
            if (GUILayout.Button("📸 Capture Now", GUILayout.Height(30)))
            {
                CaptureSnapshot("Manual capture");
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("🗑 Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Snapshots", "Delete all snapshots?", "Yes", "No"))
                {
                    ClearSnapshots();
                }
            }

            if (GUILayout.Button("💾 Save to Disk", GUILayout.Height(30)))
            {
                SaveSnapshotsToDisk();
            }

            if (GUILayout.Button("📁 Load from Disk", GUILayout.Height(30)))
            {
                LoadSnapshots();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSnapshotsTab()
        {
            EditorGUILayout.LabelField("Captured Snapshots", EditorStyles.boldLabel);

            if (snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("No snapshots captured yet. Enter Play Mode and start making changes!", MessageType.Info);
                return;
            }

            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                DrawSnapshotItem(snapshot, i);
            }
        }

        private static void DrawSnapshotItem(PlayModeSnapshot snapshot, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();

            string icon = snapshot.isCheckpoint ? "⭐" : "📷";
            EditorGUILayout.LabelField(icon, GUILayout.Width(20));

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(snapshot.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(snapshot.timestamp.ToString("HH:mm:ss"), EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Compare", GUILayout.Width(70)))
            {
                selectedSnapshot = snapshot;
                GenerateDiff();
                selectedTabIndex = 2;
            }

            if (GUILayout.Button("Apply All", GUILayout.Width(70)))
            {
                ApplySnapshot(snapshot);
            }

            if (snapshot.isCheckpoint)
            {
                if (GUILayout.Button("Rename", GUILayout.Width(70)))
                {
                    RenameCheckpoint(snapshot);
                }
            }
            else
            {
                if (GUILayout.Button("→ Checkpoint", GUILayout.Width(70)))
                {
                    PromoteToCheckpoint(snapshot);
                }
            }

            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                snapshots.RemoveAt(index);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Components: {snapshot.componentSnapshots.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"Objects: {settings?.watchedObjects?.Count ?? 0}", EditorStyles.miniLabel, GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private static void DrawWatchListTab()
        {
            if (settings == null)
            {
                EditorGUILayout.HelpBox("No settings found. Create settings in the Settings tab first.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Object-Based Watch List", EditorStyles.boldLabel);

            // Add watched objects section
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Objects to Watch:", GUILayout.Width(150));
            if (GUILayout.Button("Add Selected Objects", GUILayout.Height(25)))
            {
                AddSelectedObjectsToWatchList();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Show watched objects
            EditorGUILayout.LabelField($"Watching {settings.watchedObjects.Count} Objects:", EditorStyles.boldLabel);

            if (settings.watchedObjects.Count == 0)
            {
                EditorGUILayout.HelpBox("No objects in watch list. Select GameObjects in the scene and click 'Add Selected Objects' above!", MessageType.Info);
            }
            else
            {
                for (int i = settings.watchedObjects.Count - 1; i >= 0; i--)
                {
                    var watchedObj = settings.watchedObjects[i];
                    DrawWatchedObjectItem(watchedObj, i);
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        private static void DrawWatchedObjectItem(WatchedObject watchedObj, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with object info
            EditorGUILayout.BeginHorizontal();

            // Enable toggle
            watchedObj.isEnabled = EditorGUILayout.Toggle(watchedObj.isEnabled, GUILayout.Width(20));

            // Object field (for reference and ping)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField(watchedObj.gameObject, typeof(GameObject), true, GUILayout.Width(150));
            EditorGUI.EndDisabledGroup();

            // Object name and path
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(watchedObj.gameObjectName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(watchedObj.gameObjectPath, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Watch all components toggle
            watchedObj.watchAllComponents = EditorGUILayout.Toggle("Watch All", watchedObj.watchAllComponents, GUILayout.Width(80));

            // Ping button
            if (GUILayout.Button("📍", GUILayout.Width(25)))
            {
                if (watchedObj.gameObject != null)
                    EditorGUIUtility.PingObject(watchedObj.gameObject);
            }

            // Remove button
            if (GUILayout.Button("❌", GUILayout.Width(25)))
            {
                settings.watchedObjects.RemoveAt(index);
                EditorUtility.SetDirty(settings);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
                return;
            }

            EditorGUILayout.EndHorizontal();

            // Component list (if not watching all)
            if (!watchedObj.watchAllComponents)
            {
                EditorGUILayout.LabelField("Watched Components:", EditorStyles.miniLabel);
                EditorGUI.indentLevel++;

                for (int j = watchedObj.watchedComponentTypes.Count - 1; j >= 0; j--)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"• {watchedObj.watchedComponentTypes[j]}", EditorStyles.miniLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        watchedObj.watchedComponentTypes.RemoveAt(j);
                        EditorUtility.SetDirty(settings);
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // Add component button
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add Component Type", GUILayout.Height(20)))
                {
                    ShowAddComponentToObjectDialog(watchedObj);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel--;
            }
            else
            {
                // Show current components (read-only)
                if (watchedObj.gameObject != null)
                {
                    EditorGUILayout.LabelField($"All Components ({GetComponentCount(watchedObj.gameObject)}):", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private static void DrawDiffViewerTab()
        {
            EditorGUILayout.LabelField("Diff Viewer", EditorStyles.boldLabel);

            if (selectedSnapshot == null)
            {
                EditorGUILayout.HelpBox("Select a snapshot to compare from the Snapshots tab.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Comparing with: {selectedSnapshot.name}");
            if (GUILayout.Button("Refresh Diff", GUILayout.Width(100)))
            {
                GenerateDiff();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply All Changes", GUILayout.Height(25)))
            {
                ApplyAllDiffs();
            }
            if (GUILayout.Button("Revert All Changes", GUILayout.Height(25)))
            {
                RevertAllDiffs();
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            diffScrollPos = EditorGUILayout.BeginScrollView(diffScrollPos);

            var changedDiffs = currentDiff.Where(cd => cd.hasChanges).ToList();

            if (changedDiffs.Count == 0)
            {
                EditorGUILayout.HelpBox("No differences found between current state and selected snapshot.", MessageType.Info);
            }
            else
            {
                foreach (var componentDiff in changedDiffs)
                {
                    DrawComponentDiff(componentDiff);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawComponentDiff(ComponentDiff componentDiff)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{componentDiff.componentType} on {componentDiff.gameObjectPath}", EditorStyles.boldLabel);

            if (GUILayout.Button("Apply Component", GUILayout.Width(120)))
            {
                ApplyComponentDiff(componentDiff);
            }

            if (GUILayout.Button("Revert Component", GUILayout.Width(120)))
            {
                RevertComponentDiff(componentDiff);
            }

            EditorGUILayout.EndHorizontal();

            foreach (var fieldDiff in componentDiff.fieldDiffs)
            {
                DrawFieldDiff(fieldDiff);
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(4);
        }

        private static void DrawFieldDiff(FieldDiff fieldDiff)
        {
            EditorGUILayout.BeginHorizontal();

            string statusIcon = fieldDiff.isApplied ? "✅" : "⚠️";
            EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));

            EditorGUILayout.LabelField(fieldDiff.fieldPath, GUILayout.Width(120));

            var oldStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = Color.white },
                focused = { textColor = Color.white }
            };
            oldStyle.normal.background = MakeColorTexture(new Color(0.8f, 0.2f, 0.2f, 0.3f));

            EditorGUILayout.LabelField("Saved:", GUILayout.Width(50));
            EditorGUILayout.SelectableLabel(fieldDiff.oldValue, oldStyle, GUILayout.Height(18));

            var newStyle = new GUIStyle(EditorStyles.textField)
            {
                normal = { textColor = Color.white },
                focused = { textColor = Color.white }
            };
            newStyle.normal.background = MakeColorTexture(new Color(0.2f, 0.8f, 0.2f, 0.3f));

            EditorGUILayout.LabelField("Current:", GUILayout.Width(55));
            EditorGUILayout.SelectableLabel(fieldDiff.newValue, newStyle, GUILayout.Height(18));

            if (GUILayout.Button("Apply Saved", GUILayout.Width(80)))
            {
                ApplyFieldDiff(fieldDiff);
            }

            if (GUILayout.Button("Keep Current", GUILayout.Width(80)))
            {
                RevertFieldDiff(fieldDiff);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            if (settings != null)
            {
                EditorGUILayout.LabelField($"Settings Asset: {AssetDatabase.GetAssetPath(settings)}", EditorStyles.miniLabel);
                if (GUILayout.Button("Ping Asset", GUILayout.Width(80)))
                {
                    EditorGUIUtility.PingObject(settings);
                }
            }
            else
            {
                EditorGUILayout.LabelField("No settings asset found", EditorStyles.miniLabel);
                if (GUILayout.Button("Create Settings", GUILayout.Width(100)))
                {
                    CreateSettingsAsset();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (settings == null) return;

            GUILayout.Space(10);

            EditorGUILayout.LabelField("Auto Save", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            settings.autoSaveEnabled = EditorGUILayout.Toggle("Enable Auto Save", settings.autoSaveEnabled);
            EditorGUI.BeginDisabledGroup(!settings.autoSaveEnabled);
            settings.autoSaveInterval = EditorGUILayout.FloatField("Interval (seconds)", settings.autoSaveInterval);
            settings.maxSnapshots = EditorGUILayout.IntField("Max Snapshots", settings.maxSnapshots);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            EditorGUILayout.LabelField("User Interface", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            settings.showToastNotifications = EditorGUILayout.Toggle("Show Toast Notifications", settings.showToastNotifications);
            settings.autoMarkSceneDirty = EditorGUILayout.Toggle("Auto Mark Scene Dirty", settings.autoMarkSceneDirty);
            EditorGUILayout.EndVertical();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(settings);
            }
        }

        private static void CreateSettingsAsset()
        {
            string dir = Path.GetDirectoryName(settingsPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            settings = ScriptableObject.CreateInstance<PlayModeSaverSettings>();

            AssetDatabase.CreateAsset(settings, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created PlayModeSaver settings asset at: {settingsPath}");
        }

        private static void ShowAddComponentToObjectDialog(WatchedObject watchedObj)
        {
            if (watchedObj.gameObject == null) return;

            var availableComponents = watchedObj.gameObject.GetComponents<Component>()
                .Where(c => c != null && !watchedObj.watchedComponentTypes.Contains(c.GetType().Name))
                .Select(c => c.GetType().Name)
                .ToList();

            if (availableComponents.Count == 0)
            {
                EditorUtility.DisplayDialog("No Components", "All components on this object are already being watched.", "OK");
                return;
            }

            // Simple dialog for now
            string componentType = "";
            if (ShowTextInputDialog("Add Component", $"Available: {string.Join(", ", availableComponents)}\n\nEnter component type:", ref componentType))
            {
                if (availableComponents.Contains(componentType))
                {
                    watchedObj.watchedComponentTypes.Add(componentType);
                    EditorUtility.SetDirty(settings);
                }
            }
        }

        private static int GetComponentCount(GameObject go)
        {
            return go != null ? go.GetComponents<Component>().Length : 0;
        }

        private static void AddSelectedObjectsToWatchList()
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select one or more GameObjects in the scene to add to the watch list.", "OK");
                return;
            }

            int addedCount = 0;
            foreach (var go in Selection.gameObjects)
            {
                bool alreadyExists = settings.watchedObjects.Any(w => w.gameObject == go);
                if (!alreadyExists)
                {
                    settings.watchedObjects.Add(new WatchedObject(go));
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(settings);
                ShowToast($"Added {addedCount} objects to watch list");
            }
            else
            {
                EditorUtility.DisplayDialog("Already Watched", "All selected objects are already in the watch list.", "OK");
            }
        }
    }
}
#endif
