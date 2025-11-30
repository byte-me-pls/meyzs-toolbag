#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// BatchRenameTool - Main UI and mode-specific panels.
    /// </summary>
    public static partial class BatchRenameTool
    {
        // ---------------------------------------------------------------------
        // Entry
        // ---------------------------------------------------------------------
        public static void Draw()
        {
            // Header
            GUILayout.Space(10);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🏷️ Advanced Batch Rename", headerStyle);
            EditorGUILayout.LabelField(GUIContent.none, GUI.skin.horizontalSlider);
            GUILayout.Space(6);

            mainScroll = EditorGUILayout.BeginScrollView(mainScroll);

            // Quick Presets
            if (showPresetButtons && !renamePreviewing)
            {
                EditorGUILayout.LabelField("📋 Quick Presets:", EditorStyles.boldLabel);
                DrawTwoColumnButtons(
                    presets.Select(kv => (kv.Key, kv.Value)),
                    height: 22f
                );
                GUILayout.Space(6);
            }

            // Mode selector
            EditorGUILayout.LabelField("🔧 Rename Mode:", EditorStyles.boldLabel);
            mode = (RenameMode)EditorGUILayout.EnumPopup("Mode", mode);
            GUILayout.Space(4);

            // Mode-specific area
            DrawModeSpecificUI();
            GUILayout.Space(6);

            // Options
            EditorGUILayout.LabelField("⚙️ Options:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(GUI.skin.box);
            renameAssets    = EditorGUILayout.ToggleLeft("🗂️ Also rename selected assets", renameAssets);
            includeChildren = EditorGUILayout.ToggleLeft("👨‍👩‍👧‍👦 Include Children", includeChildren);
            sortSelection   = EditorGUILayout.ToggleLeft("🔢 Sort selection before renaming", sortSelection);
            EditorGUILayout.EndVertical();

            // Advanced options
            GUILayout.Space(4);
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "⚙️ Advanced Options");
            if (showAdvanced)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                preserveExtensions = EditorGUILayout.ToggleLeft("📄 Preserve file extensions", preserveExtensions);
                skipIfExists       = EditorGUILayout.ToggleLeft("⚠️ Skip if name already exists", skipIfExists);
                addTimestamp       = EditorGUILayout.ToggleLeft("🕐 Add timestamp", addTimestamp);
                customSeparator    = EditorGUILayout.TextField("🔗 Custom separator", customSeparator);

                filterByType = EditorGUILayout.ToggleLeft("🔍 Filter by component type", filterByType);
                if (filterByType)
                    typeFilter = EditorGUILayout.TextField("Component type", typeFilter);

                EditorGUILayout.EndVertical();
            }

            GUILayout.Space(8);

            // Selection & controls
            var (gos, assetPaths) = GetSelectionData();
            if (!renamePreviewing)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("📊 Selection Info:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  GameObjects: {gos.Count}");
                EditorGUILayout.LabelField($"  Assets: {assetPaths.Count}");
                EditorGUILayout.LabelField($"  Total: {gos.Count + assetPaths.Count}");
                EditorGUILayout.EndVertical();

                GUILayout.Space(4);

                using (new EditorGUI.DisabledScope(gos.Count == 0 && assetPaths.Count == 0))
                {
                    DrawTwoColumnButtons(new (string, Action)[]
                    {
                        ("🔍 Preview Rename", () => PreparePreview(gos, assetPaths)),
                        ("📊 Show Statistics", () => ShowStatistics(gos, assetPaths)),
                    }, height: 28f);
                }

                GUILayout.Space(4);
                DrawTwoColumnButtons(new (string, Action)[]
                {
                    ("💾 Save Settings", SaveSettings),
                    ("📁 Load Settings", LoadSettings),
                    ("🔄 Reset Tool",  ResetTool)
                }, height: 24f);
            }
            else
            {
                DrawPreviewUI();
            }

            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------------
        // Mode-specific UI
        // ---------------------------------------------------------------------
        private static void DrawModeSpecificUI()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);

            switch (mode)
            {
                case RenameMode.Prefix:
                    EditorGUILayout.LabelField("Add text before the current name.", MiniGray);
                    prefix = EditorGUILayout.TextField("Prefix", prefix);
                    if (!string.IsNullOrEmpty(prefix))
                        EditorGUILayout.LabelField($"Example: 'Object' → '{prefix}Object'", MiniGray);
                    break;

                case RenameMode.Suffix:
                    EditorGUILayout.LabelField("Add text after the current name.", MiniGray);
                    suffix = EditorGUILayout.TextField("Suffix", suffix);
                    if (!string.IsNullOrEmpty(suffix))
                        EditorGUILayout.LabelField($"Example: 'Object' → 'Object{suffix}'", MiniGray);
                    break;

                case RenameMode.FindReplace:
                    EditorGUILayout.LabelField("Replace specific text.", MiniGray);
                    findText    = EditorGUILayout.TextField("Find", findText);
                    replaceText = EditorGUILayout.TextField("Replace With", replaceText);
                    if (!string.IsNullOrEmpty(findText))
                        EditorGUILayout.LabelField($"Example: 'Old{findText}Name' → 'Old{replaceText}Name'", MiniGray);
                    break;

                case RenameMode.Numbering:
                    EditorGUILayout.LabelField("Add sequential numbers.", MiniGray);
                    startIndex      = EditorGUILayout.IntField("Start Index", startIndex);
                    increment       = EditorGUILayout.IntField("Increment", increment);
                    padding         = EditorGUILayout.IntField("Padding (0 = none)", padding);
                    numberPosition  = (NumberingPosition)EditorGUILayout.EnumPopup("Position", numberPosition);

                    string example = GetNumberingExample();
                    EditorGUILayout.LabelField($"Example: 'Object' → '{example}'", MiniGray);
                    break;

                case RenameMode.Template:
                    EditorGUILayout.LabelField("Use a template with variables.", MiniGray);
                    template = EditorGUILayout.TextField("Template", template);

                    EditorGUILayout.LabelField("Available variables:", MiniGray);
                    EditorGUILayout.LabelField("  {name}        = original name", MiniGray);
                    EditorGUILayout.LabelField("  {index} or {index:00} = number with padding", MiniGray);
                    EditorGUILayout.LabelField("  {timestamp}   = current timestamp (per batch)", MiniGray);
                    EditorGUILayout.LabelField("  {type}        = component type (GameObjects only)", MiniGray);
                    EditorGUILayout.LabelField("  {parent}      = parent name", MiniGray);

                    if (!string.IsNullOrEmpty(template))
                        EditorGUILayout.LabelField(
                            $"Example: 'Object' → '{ProcessTemplate(template, "Object", 1, "Transform", "Parent", SafePreviewTimestamp())}'",
                            MiniGray);
                    break;

                case RenameMode.Regex:
                    EditorGUILayout.LabelField("Advanced pattern replacement (Compiled).", MiniGray);
                    regexPattern = EditorGUILayout.TextField("Regex Pattern", regexPattern);
                    regexReplace = EditorGUILayout.TextField("Replace With", regexReplace);

                    DrawTwoColumnButtons(new (string, Action)[]
                    {
                        ("Remove Numbers",   () => { regexPattern = @"\\d+";    regexReplace = "";  }),
                        ("Extract Numbers",  () => { regexPattern = @"[^\\d]";  regexReplace = "";  }),
                        ("Clean Special",    () => { regexPattern = @"[^a-zA-Z0-9_]"; regexReplace = "_"; })
                    }, height: 20f);

                    EnsureCompiledRegex();
                    break;

                case RenameMode.CaseChange:
                    EditorGUILayout.LabelField("Change casing.", MiniGray);
                    caseMode = (CaseMode)EditorGUILayout.EnumPopup("Case Mode", caseMode);

                    string caseExample = ApplyCaseChange("hello world test", caseMode);
                    EditorGUILayout.LabelField($"Example: 'hello world test' → '{caseExample}'", MiniGray);
                    break;

                case RenameMode.RemoveCharacters:
                    EditorGUILayout.LabelField("Remove specific characters.", MiniGray);
                    charactersToRemove = EditorGUILayout.TextField("Characters to Remove", charactersToRemove);

                    if (!string.IsNullOrEmpty(charactersToRemove))
                    {
                        string cleaned = RemoveCharacters("Test-Name_123 (Copy)", charactersToRemove);
                        EditorGUILayout.LabelField($"Example: 'Test-Name_123 (Copy)' → '{cleaned}'", MiniGray);
                    }
                    break;

                case RenameMode.InsertAt:
                    EditorGUILayout.LabelField("Insert text at a specific position.", MiniGray);
                    insertText     = EditorGUILayout.TextField("Text to Insert", insertText);
                    insertPosition = EditorGUILayout.IntField("Position", insertPosition);

                    if (!string.IsNullOrEmpty(insertText))
                    {
                        string inserted = InsertTextAt("ObjectName", insertText, insertPosition);
                        EditorGUILayout.LabelField($"Example: 'ObjectName' → '{inserted}'", MiniGray);
                    }
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        // ---------------------------------------------------------------------
        // Two-column responsive buttons
        // ---------------------------------------------------------------------
        /// <summary>
        /// Draws buttons in a responsive 2-column layout (max 2 per row).
        /// </summary>
        private static void DrawTwoColumnButtons(IEnumerable<(string label, Action onClick)> items, float height = 24f, bool emphasizeFirst = false)
        {
            if (items == null) return;

            int col = 0;
            EditorGUILayout.BeginHorizontal();
            int i = 0;
            foreach (var (label, onClick) in items)
            {
                if (col == 2)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    col = 0;
                }

                if (emphasizeFirst && i == 0)
                {
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.6f, 0.95f, 0.6f);
                    if (GUILayout.Button(label, GUILayout.Height(height)))
                        onClick?.Invoke();
                    GUI.backgroundColor = old;
                }
                else
                {
                    if (GUILayout.Button(label, GUILayout.Height(height)))
                        onClick?.Invoke();
                }

                col++;
                i++;
            }

            if (col == 1) GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
