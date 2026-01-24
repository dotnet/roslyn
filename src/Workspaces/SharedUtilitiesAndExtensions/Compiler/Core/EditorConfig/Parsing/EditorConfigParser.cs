// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing;

internal static class EditorConfigParser
{
    // Matches EditorConfig section header such as "[*.{js,py}]", see https://editorconfig.org for details
    private static readonly Regex s_sectionMatcher = new(@"^\s*\[(([^#;]|\\#|\\;)+)\]\s*([#;].*)?$", RegexOptions.Compiled);

    private static ImmutableHashSet<string> ReservedKeys { get; }
        = ImmutableHashSet.CreateRange(AnalyzerConfigOptions.KeyComparer, [
            "root",
            "indent_style",
            "indent_size",
            "tab_width",
            "end_of_line",
            "charset",
            "trim_trailing_whitespace",
            "insert_final_newline",
        ]);

    private static ImmutableHashSet<string> ReservedValues { get; }
        = ImmutableHashSet.CreateRange(CaseInsensitiveComparison.Comparer, ["unset"]);

    public static TEditorConfigFile Parse<TEditorConfigFile, TResult, TAccumulator>(string text, string? pathToFile, TAccumulator accumulator)
        where TAccumulator : IEditorConfigOptionAccumulator<TEditorConfigFile, TResult>
        where TEditorConfigFile : EditorConfigFile<TResult>
        where TResult : EditorConfigOption
    {
        return Parse<TEditorConfigFile, TResult, TAccumulator>(SourceText.From(text), pathToFile, accumulator);
    }

    public static TEditorConfigFile Parse<TEditorConfigFile, TEditorConfigOption, TAccumulator>(SourceText text, string? pathToFile, TAccumulator accumulator)
        where TAccumulator : IEditorConfigOptionAccumulator<TEditorConfigFile, TEditorConfigOption>
        where TEditorConfigFile : EditorConfigFile<TEditorConfigOption>
        where TEditorConfigOption : EditorConfigOption
    {
        var activeSectionValues = ImmutableDictionary.CreateBuilder<string, string>(AnalyzerConfigOptions.KeyComparer);
        var activeSectionLines = ImmutableDictionary.CreateBuilder<string, TextLine>(AnalyzerConfigOptions.KeyComparer);
        var activeSectionName = "";
        var activeSectionStart = 0;
        var activeSectionEnd = 0;
        var activeLine = default(TextLine);
        foreach (var textLine in text.Lines)
        {
            activeLine = textLine;
            var line = textLine.ToString();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (IsComment(line))
            {
                continue;
            }

            // Section matching
            var sectionMatches = s_sectionMatcher.Matches(line);
            if (sectionMatches is [{ Groups.Count: > 0 }, ..])
            {
                ProcessActiveSection();
                var sectionName = sectionMatches[0].Groups[1].Value;
                Debug.Assert(!string.IsNullOrEmpty(sectionName));

                activeSectionStart = textLine.Start;
                activeSectionName = sectionName;
                activeSectionEnd = textLine.End;
                activeSectionValues.Clear();
                activeSectionLines.Clear();
                continue;
            }

            // property matching
            if (ExtractKeyValue(line, activeSectionValues, out var key))
            {
                activeSectionLines[key] = textLine;
                activeSectionEnd = textLine.End;

                continue;
            }
        }

        // add remaining property to the final section
        ProcessActiveSection();

        return accumulator.Complete(pathToFile);

        void ProcessActiveSection()
        {
            var isGlobal = activeSectionName == "";
            var fullText = activeLine.ToString();
            var sectionSpan = new TextSpan(activeSectionStart, activeSectionEnd);
            var previousSection = new Section(pathToFile, isGlobal, sectionSpan, activeSectionName, fullText);
            accumulator.ProcessSection(previousSection, activeSectionValues, activeSectionLines);
        }

        // Check if the line is a comment. A line is considered a comment if its first non-space character is either # or ;
        static bool IsComment(string line)
        {
            foreach (var c in line)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return c == '#' || c == ';';
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Extracts a key-value pair from the given line. The line content will be trimmed
    /// </summary>
    /// <param name="line">Line to extract key/value from as an expected form of key{:|=}[value[{#|;}inline comment]]</param>
    /// <param name="activeSectionProperties">Property collection builder</param>
    /// <param name="key">Optional key found; default is "" because nullability is flagged by return</param>
    /// <returns>Actual key found flag</returns>
    private static bool ExtractKeyValue(string line, ImmutableDictionary<string, string>.Builder activeSectionProperties, out string key)
    {
        // Look for a key-value pair
        var trimmedLine = line.TrimStart(); // remove leading whitespace for the key part
        string keyPart;

        // Look for a non-empty key part
        var keyStart = trimmedLine.IndexOfAny(['=', ':']);
        if (keyStart <= 0 || (keyPart = trimmedLine[..keyStart].TrimEnd()).Length == 0) // remove trailing whitespace for the key part, and ensure keyPart has a non-trimmable content (it can't have a content if keyStart is below second character)
        {
            key = "";
            return false;
        }

        key = keyPart.ToLower(); // lower casing the key part
        var valueComment = trimmedLine[(keyStart + 1)..].TrimStart(); // remove leading whitespace for the value part
        string valuePart;

        // Look for an inline comment in the value part
        var commentStart = valueComment.IndexOfAny(['#', ';']);
        if (commentStart > -1)
        {
            // Remove inline comment from the value part
            valuePart = valueComment[..commentStart];
        }
        else
        {
            valuePart = valueComment;
        }

        string value;
        if (ReservedKeys.Contains(key) || ReservedValues.Contains(valuePart))
        {
            // Lower case the value part if the key is reserved
            value = valuePart.ToLower();
        }
        else
        {
            value = valuePart;
        }

        // Add the key-value pair to the dictionary
        activeSectionProperties[key] = value.TrimEnd(); // remove trailing whitespace for the value part, allowing for "" values
        return true;
    }
}
