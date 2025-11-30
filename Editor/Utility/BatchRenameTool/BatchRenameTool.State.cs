#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Centralized state and caches for BatchRenameTool.
    /// </summary>
    public static partial class BatchRenameTool
    {
        // ----- Core settings (backward-compatible defaults) -----
        private static RenameMode mode = RenameMode.Prefix;
        private static string prefix = "";
        private static string suffix = "";
        private static string findText = "";
        private static string replaceText = "";
        private static int startIndex = 1;
        private static int padding = 0;
        private static bool renameAssets = false;
        private static bool includeChildren = false;

        // ----- Advanced settings -----
        private static int increment = 1;
        private static NumberingPosition numberPosition = NumberingPosition.Suffix;
        private static string template = "{name}_{index:00}";
        private static string regexPattern = "";
        private static string regexReplace = "";
        private static CaseMode caseMode = CaseMode.TitleCase;
        private static string charactersToRemove = " -_()[]{}";
        private static string insertText = "";
        private static int insertPosition = 0;

        // ----- Advanced options -----
        private static bool showAdvanced = false;
        private static bool preserveExtensions = true;
        private static bool skipIfExists = true;
        private static bool addTimestamp = false;
        private static string customSeparator = "_";
        private static bool filterByType = false;
        private static string typeFilter = "";
        private static bool sortSelection = true;
        private static bool showPresetButtons = true;

        // ----- Preview state -----
        private static bool renamePreviewing = false;
        private static List<PreviewGameObject> previewGos = new List<PreviewGameObject>(256);
        private static List<PreviewAsset> previewAssets = new List<PreviewAsset>(256);
        private static Vector2 previewScroll;
        private static Vector2 mainScroll;

        // ----- UI helpers / styles -----
        private static GUIStyle _miniGray;
        private static GUIStyle MiniGray => _miniGray ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

        // ----- Performance caches -----
        private static readonly char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
        private static Regex compiledUserRegex;
        private static string compiledPatternCache;
        private const RegexOptions kUserRegexOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant;

        // Single timestamp used for one preview/apply batch
        private static string previewTimestamp = null;

        // ----- Presets -----
        private static readonly Dictionary<string, Action> presets = new Dictionary<string, Action>
        {
            ["Sequential Numbers"] = () =>
            {
                mode = RenameMode.Numbering;
                startIndex = 1;
                padding = 2;
                numberPosition = NumberingPosition.Suffix;
            },
            ["Remove Spaces"] = () =>
            {
                mode = RenameMode.FindReplace;
                findText = " ";
                replaceText = "";
            },
            ["Add Prefix"] = () =>
            {
                mode = RenameMode.Prefix;
                prefix = "New_";
            },
            ["Snake Case"] = () =>
            {
                mode = RenameMode.CaseChange;
                caseMode = CaseMode.SnakeCase;
            },
            ["Title Case"] = () =>
            {
                mode = RenameMode.CaseChange;
                caseMode = CaseMode.TitleCase;
            },
            ["Template Basic"] = () =>
            {
                mode = RenameMode.Template;
                template = "{name}_{index:00}";
            },
            ["Clean Names"] = () =>
            {
                mode = RenameMode.RemoveCharacters;
                charactersToRemove = " -_()[]{}0123456789";
            }
        };
    }
}
#endif
