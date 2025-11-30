#if UNITY_EDITOR
using System;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// Types and small preview models for BatchRenameTool.
    /// </summary>
    public static partial class BatchRenameTool
    {
        public enum RenameMode
        {
            Prefix,
            Suffix,
            FindReplace,
            Numbering,
            Template,
            Regex,
            CaseChange,
            RemoveCharacters,
            InsertAt
        }

        public enum CaseMode
        {
            Lowercase,
            Uppercase,
            TitleCase,
            CamelCase,
            PascalCase,
            SnakeCase
        }

        public enum NumberingPosition
        {
            Prefix,
            Suffix,
            Replace
        }

        // Preview models
        private class PreviewGameObject
        {
            public GameObject go;
            public string oldName;
            public string newName;
            public bool hasError;
            public string errorMessage;
        }

        private class PreviewAsset
        {
            public string path;
            public string oldName;
            public string newName;
            public bool hasError;
            public string errorMessage;
        }

        [Serializable]
        public class RenameSettings
        {
            public RenameMode mode;
            public string prefix;
            public string suffix;
            public string findText;
            public string replaceText;
            public int startIndex;
            public int padding;
            public bool renameAssets;
            public bool includeChildren;
            public int increment;
            public NumberingPosition numberPosition;
            public string template;
            public string regexPattern;
            public string regexReplace;
            public CaseMode caseMode;
            public string charactersToRemove;
            public string insertText;
            public int insertPosition;
            public bool preserveExtensions;
            public bool skipIfExists;
            public bool addTimestamp;
            public string customSeparator;
            public bool filterByType;
            public string typeFilter;
            public bool sortSelection;
        }
    }
}
#endif
