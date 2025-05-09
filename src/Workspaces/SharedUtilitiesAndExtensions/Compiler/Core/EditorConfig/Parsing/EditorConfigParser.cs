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

            if (AnalyzerConfig.IsComment(line))
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
            if (AnalyzerConfig.ExtractKeyValue(line, activeSectionValues, out var key))
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
    }
}
