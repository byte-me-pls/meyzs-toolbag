#if UNITY_EDITOR
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MeyzsToolBag.Editor.Utility
{
    /// <summary>
    /// BatchRenameTool - Name computation, templates, casing, regex compile and validation helpers.
    /// </summary>
    public static partial class BatchRenameTool
    {
        // ---------------------------------------------------------------------
        // Name computation (central place)
        // ---------------------------------------------------------------------
        private static string ComputeNewName(string current, ref int idx, string componentType = "", string parentName = "", string timestamp = null)
        {
            string result = current ?? string.Empty;

            switch (mode)
            {
                case RenameMode.Prefix:
                    result = prefix + result;
                    break;

                case RenameMode.Suffix:
                    result = result + suffix;
                    break;

                case RenameMode.FindReplace:
                    if (!string.IsNullOrEmpty(findText))
                        result = result.Replace(findText, replaceText ?? string.Empty);
                    break;

                case RenameMode.Numbering:
                {
                    string num = padding > 0 ? idx.ToString().PadLeft(padding, '0') : idx.ToString();
                    result = numberPosition switch
                    {
                        NumberingPosition.Prefix => $"{num}{customSeparator}{result}",
                        NumberingPosition.Suffix => $"{result}{customSeparator}{num}",
                        NumberingPosition.Replace => num,
                        _ => $"{result}{customSeparator}{num}"
                    };
                    idx += increment;
                    break;
                }

                case RenameMode.Template:
                    result = ProcessTemplate(template, current, idx, componentType, parentName, timestamp ?? SafePreviewTimestamp());
                    idx += increment;
                    break;

                case RenameMode.Regex:
                    EnsureCompiledRegex();
                    if (compiledUserRegex != null)
                    {
                        try { result = compiledUserRegex.Replace(result, regexReplace ?? string.Empty); }
                        catch (Exception e)
                        {
                            Debug.LogError($"Regex replace error: {e.Message}");
                            result = current; // fallback
                        }
                    }
                    break;

                case RenameMode.CaseChange:
                    result = ApplyCaseChange(result, caseMode);
                    break;

                case RenameMode.RemoveCharacters:
                    result = RemoveCharacters(result, charactersToRemove);
                    break;

                case RenameMode.InsertAt:
                    result = InsertTextAt(result, insertText, insertPosition);
                    break;
            }

            // Optional timestamp per batch
            if (addTimestamp)
            {
                string ts = timestamp ?? SafePreviewTimestamp();
                if (!string.IsNullOrEmpty(ts))
                    result = $"{result}{customSeparator}{ts}";
            }

            return result;
        }

        private static string GetNumberingExample()
        {
            string num = padding > 0 ? startIndex.ToString().PadLeft(padding, '0') : startIndex.ToString();
            return numberPosition switch
            {
                NumberingPosition.Prefix  => $"{num}{customSeparator}Object",
                NumberingPosition.Suffix  => $"Object{customSeparator}{num}",
                NumberingPosition.Replace => num,
                _ => $"Object{customSeparator}{num}"
            };
        }

        private static string SafePreviewTimestamp()
        {
            if (!addTimestamp) return "";
            if (string.IsNullOrEmpty(previewTimestamp))
                previewTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return previewTimestamp;
        }

        // ---------------------------------------------------------------------
        // Template processor
        // ---------------------------------------------------------------------
        private static string ProcessTemplate(string tpl,
                                              string originalName,
                                              int index,
                                              string componentType,
                                              string parentName,
                                              string timestamp)
        {
            if (string.IsNullOrEmpty(tpl)) return originalName ?? "";

            string result = tpl;
            result = result.Replace("{name}", originalName ?? "");
            result = result.Replace("{type}", componentType ?? "");
            result = result.Replace("{parent}", parentName ?? "");
            result = result.Replace("{timestamp}", timestamp ?? "");

            // Handle {index} and {index:00}
            var matches = Regex.Matches(result, @"\{index(?::(\d+))?\}");
            foreach (Match m in matches)
            {
                string replacement;
                if (m.Groups[1].Success && int.TryParse(m.Groups[1].Value, out int pad))
                    replacement = index.ToString().PadLeft(pad, '0');
                else
                    replacement = index.ToString();

                result = result.Replace(m.Value, replacement);
            }

            return result;
        }

        // ---------------------------------------------------------------------
        // Case conversions
        // ---------------------------------------------------------------------
        private static string ApplyCaseChange(string input, CaseMode mode)
        {
            if (string.IsNullOrEmpty(input)) return input;

            return mode switch
            {
                CaseMode.Lowercase  => input.ToLowerInvariant(),
                CaseMode.Uppercase  => input.ToUpperInvariant(),
                CaseMode.TitleCase  => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower()),
                CaseMode.CamelCase  => ToCamelCase(input),
                CaseMode.PascalCase => ToPascalCase(input),
                CaseMode.SnakeCase  => ToSnakeCase(input),
                _ => input
            };
        }

        private static string ToCamelCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var words = Regex.Split(input, @"[\s\-_]+");
            if (words.Length == 0) return input;

            var result = words[0].ToLowerInvariant();
            for (int i = 1; i < words.Length; i++)
            {
                var w = words[i];
                if (w.Length > 0) result += char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
            }
            return result;
        }

        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var words = Regex.Split(input, @"[\s\-_]+");
            string result = "";
            foreach (var w in words)
            {
                if (w.Length > 0) result += char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
            }
            return result;
        }

        private static string ToSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            var result = Regex.Replace(input, @"[\s\-]+", "_");
            result = Regex.Replace(result, @"([a-z])([A-Z])", "$1_$2");
            return result.ToLowerInvariant();
        }

        // ---------------------------------------------------------------------
        // Simple transforms
        // ---------------------------------------------------------------------
        private static string RemoveCharacters(string input, string charsToRemove)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(charsToRemove)) return input;

            string result = input;
            for (int i = 0; i < charsToRemove.Length; i++)
            {
                string c = charsToRemove[i].ToString();
                if (!string.IsNullOrEmpty(c)) result = result.Replace(c, "");
            }
            return result;
        }

        private static string InsertTextAt(string input, string text, int position)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(text)) return input;
            position = Mathf.Clamp(position, 0, input.Length);
            return input.Insert(position, text);
        }

        // ---------------------------------------------------------------------
        // Regex compilation cache
        // ---------------------------------------------------------------------
        private static void EnsureCompiledRegex()
        {
            if (mode != RenameMode.Regex) return;
            var pat = regexPattern ?? string.Empty;

            if (compiledUserRegex == null || !string.Equals(compiledPatternCache, pat, StringComparison.Ordinal))
            {
                try
                {
                    compiledUserRegex   = string.IsNullOrEmpty(pat) ? null : new Regex(pat, kUserRegexOptions);
                    compiledPatternCache = pat;
                }
                catch (Exception e)
                {
                    compiledUserRegex    = null;
                    compiledPatternCache = null;
                    Debug.LogError($"Regex compile error: {e.Message}");
                }
            }
        }

        // ---------------------------------------------------------------------
        // Validation
        // ---------------------------------------------------------------------
        private static bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            for (int i = 0; i < invalidFileNameChars.Length; i++)
            {
                if (fileName.IndexOf(invalidFileNameChars[i]) >= 0)
                    return false;
            }
            return true;
        }

        private static bool IsValidGameObjectName(string name)
        {
            return !string.IsNullOrEmpty(name);
        }
    }
}
#endif
