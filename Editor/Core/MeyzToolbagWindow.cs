#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MeyzsToolBag.Editor.Animation;
using MeyzsToolBag.Editor.Performance;
using MeyzsToolBag.Editor.SceneTools;
using MeyzsToolBag.Editor.Transform;
using MeyzsToolBag.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Core
{
    public class MeyzToolbagWindow : EditorWindow
    {
        private enum ToolCategory
        {
            Favorites,
            Animation,
            SceneTools,
            Transform,
            Utility,
            Performance
        }

        private struct ToolData
        {
            public string Icon;
            public string Title;
            public string Description;
            public string MenuPath;
            public Func<bool> AvailabilityCheck;
            public Action LaunchAction;

            public string HelpURL;
            public string ShortcutPrefKey;
            public string DefaultShortcut;
        }

        private ToolCategory selectedCategory;
        private Vector2 scrollPosition;
        private string searchText = string.Empty;
        private HashSet<string> favoriteTools;

        // active single-tool render
        private ToolData activeToolData;
        private Action renderActiveTool = null;

        private const string FAVORITES_KEY = "MeyzToolbag.Favorites";
        private const string VERSION = "v0.1.0";

        // Tüm menü yollarını TEK standarda çektim: "MeyzToolbag/<Category>/<Tool>"
        private static readonly Dictionary<string, string> ToolIdentifierMap =
            new Dictionary<string, string>
        {
            // Animation
            { "ForcePosePreview", "MeyzToolbag/Animation/Force Pose Preview" },

            // Utility
            { "BatchRename", "MeyzToolbag/Utility/Batch Rename" },
            { "AssetUsageFinder", "MeyzToolbag/Utility/Asset Usage Finder" },
            { "MissingScriptsCleaner", "MeyzToolbag/Utility/Missing Scripts Cleaner" },
            { "QuickMaterialSwapper", "MeyzToolbag/Utility/Quick Material Swapper" },
            { "AudioBankOrganizer", "MeyzToolbag/Utility/Audio Bank Organizer" },

            // Transform
            { "PivotChange", "MeyzToolbag/Transform/Pivot Editor" },

            // Scene Tools
            { "LinearArray", "MeyzToolbag/Scene Tools/Linear Duplication Tool" },
            { "CircularArray", "MeyzToolbag/Scene Tools/Circular Duplication Tool" },
            { "GridArray", "MeyzToolbag/Scene Tools/Grid Duplication Tool" },
            { "SplineArray", "MeyzToolbag/Scene Tools/Spline Duplication Tool" },
            { "RandomAreaSpawn", "MeyzToolbag/Scene Tools/Random Area Duplication Tool" },

            // Performance
            { "SimpleTextureAtlasPacker", "MeyzToolbag/Performance/Simple Texture Atlas Packer" },
            { "PreBuildSizeEstimator", "MeyzToolbag/Performance/PreBuild Size Estimator Tool" },
            { "NullSafetyAnalyzer", "MeyzToolbag/Performance/NullSafetyAnalyzerTool" },

            // Extras
            { "SceneTimeMachine", "MeyzToolbag/Scene Tools/Scene Time Machine" },
            { "PlayModeSaverPro", "MeyzToolbag/Utility/Play Mode Saver Pro" },
        };

        public static void OpenWindow()
        {
            var window = GetWindow<MeyzToolbagWindow>("Meyz's Toolbag");
            window.minSize = new Vector2(520, 580);
        }

        private void OnEnable() => LoadFavorites();

        // Repaint sık (shortcut yakalamak için)
        private void OnInspectorUpdate() => Repaint();

        private void LoadFavorites()
        {
            string data = EditorPrefs.GetString(FAVORITES_KEY, "");
            favoriteTools = new HashSet<string>(data.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)));
        }

        private void SaveFavorites()
        {
            string data = string.Join(";", favoriteTools);
            EditorPrefs.SetString(FAVORITES_KEY, data);
        }

        private void OnGUI()
        {
            HandleShortcuts(Event.current);

            DrawHeader();
            GUILayout.Space(10);

            // aktif tool açıkken onun UI'ını render et
            if (renderActiveTool != null)
            {
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
                renderActiveTool.Invoke();
                EditorGUILayout.EndScrollView();
                GUILayout.Space(10);

                // aktif tool için shortcut editörü
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Shortcut:", GUILayout.Width(60));
                string current = GetShortcut(activeToolData);
                string edited = EditorGUILayout.TextField(current);
                if (edited != current) SetShortcut(activeToolData, edited);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);

                if (GUILayout.Button("← Back to Menu", GUILayout.Height(30)))
                {
                    renderActiveTool = null;
                    return;
                }

                DrawFooter();
                return;
            }

            // ana menü
            DrawToolbar();
            GUILayout.Space(5);

            searchText = EditorGUILayout.TextField("Search", searchText);
            GUILayout.Space(10);

            DrawQuickActions();
            GUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical("box");

            IEnumerable<ToolData> tools = GetAllTools();

            if (!string.IsNullOrEmpty(searchText))
            {
                tools = tools.Where(t =>
                    t.Title.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.Description.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.MenuPath.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (!string.IsNullOrEmpty(t.HelpURL) && t.HelpURL.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0));
            }
            else if (selectedCategory == ToolCategory.Favorites)
            {
                tools = tools.Where(t => favoriteTools.Contains(t.MenuPath));
            }
            else
            {
                tools = tools.Where(t => selectedCategory.ToString() == GetToolCategory(t.MenuPath));
            }

            DrawToolGroup(tools);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            GUILayout.Label("🧰 Meyz's Toolbag",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter });
            GUILayout.Label("Small tools. Big impact. - " + VERSION, EditorStyles.centeredGreyMiniLabel);
        }

        private void DrawToolbar()
        {
            selectedCategory = (ToolCategory)GUILayout.Toolbar(
                (int)selectedCategory,
                Enum.GetNames(typeof(ToolCategory)),
                GUILayout.Height(30));
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🧹 Cleanup Missing Scripts", GUILayout.Height(24)))
                EditorApplication.ExecuteMenuItem("MeyzToolbag/Utility/Missing Scripts Cleaner");

            if (GUILayout.Button("🎨 Quick Material Swapper", GUILayout.Height(24)))
                EditorApplication.ExecuteMenuItem("MeyzToolbag/Utility/Quick Material Swapper");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button("⚙ Settings", GUILayout.Width(80)))
                ShowSettingsPopup();

            GUILayout.FlexibleSpace();
            GUILayout.Label("by Meyz", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("meyz.dev", EditorStyles.linkLabel))
                Application.OpenURL("https://meyz.dev");

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolGroup(IEnumerable<ToolData> tools)
        {
            foreach (var tool in tools) DrawTool(tool);
        }

        private void DrawTool(ToolData tool)
        {
            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(tool.Icon, GUILayout.Width(40));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(tool.Title, EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(tool.HelpURL) &&
                GUILayout.Button("?", GUILayout.Width(20), GUILayout.Height(18)))
            {
                Application.OpenURL(tool.HelpURL);
            }

            bool isFav = favoriteTools.Contains(tool.MenuPath);
            if (GUILayout.Button(isFav ? "★" : "☆", GUILayout.Width(24)))
            {
                if (isFav) favoriteTools.Remove(tool.MenuPath);
                else favoriteTools.Add(tool.MenuPath);
                SaveFavorites();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(tool.Description, EditorStyles.wordWrappedMiniLabel);

            // Shortcut hint
            EditorGUILayout.LabelField($"Shortcut: {GetShortcut(tool)}", EditorStyles.miniLabel);

            using (new EditorGUI.DisabledScope(tool.AvailabilityCheck != null && !tool.AvailabilityCheck()))
            {
                if (GUILayout.Button("Open Tool", GUILayout.Width(100)))
                    OpenTool(tool);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private void HandleShortcuts(Event evt)
        {
            if (evt.type != EventType.KeyDown) return;

            foreach (var tool in GetAllTools())
            {
                var binding = GetShortcut(tool);
                if (IsEventMatchShortcut(evt, binding))
                {
                    if (tool.AvailabilityCheck == null || tool.AvailabilityCheck())
                        OpenTool(tool);
                    evt.Use();
                    break;
                }
            }
        }

        private bool IsEventMatchShortcut(Event evt, string binding)
        {
            if (string.IsNullOrEmpty(binding)) return false;

            var parts = binding.Split('+');
            bool wantCtrl  = parts.Contains("Ctrl", StringComparer.OrdinalIgnoreCase);
            bool wantAlt   = parts.Contains("Alt",  StringComparer.OrdinalIgnoreCase);
            bool wantShift = parts.Contains("Shift",StringComparer.OrdinalIgnoreCase);
            string keyPart = parts.Last();

            if (evt.control != wantCtrl || evt.alt != wantAlt || evt.shift != wantShift)
                return false;

            if (Enum.TryParse<KeyCode>(keyPart, true, out var code))
                return evt.keyCode == code;

            return false;
        }

        private void OpenTool(ToolData tool)
        {
            activeToolData = tool;
            if (tool.LaunchAction != null)
            {
                renderActiveTool = tool.LaunchAction;
            }
            else
            {
                EditorApplication.ExecuteMenuItem(tool.MenuPath);
            }
        }

        private List<ToolData> GetAllTools()
        {
            return new List<ToolData>
            {
                new ToolData {
                    Icon = "🎞", Title = "Force Pose Preview",
                    Description = "Preview and bake poses from AnimationClips.",
                    MenuPath = "MeyzToolbag/Animation/Force Pose Preview",
                    LaunchAction = () => ForcePoseTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/force-pose-preview",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.ForcePosePreview",
                    DefaultShortcut = "Ctrl+Shift+F"
                },
                new ToolData {
                    Icon = "🔤", Title = "Batch Rename",
                    Description = "Batch-rename selected GameObjects and assets in the scene.",
                    MenuPath = "MeyzToolbag/Utility/Batch Rename",
                    LaunchAction = () => BatchRenameTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/batch-rename",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.BatchRename",
                    DefaultShortcut = "Ctrl+Shift+B"
                },
                new ToolData {
                    Icon = "🔍", Title = "Asset Usage Finder",
                    Description = "Find where selected assets are referenced.",
                    MenuPath = "MeyzToolbag/Utility/Asset Usage Finder",
                    LaunchAction = () => AssetUsageFinderTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/asset-usage-finder",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.AssetUsageFinder",
                    DefaultShortcut = "Ctrl+Shift+A"
                },
                new ToolData {
                    Icon = "🗑", Title = "Missing Scripts Cleaner",
                    Description = "Scan scenes and prefabs to remove missing MonoBehaviour references.",
                    MenuPath = "MeyzToolbag/Utility/Missing Scripts Cleaner",
                    LaunchAction = () => MissingScriptsCleanerTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/missing-scripts-cleaner",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.MissingScriptsCleaner",
                    DefaultShortcut = "Ctrl+Shift+M"
                },
                new ToolData {
                    Icon = "🎨", Title = "Quick Material Swapper",
                    Description = "Save and apply material presets to selected objects.",
                    MenuPath = "MeyzToolbag/Utility/Quick Material Swapper",
                    LaunchAction = () => QuickMaterialSwapperTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/quick-material-swapper",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.QuickMaterialSwapper",
                    DefaultShortcut = "Ctrl+Shift+Q"
                },
                new ToolData {
                    Icon = "📐", Title = "Pivot Change",
                    Description = "Adjust and apply custom pivot positions in the Scene view.",
                    MenuPath = "MeyzToolbag/Transform/Pivot Editor",
                    LaunchAction = () => PivotChangeTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/pivot-change",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.PivotChange",
                    DefaultShortcut = "Ctrl+Shift+P"
                },
                new ToolData {
                    Icon = "📏", Title = "Linear Array",
                    Description = "Generate linear arrays of selected objects with count and offset settings.",
                    MenuPath = "MeyzToolbag/Scene Tools/Linear Duplication Tool",
                    LaunchAction = () => LinearDuplicationTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/linear-array",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.LinearArray",
                    DefaultShortcut = "Ctrl+Shift+L"
                },
                new ToolData {
                    Icon = "⭕", Title = "Circular Array",
                    Description = "Distribute objects around a center point in a full or partial circle.",
                    MenuPath = "MeyzToolbag/Scene Tools/Circular Duplication Tool",
                    LaunchAction = () => CircularDuplicationTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/circular-array",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.CircularArray",
                    DefaultShortcut = "Ctrl+Shift+C"
                },
                new ToolData {
                    Icon = "🔲", Title = "Grid Array",
                    Description = "Arrange objects in a grid formation with optional brick pattern.",
                    MenuPath = "MeyzToolbag/Scene Tools/Grid Duplication Tool",
                    LaunchAction = () => GridDuplicationTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/grid-array",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.GridArray",
                    DefaultShortcut = "Ctrl+Shift+G"
                },
                new ToolData {
                    Icon = "🛤️", Title = "Spline Array",
                    Description = "Place objects along a spline path with spacing, snapping, and scale curve.",
                    MenuPath = "MeyzToolbag/Scene Tools/Spline Duplication Tool",
                    LaunchAction = () => SplineDuplicationTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/spline-array",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.SplineArray",
                    DefaultShortcut = "Ctrl+Shift+S"
                },
                new ToolData {
                    Icon = "🌳", Title = "Random Area Spawn",
                    Description = "Scatter objects randomly within a radius with overlap avoidance and surface snapping.",
                    MenuPath = "MeyzToolbag/Scene Tools/Random Area Duplication Tool",
                    LaunchAction = () => RandomAreaDuplicationTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/random-area-spawn",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.RandomAreaSpawn",
                    DefaultShortcut = "Ctrl+Shift+R"
                },
                new ToolData {
                    Icon = "🔉", Title = "Audio Bank Organizer",
                    Description = "Scan & clean up your AudioClip library.",
                    MenuPath = "MeyzToolbag/Utility/Audio Bank Organizer",
                    LaunchAction = () => AudioBankOrganizerTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/audio-bank-organizer",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.AudioBankOrganizer",
                    DefaultShortcut = "Ctrl+Shift+U"
                },
                new ToolData {
                    Icon = "🗺", Title = "Simple Texture Atlas Packer",
                    Description = "Pack selected textures into an optimized atlas with trimming, metadata & material generation.",
                    MenuPath = "MeyzToolbag/Performance/Simple Texture Atlas Packer",
                    LaunchAction = () => TextureAtlasPackerTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/simple-texture-atlas-packer",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.SimpleTextureAtlasPacker",
                    DefaultShortcut = "Ctrl+Shift+T"
                },

                new ToolData {
                    Icon = "🧯", Title = "Null Safety Analyzer Tool",
                    Description = "Find null-prone fields across scenes & prefabs; assign & fix quickly.",
                    MenuPath = "MeyzToolbag/Performance/NullSafetyAnalyzerTool",
                    LaunchAction = () => NullSafetyAnalyzerTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/null-safety-analyzer",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.NullSafetyAnalyzer",
                    DefaultShortcut = "Ctrl+Shift+N"
                },
                new ToolData {
                    Icon = "🕰️",
                    Title = "Scene Time Machine",
                    Description = "Auto-backup scenes & roll back to any snapshot with a click.",
                    MenuPath = "MeyzToolbag/Scene Tools/Scene Time Machine",
                    LaunchAction = () => SceneTimeMachineTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/scene-time-machine",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.SceneTimeMachine",
                    DefaultShortcut = "Ctrl+Alt+B"
                },
                new ToolData {
                    Icon = "🎮",
                    Title = "Play Mode Saver Pro",
                    Description = "Auto-backup play mode changes with diff viewer and selective apply/revert system.",
                    MenuPath = "MeyzToolbag/Utility/Play Mode Saver Pro",
                    LaunchAction = () => PlayModeSaverTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/play-mode-saver-pro",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.PlayModeSaverPro",
                    DefaultShortcut = "Ctrl+Alt+S"
                },
                new ToolData {
                    Icon = "🔧", 
                    Title = "Enhanced Inspector Editor",
                    Description = "Smart inspector experience with search, grouping, quick actions and visual enhancements.",
                    MenuPath = "MeyzToolbag/Utility/Enhanced Inspector Editor",
                    LaunchAction = () => EnhancedInspectorTool.Draw(),
                    HelpURL = "https://meyz.dev/docs/enhanced-inspector-editor",
                    ShortcutPrefKey = "MeyzToolbag.Shortcut.EnhancedInspectorEditor",
                    DefaultShortcut = "Ctrl+Shift+E",
                },
            };
        }

        private string GetShortcut(ToolData tool)
            => EditorPrefs.GetString(tool.ShortcutPrefKey, tool.DefaultShortcut);

        private void SetShortcut(ToolData tool, string val)
            => EditorPrefs.SetString(tool.ShortcutPrefKey, val);

        private string GetToolCategory(string menuPath)
        {
            // Expect: "MeyzToolbag/<Category>/<Title>"
            if (string.IsNullOrEmpty(menuPath)) return "Other";
            var parts = menuPath.Split('/');
            return parts.Length > 1 ? parts[1].Replace(" ", string.Empty) : "Other";
        }

        private void ShowSettingsPopup()
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent("Settings (coming soon)"));
            menu.ShowAsContext();
        }

        /// <summary> Dışarıdan doğrudan tool açmak istersen </summary>
        public void OpenToolDirectly(string toolIdentifier)
        {
            var allTools = GetAllTools();
            if (!ToolIdentifierMap.TryGetValue(toolIdentifier, out var path)) return;

            var tool = allTools.FirstOrDefault(t => t.MenuPath == path);
            if (tool.LaunchAction != null)
            {
                activeToolData = tool;
                renderActiveTool = tool.LaunchAction;
                Debug.Log($"Opened {tool.Title} via direct call.");
                Repaint();
            }
            else
            {
                Debug.LogWarning($"Tool '{toolIdentifier}' has no LaunchAction!");
            }
        }
    }
}
#endif
