#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Core
{
    /// <summary>
    /// Global shortcut manager that provides MenuItem-based shortcuts for all tools
    /// These shortcuts work from anywhere in Unity Editor and integrate with the single MeyzToolbagWindow
    /// </summary>
    ///

    public static class GlobalShortcutsManager
    {
        // =============== MAIN TOOLBAG WINDOW ===============
        [MenuItem("Meyz's Toolbag/Open Toolbag %&m", false, 0)]
        private static void OpenToolbagShortcut()
        {
  
            MeyzToolbagWindow.OpenWindow();
        }

        // =============== ANIMATION TOOLS (Pattern: Ctrl+Alt+A+X) ===============
        [MenuItem("Meyz's Toolbag/Animation/Force Pose Preview %&a", false, 100)]
        private static void ForcePosePreviewShortcut()
        {
            OpenSpecificTool("ForcePosePreview");
        }

        [MenuItem("Meyz's Toolbag/Animation/Force Pose Preview %&a", true)]
        private static bool ValidateForcePosePreview()
        {
            return true; // Always available
        }

        // =============== UTILITY TOOLS (Pattern: Ctrl+Alt+U+X) ===============
        [MenuItem("Meyz's Toolbag/Utility/Batch Rename %&u", false, 200)]
        private static void BatchRenameShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Batch Rename: No objects selected!");
                return;
            }
            OpenSpecificTool("BatchRename");
        }

        [MenuItem("Meyz's Toolbag/Utility/Batch Rename %&u", true)]
        private static bool ValidateBatchRename()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Utility/Asset Usage Finder %&i", false, 201)]
        private static void AssetUsageFinderShortcut()
        {
            if (Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets).Length == 0)
            {
                Debug.LogWarning("Asset Usage Finder: No assets selected!");
                return;
            }
            OpenSpecificTool("AssetUsageFinder");
        }

        [MenuItem("Meyz's Toolbag/Utility/Asset Usage Finder %&i", true)]
        private static bool ValidateAssetUsageFinder()
        {
            return Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Utility/Missing Scripts Cleaner %&x", false, 202)]
        private static void MissingScriptsCleanerShortcut()
        {
            OpenSpecificTool("MissingScriptsCleaner");
        }

        [MenuItem("Meyz's Toolbag/Utility/Quick Material Swapper %&q", false, 203)]
        private static void QuickMaterialSwapperShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Quick Material Swapper: No objects selected!");
                return;
            }
            OpenSpecificTool("QuickMaterialSwapper");
        }

        [MenuItem("Meyz's Toolbag/Utility/Quick Material Swapper %&q", true)]
        private static bool ValidateQuickMaterialSwapper()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Utility/Audio Bank Organizer %&o", false, 204)]
        private static void AudioBankOrganizerShortcut()
        {
            OpenSpecificTool("AudioBankOrganizer");
        }

        // =============== TRANSFORM TOOLS (Pattern: Ctrl+Alt+T+X) ===============
        [MenuItem("Meyz's Toolbag/Transform/Pivot Editor %&p", false, 300)]
        private static void PivotEditorShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Pivot Editor: No objects selected!");
                return;
            }
            OpenSpecificTool("PivotChange");
        }

        [MenuItem("Meyz's Toolbag/Transform/Pivot Editor %&p", true)]
        private static bool ValidatePivotEditor()
        {
            return Selection.gameObjects.Length > 0;
        }

        // =============== SCENE TOOLS (Pattern: Ctrl+Alt+S+X) ===============
        [MenuItem("Meyz's Toolbag/Scene Tools/Linear Array %&l", false, 400)]
        private static void LinearArrayShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Linear Array: No objects selected!");
                return;
            }
            OpenSpecificTool("LinearArray");
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Linear Array %&l", true)]
        private static bool ValidateLinearArray()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Circular Array %&c", false, 401)]
        private static void CircularArrayShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Circular Array: No objects selected!");
                return;
            }
            OpenSpecificTool("CircularArray");
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Circular Array %&c", true)]
        private static bool ValidateCircularArray()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Grid Array %&g", false, 402)]
        private static void GridArrayShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Grid Array: No objects selected!");
                return;
            }
            OpenSpecificTool("GridArray");
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Grid Array %&g", true)]
        private static bool ValidateGridArray()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Spline Array %&s", false, 403)]
        private static void SplineArrayShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Spline Array: No objects selected!");
                return;
            }
            OpenSpecificTool("SplineArray");
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Spline Array %&s", true)]
        private static bool ValidateSplineArray()
        {
            return Selection.gameObjects.Length > 0;
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Random Area Spawn %&r", false, 404)]
        private static void RandomAreaSpawnShortcut()
        {
            if (Selection.gameObjects.Length == 0)
            {
                Debug.LogWarning("Random Area Spawn: No objects selected!");
                return;
            }
            OpenSpecificTool("RandomAreaSpawn");
        }

        [MenuItem("Meyz's Toolbag/Scene Tools/Random Area Spawn %&r", true)]
        private static bool ValidateRandomAreaSpawn()
        {
            return Selection.gameObjects.Length > 0;
        }

        // =============== PERFORMANCE TOOLS (Pattern: Ctrl+Shift+X) ===============
        [MenuItem("Meyz's Toolbag/Performance/Texture Atlas Packer %#t", false, 500)]
        private static void TextureAtlasPackerShortcut()
        {
            if (!Selection.objects.OfType<Texture2D>().Any())
            {
                Debug.LogWarning("Texture Atlas Packer: No Texture2D assets selected!");
                return;
            }
            OpenSpecificTool("SimpleTextureAtlasPacker");
        }

        [MenuItem("Meyz's Toolbag/Performance/Texture Atlas Packer %#t", true)]
        private static bool ValidateTextureAtlasPacker()
        {
            return Selection.objects.OfType<Texture2D>().Any();
        }

        [MenuItem("Meyz's Toolbag/Performance/PreBuild Size Estimator %#e", false, 501)]
        private static void PreBuildSizeEstimatorShortcut()
        {
            OpenSpecificTool("PreBuildSizeEstimator");
        }

        [MenuItem("Meyz's Toolbag/Performance/Null Safety Analyzer %#n", false, 502)]
        private static void NullSafetyAnalyzerShortcut()
        {
            OpenSpecificTool("NullSafetyAnalyzer");
        }

        // =============== QUICK ACTIONS (Pattern: Ctrl+Shift+F+X) ===============
        [MenuItem("Meyz's Toolbag/Quick Actions/Cleanup Missing Scripts %#z", false, 600)]
        private static void CleanupMissingScriptsShortcut()
        {
            // Direct execution without opening window
            if (EditorUtility.DisplayDialog("Cleanup Missing Scripts", 
                "This will scan all scenes and prefabs to remove missing script references. Continue?", 
                "Yes", "Cancel"))
            {
                // Execute menu item directly
                EditorApplication.ExecuteMenuItem("MeyzToolbag/Utility/Cleanup Missing Scripts");
            }
        }

        [MenuItem("Meyz's Toolbag/Quick Actions/Take Screenshot %#k", false, 601)]
        private static void TakeScreenshotShortcut()
        {
            // Execute screenshot menu item directly
            EditorApplication.ExecuteMenuItem("MeyzToolbag/Utility/Screenshot Tool");
        }

        // =============== CORE HELPER METHOD ===============
        
        /// <summary>
        /// Opens a specific tool in the main toolbag window using the single-window pattern
        /// This method needs to be implemented in MeyzToolbagWindow to support direct tool opening
        /// </summary>
        private static void OpenSpecificTool(string toolIdentifier)
        {
            // Get or create the toolbag window
            var window = EditorWindow.GetWindow<MeyzToolbagWindow>("Meyz's Toolbag");
            window.Show();
            window.Focus();
            
            // Call a method on the window to open the specific tool
            // This method needs to be added to MeyzToolbagWindow
            window.OpenToolDirectly(toolIdentifier);
        }

        // =============== SHORTCUT REFERENCE ===============
        [MenuItem("Meyz's Toolbag/📋 Shortcut Reference", false, 1000)]
        private static void ShowShortcutReference()
        {
            string shortcuts = @"🧰 MEYZ'S TOOLBAG - SMART 3-KEY SHORTCUTS

═══════════════════════════════════════════════

🏠 MAIN WINDOW
Ctrl+Alt+M          Open Toolbag

🎞 ANIMATION 
Ctrl+Alt+A          Force Pose Preview

🔧 UTILITY (Ctrl+Alt+Letter)
Ctrl+Alt+U          Batch Rename
Ctrl+Alt+I          Asset Usage Finder (Find Info)
Ctrl+Alt+X          Missing Scripts Cleaner
Ctrl+Alt+Q          Quick Material Swapper
Ctrl+Alt+O          Audio Bank Organizer

📐 TRANSFORM
Ctrl+Alt+P          Pivot Editor

🏗️ SCENE TOOLS (Ctrl+Alt+Letter)
Ctrl+Alt+L          Linear Array
Ctrl+Alt+C          Circular Array
Ctrl+Alt+G          Grid Array
Ctrl+Alt+S          Spline Array
Ctrl+Alt+R          Random Area Spawn

⚡ PERFORMANCE (Ctrl+Shift+Letter)
Ctrl+Shift+T        Texture Atlas Packer
Ctrl+Shift+E        PreBuild Size Estimator
Ctrl+Shift+N        Null Safety Analyzer

🚀 QUICK ACTIONS (Ctrl+Shift+Letter) 
Ctrl+Shift+Z        Cleanup Missing Scripts
Ctrl+Shift+K        Take Screenshot (Kamera)

═══════════════════════════════════════════════
Only 3 keys! Ctrl+Alt for most tools, Ctrl+Shift for performance/actions
";
            EditorUtility.DisplayDialog("Shortcut Reference", shortcuts, "OK");
        }
    }
}

#endif