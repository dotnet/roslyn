// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.EditorConfig;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater;

internal static partial class SettingsUpdateHelper
{
    private const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
    private const string SeveritySuffix = ".severity";

    public static SourceText? TryUpdateAnalyzerConfigDocument(SourceText originalText,
                                                              string filePath,
                                                              IReadOnlyList<(AnalyzerSetting option, ReportDiagnostic value)> settingsToUpdate)
    {
        if (originalText is null)
            return null;
        if (settingsToUpdate is null)
            return null;
        if (filePath is null)
            return null;

        return TryUpdateAnalyzerConfigDocument(originalText, filePath, settingsToUpdate.Select(x => GetOptionValueAndLanguage(x.option, x.value)));

        static (string option, string value, Language language) GetOptionValueAndLanguage(AnalyzerSetting diagnostic, ReportDiagnostic severity)
        {
            var optionName = $"{DiagnosticOptionPrefix}{diagnostic.Id}{SeveritySuffix}";
            var optionValue = severity.ToEditorConfigString();
            var language = diagnostic.Language;
            return (optionName, optionValue, language);
        }
    }

    public static SourceText? TryUpdateAnalyzerConfigDocument(
        SourceText originalText,
        string filePath,
        IReadOnlyList<(IOption2 option, object value)> settingsToUpdate)
    {
        if (originalText is null)
            return null;
        if (settingsToUpdate is null)
            return null;
        if (filePath is null)
            return null;

        return TryUpdateAnalyzerConfigDocument(originalText, filePath, settingsToUpdate.Select(x => GetOptionValueAndLanguage(x.option, x.value)));

        static (string option, string value, Language language) GetOptionValueAndLanguage(IOption2 option, object value)
        {
            var optionName = option.Definition.ConfigName;
            var optionValue = option.Definition.Serializer.Serialize(value);

            if (value is ICodeStyleOption2 codeStyleOption && !optionValue.Contains(':'))
            {
                var severity = codeStyleOption.Notification.Severity switch
                {
                    ReportDiagnostic.Hidden => "silent",
                    ReportDiagnostic.Info => "suggestion",
                    ReportDiagnostic.Warn => "warning",
                    ReportDiagnostic.Error => "error",
                    _ => string.Empty
                };
                optionValue = $"{optionValue}:{severity}";
            }

            Language language;
            if (option is ISingleValuedOption singleValuedOption)
            {
                language = singleValuedOption.LanguageName switch
                {
                    LanguageNames.CSharp => Language.CSharp,
                    LanguageNames.VisualBasic => Language.VisualBasic,
                    null => Language.CSharp | Language.VisualBasic,
                    _ => throw ExceptionUtilities.UnexpectedValue(singleValuedOption.LanguageName),
                };
            }
            else if (option.IsPerLanguage)
            {
                language = Language.CSharp | Language.VisualBasic;
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(option);
            }

            return (optionName, optionValue, language);
        }
    }

    public static SourceText? TryUpdateAnalyzerConfigDocument(SourceText originalText,
                                                              string filePath,
                                                              IEnumerable<(string option, string value, Language language)> settingsToUpdate)
    {
        var updatedText = originalText;
        TextLine? lastValidHeaderSpanEnd;
        TextLine? lastValidSpecificHeaderSpanEnd;
        foreach (var (option, value, language) in settingsToUpdate)
        {
            SourceText? newText;
            (newText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd) = UpdateIfExistsInFile(updatedText, filePath, option, value, language);
            if (newText != null)
            {
                updatedText = newText;
                continue;
            }

            (newText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd) = AddMissingRule(updatedText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd, option, value, language);
            if (newText != null)
            {
                updatedText = newText;
            }
        }

        return updatedText.Equals(originalText) ? null : updatedText;
    }

    /// <summary>
    /// <para>Regular expression for .editorconfig header.</para>
    /// <para>For example: "[*.cs]    # Optional comment"</para>
    /// <para>             "[*.{vb,cs}]"</para>
    /// <para>             "[*]    ; Optional comment"</para>
    /// <para>             "[ConsoleApp/Program.cs]"</para>
    /// </summary>
    private static readonly Regex s_headerPattern = new(@"\[(\*|[^ #;\[\]]+\.({[^ #;{}\.\[\]]+}|[^ #;{}\.\[\]]+))\]\s*([#;].*)?");

