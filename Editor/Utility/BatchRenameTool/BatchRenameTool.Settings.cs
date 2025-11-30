#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// BatchRenameTool - Settings persistence, quick presets and selection statistics.
    /// </summary>
    public static partial class BatchRenameTool
    {
        // ---------------------------------------------------------------------
        // Reset to defaults
        // ---------------------------------------------------------------------
        public static void ResetTool()
        {
            mode              = RenameMode.Prefix;
            prefix            = "";
            suffix            = "";
            findText          = "";
            replaceText       = "";
            startIndex        = 1;
            padding           = 0;
            renameAssets      = false;
            includeChildren   = false;

            increment         = 1;
            numberPosition    = NumberingPosition.Suffix;
            template          = "{name}_{index:00}";
            regexPattern      = "";
            regexReplace      = "";
            caseMode          = CaseMode.TitleCase;
            charactersToRemove= " -_()[]{}";
            insertText        = "";
            insertPosition    = 0;

            showAdvanced      = false;
            preserveExtensions= true;
            skipIfExists      = true;
            addTimestamp      = false;
            customSeparator   = "_";
            filterByType      = false;
            typeFilter        = "";
            sortSelection     = true;
            showPresetButtons = true;

            compiledUserRegex    = null;
            compiledPatternCache = null;
            previewTimestamp     = null;

            CancelPreview();
            Debug.Log("Advanced Batch Rename Tool reset to defaults");
        }

        // ---------------------------------------------------------------------
        // Settings serialization
        // ---------------------------------------------------------------------
        public static void SaveSettings()
        {
            var settings = new RenameSettings
            {
                mode            = mode,
                prefix          = prefix,
                suffix          = suffix,
                findText        = findText,
                replaceText     = replaceText,
                startIndex      = startIndex,
                padding         = padding,
                renameAssets    = renameAssets,
                includeChildren = includeChildren,
                increment       = increment,
                numberPosition  = numberPosition,
                template        = template,
                regexPattern    = regexPattern,
                regexReplace    = regexReplace,
                caseMode        = caseMode,
                charactersToRemove = charactersToRemove,
                insertText      = insertText,
                insertPosition  = insertPosition,
                preserveExtensions = preserveExtensions,
                skipIfExists    = skipIfExists,
                addTimestamp    = addTimestamp,
                customSeparator = customSeparator,
                filterByType    = filterByType,
                typeFilter      = typeFilter,
                sortSelection   = sortSelection
            };

            string json = JsonUtility.ToJson(settings, true);
            string path = EditorUtility.SaveFilePanel("Save Rename Settings", Application.dataPath, "RenameSettings", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                Debug.Log($"Rename settings saved to: {path}");
            }
        }

        public static void LoadSettings()
        {
            string path = EditorUtility.OpenFilePanel("Load Rename Settings", Application.dataPath, "json");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var s = JsonUtility.FromJson<RenameSettings>(json);

                    mode              = s.mode;
                    prefix            = s.prefix;
                    suffix            = s.suffix;
                    findText          = s.findText;
                    replaceText       = s.replaceText;
                    startIndex        = s.startIndex;
                    padding           = s.padding;
                    renameAssets      = s.renameAssets;
                    includeChildren   = s.includeChildren;
                    increment         = s.increment;
                    numberPosition    = s.numberPosition;
                    template          = s.template;
                    regexPattern      = s.regexPattern;
                    regexReplace      = s.regexReplace;
                    caseMode          = s.caseMode;
                    charactersToRemove= s.charactersToRemove;
                    insertText        = s.insertText;
                    insertPosition    = s.insertPosition;
                    preserveExtensions= s.preserveExtensions;
                    skipIfExists      = s.skipIfExists;
                    addTimestamp      = s.addTimestamp;
                    customSeparator   = s.customSeparator;
                    filterByType      = s.filterByType;
                    typeFilter        = s.typeFilter;
                    sortSelection     = s.sortSelection;

                    compiledUserRegex    = null;
                    compiledPatternCache = null;

                    Debug.Log($"Rename settings loaded from: {path}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load rename settings: {e.Message}");
                }
            }
        }

        // ---------------------------------------------------------------------
        // Quick helpers (menu-friendly presets)
        // ---------------------------------------------------------------------
        public static void QuickCleanNames()
        {
            mode = RenameMode.RemoveCharacters;
            charactersToRemove = " -()[]{}";
            Debug.Log("Quick Clean: configured to remove common special characters.");
        }

        public static void QuickSequentialNumber()
        {
            mode = RenameMode.Numbering;
            startIndex = 1;
            padding = 2;
            numberPosition = NumberingPosition.Suffix;
            Debug.Log("Quick Sequential: numbering with 2-digit padding.");
        }

        public static void QuickTemplateStandard()
        {
            mode = RenameMode.Template;
            template = "{name}_{index:00}";
            startIndex = 1;
            Debug.Log("Quick Template: standard template with numbering.");
        }

        // ---------------------------------------------------------------------
        // Statistics for current selection
        // ---------------------------------------------------------------------
        public static void ShowStatistics(List<GameObject> gameObjects, List<string> assetPaths)
        {
            int totalObjects = gameObjects.Count + assetPaths.Count;
            if (totalObjects == 0)
            {
                Debug.Log("No objects selected for statistics.");
                return;
            }

            var nameLengths = gameObjects.Select(go => go.name.Length)
                .Concat(assetPaths.Select(p => Path.GetFileNameWithoutExtension(p).Length))
                .ToList();

            float avgLength = (float)nameLengths.Average();
            int minLength   = nameLengths.Min();
            int maxLength   = nameLengths.Max();

            var allNames = gameObjects.Select(go => go.name)
                                      .Concat(assetPaths.Select(p => Path.GetFileNameWithoutExtension(p)))
                                      .ToList();

            var duplicateNames = allNames.GroupBy(n => n)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => new { Name = g.Key, Count = g.Count() })
                                         .ToList();

            int namesWithNumbers     = allNames.Count(n => Regex.IsMatch(n, @"\d"));
            int namesWithSpecial     = allNames.Count(n => Regex.IsMatch(n, @"[^a-zA-Z0-9_]"));
            int shortNames           = allNames.Count(n => n.Length <= 2);

            string stats = $"📊 SELECTION STATISTICS 📊\n" +
                           $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                           $"Total Objects: {totalObjects}\n" +
                           $"├─ GameObjects: {gameObjects.Count}\n" +
                           $"└─ Assets: {assetPaths.Count}\n\n" +
                           $"NAME ANALYSIS:\n" +
                           $"├─ Average Length: {avgLength:F1} chars\n" +
                           $"├─ Shortest Name: {minLength} chars\n" +
                           $"├─ Longest Name: {maxLength} chars\n" +
                           $"├─ Names with Numbers: {namesWithNumbers}\n" +
                           $"├─ Names with Special Chars: {namesWithSpecial}\n" +
                           $"├─ Short Names (≤2 chars): {shortNames}\n" +
                           $"└─ Duplicate Names: {duplicateNames.Count}\n";

            if (duplicateNames.Count > 0)
            {
                stats += "\nDUPLICATE NAMES:\n";
                foreach (var dup in duplicateNames.Take(10))
                    stats += $"├─ '{dup.Name}' ({dup.Count} times)\n";
                if (duplicateNames.Count > 10)
                    stats += $"└─ ... and {duplicateNames.Count - 10} more\n";
            }

            Debug.Log(stats);

            if (duplicateNames.Count > 0)
                Debug.LogWarning($"Found {duplicateNames.Count} duplicate names. Consider Numbering mode to make them unique.");

            if (namesWithSpecial > totalObjects / 2)
                Debug.LogWarning("Many names contain special characters. Consider 'Remove Characters' mode for cleaner names.");

            if (shortNames > 0)
                Debug.LogWarning($"Found {shortNames} very short names. Consider Template mode for more descriptive names.");
        }
    }
}
#endif