    /// <summary>
    /// <para>Regular expression for .editorconfig code style option entry.</para>
    /// <para>For example:</para>
    /// <para> 1. "dotnet_style_object_initializer = true   # Optional comment"</para>
    /// <para> 2. "dotnet_style_object_initializer = true:suggestion   ; Optional comment"</para>
    /// <para> 3. "dotnet_diagnostic.CA2000.severity = suggestion   # Optional comment"</para>
    /// <para> 4. "dotnet_analyzer_diagnostic.category-Security.severity = suggestion   # Optional comment"</para>
    /// <para> 5. "dotnet_analyzer_diagnostic.severity = suggestion   # Optional comment"</para>
    /// <para>Regex groups:</para>
    /// <para> 1. Option key</para>
    /// <para> 2. Option value</para>
    /// <para> 3. Optional severity suffix in option value, i.e. ':severity' suffix</para>
    /// <para>4. Optional comment suffix</para>
    /// </summary>
    private static readonly Regex s_optionEntryPattern = new($@"(.*)=([\w, ]*)(:[\w]+)?([ ]*[;#].*)?");

    private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) UpdateIfExistsInFile(SourceText editorConfigText,
                                                                                                                                          string filePath,
                                                                                                                                          string optionName,
                                                                                                                                          string optionValue,
                                                                                                                                          Language language)
    {
        var editorConfigDirectory = PathUtilities.GetDirectoryName(filePath);
        Assumes.NotNull(editorConfigDirectory);
        var relativePath = PathUtilities.GetRelativePath(editorConfigDirectory.ToLowerInvariant(), filePath);

        TextLine? mostRecentHeader = null;
        TextLine? lastValidHeader = null;
        TextLine? lastValidHeaderSpanEnd = null;

        TextLine? lastValidSpecificHeader = null;
        TextLine? lastValidSpecificHeaderSpanEnd = null;

        var textChange = new TextChange();
        foreach (var curLine in editorConfigText.Lines)
        {
            var curLineText = curLine.ToString();
            if (s_optionEntryPattern.IsMatch(curLineText))
            {
                var groups = s_optionEntryPattern.Match(curLineText).Groups;
                var (untrimmedKey, key, value, severity, comment) = GetGroups(groups);

                // Verify the most recent header is a valid header
                if (IsValidHeader(mostRecentHeader, lastValidHeader) &&
                    string.Equals(key, optionName, StringComparison.OrdinalIgnoreCase))
                {
                    // We found the rule in the file -- replace it with updated option value.
                    textChange = new TextChange(curLine.Span, $"{untrimmedKey}= {optionValue}{comment}");
                }
            }
            else if (s_headerPattern.IsMatch(curLineText.Trim()))
            {
                mostRecentHeader = curLine;
                if (ShouldSetAsLastValidHeader(curLineText, out var mostRecentHeaderText))
                {
                    lastValidHeader = mostRecentHeader;
                }
                else
                {
                    var (fileName, splicedFileExtensions) = ParseHeaderParts(mostRecentHeaderText);
                    if ((relativePath.IsEmpty() || new Regex(fileName).IsMatch(relativePath)) &&
                        HeaderMatchesLanguageRequirements(language, splicedFileExtensions))
                    {
                        lastValidHeader = mostRecentHeader;
                    }
                }
            }

            // We want to keep track of how far this (valid) section spans.
            if (IsValidHeader(mostRecentHeader, lastValidHeader) && IsNotEmptyOrComment(curLineText))
            {
                lastValidHeaderSpanEnd = curLine;
                if (lastValidSpecificHeader != null && mostRecentHeader.Equals(lastValidSpecificHeader))
                {
                    lastValidSpecificHeaderSpanEnd = curLine;
                }
            }
        }

        // We return only the last text change in case of duplicate entries for the same rule.
        if (textChange != default)
        {
            return (editorConfigText.WithChanges(textChange), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }

        // Rule not found.
        return (null, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);

        static (string untrimmedKey, string key, string value, string severitySuffixInValue, string commentValue) GetGroups(GroupCollection groups)
        {
            var untrimmedKey = groups[1].Value.ToString();
            var key = untrimmedKey.Trim();
            var value = groups[2].Value.ToString();
            var severitySuffixInValue = groups[3].Value.ToString();
            var commentValue = groups[4].Value.ToString();
            return (untrimmedKey, key, value, severitySuffixInValue, commentValue);
        }

        static bool IsValidHeader(TextLine? mostRecentHeader, TextLine? lastValidHeader)
        {
            return mostRecentHeader is not null &&
                   lastValidHeader is not null &&
                   mostRecentHeader.Equals(lastValidHeader);
        }

        static bool ShouldSetAsLastValidHeader(string curLineText, out string mostRecentHeaderText)
        {
            var groups = s_headerPattern.Match(curLineText.Trim()).Groups;
            mostRecentHeaderText = groups[1].Value.ToString().ToLowerInvariant();
            return mostRecentHeaderText.Equals("*", StringComparison.Ordinal);
        }

        static (string fileName, string[] splicedFileExtensions) ParseHeaderParts(string mostRecentHeaderText)
        {
            // We splice on the last occurrence of '.' to account for filenames containing periods.
            var nameExtensionSplitIndex = mostRecentHeaderText.LastIndexOf('.');
            var fileName = mostRecentHeaderText[..nameExtensionSplitIndex];
            var splicedFileExtensions = mostRecentHeaderText[(nameExtensionSplitIndex + 1)..].Split(',', ' ', '{', '}');

            // Replacing characters in the header with the regex equivalent.
            fileName = fileName.Replace(".", @"\.");
            fileName = fileName.Replace("*", ".*");
            fileName = fileName.Replace("/", @"\/");

            return (fileName, splicedFileExtensions);
        }

        static bool IsNotEmptyOrComment(string currentLineText)
        {
            return !string.IsNullOrWhiteSpace(currentLineText) && !currentLineText.Trim().StartsWith("#", StringComparison.OrdinalIgnoreCase);
        }

        static bool HeaderMatchesLanguageRequirements(Language language, string[] splicedFileExtensions)
        {
            return IsCSharpOnly(language, splicedFileExtensions) || IsVisualBasicOnly(language, splicedFileExtensions) || IsBothVisualBasicAndCSharp(language, splicedFileExtensions);
        }

        static bool IsCSharpOnly(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.CSharp) && !language.HasFlag(Language.VisualBasic) && splicedFileExtensions.Contains("cs") && splicedFileExtensions.Length == 1;
        }

        static bool IsVisualBasicOnly(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.VisualBasic) && !language.HasFlag(Language.CSharp) && splicedFileExtensions.Contains("vb") && splicedFileExtensions.Length == 1;
        }

        static bool IsBothVisualBasicAndCSharp(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.VisualBasic) && language.HasFlag(Language.CSharp) && splicedFileExtensions.Contains("vb") && splicedFileExtensions.Contains("cs");
        }
    }

    private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) AddMissingRule(SourceText editorConfigText,
                                                                                                                                    TextLine? lastValidHeaderSpanEnd,
                                                                                                                                    TextLine? lastValidSpecificHeaderSpanEnd,
                                                                                                                                    string optionName,
                                                                                                                                    string optionValue,
                                                                                                                                    Language language)
    {
        var newEntry = $"{optionName} = {optionValue}";
        if (lastValidSpecificHeaderSpanEnd.HasValue)
        {
            if (lastValidSpecificHeaderSpanEnd.Value.ToString().Trim().Length != 0)
            {
                newEntry = "\r\n" + newEntry; // TODO(jmarolf): do we need to read in the users newline settings?
            }

            return (editorConfigText.WithChanges(new TextChange(new TextSpan(lastValidSpecificHeaderSpanEnd.Value.Span.End, 0), newEntry)), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }
        else if (lastValidHeaderSpanEnd.HasValue)
        {
            if (lastValidHeaderSpanEnd.Value.ToString().Trim().Length != 0)
            {
                newEntry = "\r\n" + newEntry; // TODO(jmarolf): do we need to read in the users newline settings?
            }

            return (editorConfigText.WithChanges(new TextChange(new TextSpan(lastValidHeaderSpanEnd.Value.Span.End, 0), newEntry)), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }

        // We need to generate a new header such as '[*.cs]' or '[*.vb]':
        //      - For compiler diagnostic entries and code style entries which have per-language option = false, generate only [*.cs] or [*.vb].
        //      - For the remainder, generate [*.{cs,vb}]
        // Insert a newline if not already present
        var lines = editorConfigText.Lines;
        var lastLine = lines.Count > 0 ? lines[^1] : default;
        var prefix = string.Empty;
        if (lastLine.ToString().Trim().Length != 0)
        {
            prefix = "\r\n";
        }

        // Insert newline if file is not empty
        if (lines.Count > 1 && lastLine.ToString().Trim().Length == 0)
        {
            prefix += "\r\n";
        }

        if (language.HasFlag(Language.CSharp) && language.HasFlag(Language.VisualBasic))
        {
            prefix += "[*.{cs,vb}]\r\n";
        }
        else if (language.HasFlag(Language.CSharp))
        {
            prefix += "[*.cs]\r\n";
        }
        else if (language.HasFlag(Language.VisualBasic))
        {
            prefix += "[*.vb]\r\n";
        }

        var result = editorConfigText.WithChanges(new TextChange(new TextSpan(editorConfigText.Length, 0), prefix + newEntry));
        return (result, lastValidHeaderSpanEnd, result.Lines[^2]);
    }
}
