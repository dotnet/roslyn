// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.EditorConfigSettings.Updater
{
    internal static partial class SettingsUpdateHelper
    {
        private const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
        private const string SeveritySuffix = ".severity";

        public static SourceText? TryUpdateAnalyzerConfigDocument(
            SourceText originalText,
            string filePath,
            IReadOnlyList<(AnalyzerSetting option, DiagnosticSeverity value)> settingsToUpdate)
        {
            if (originalText is null)
                return null;
            if (settingsToUpdate is null)
                return null;
            if (filePath is null)
                return null;

            var settings = settingsToUpdate.Select(x => TryGetOptionValueAndLanguage(x.option, x.value)).ToList();

            return TryUpdateAnalyzerConfigDocument(originalText, filePath, settings);

            static (string option, string value, ImmutableArray<string> ids, string? severity, Language language) TryGetOptionValueAndLanguage(AnalyzerSetting diagnostic, DiagnosticSeverity severity)
            {
                var optionName = $"{DiagnosticOptionPrefix}{diagnostic.Id}{SeveritySuffix}";
                var optionValue = severity.ToEditorConfigString();
                var language = diagnostic.Language;
                return (optionName, optionValue, ImmutableArray<string>.Empty, null, language);
            }
        }

        public static SourceText? TryUpdateAnalyzerConfigDocument(
            SourceText originalText,
            string filePath,
            OptionSet optionSet,
            IReadOnlyList<(IOption2 option, object value)> settingsToUpdate)
        {
            if (originalText is null)
                return null;
            if (settingsToUpdate is null)
                return null;
            if (filePath is null)
                return null;

            var updatedText = originalText;
            var settings = settingsToUpdate.Select(x => TryGetOptionValueAndLanguage(x.option, x.value, optionSet))
                                           .Where(x => x.success)
                                           .Select(x => (x.option, x.value, x.ids, x.severity, x.language))
                                           .ToList();

            return TryUpdateAnalyzerConfigDocument(originalText, filePath, settings);

            static (bool success, string option, string value, ImmutableArray<string> ids, string? severity, Language language) TryGetOptionValueAndLanguage(IOption2 option, object value, OptionSet optionSet)
            {
                if (option.StorageLocations.FirstOrDefault(x => x is IEditorConfigStorageLocation2) is not IEditorConfigStorageLocation2 storageLocation)
                {
                    return (false, null!, null!, ImmutableArray<string>.Empty, null, default);
                }

                var optionName = storageLocation.KeyName;
                var optionValue = storageLocation.GetEditorConfigStringValue(value, optionSet);
                var language = option.IsPerLanguage ? Language.CSharp | Language.VisualBasic : Language.CSharp;

                if (value is ICodeStyleOption codeStyleOption && !optionValue.Contains(':'))
                {
                    var diagnosticIds = IDEDiagnosticIdToOptionMappingHelper.GetDiagnosticIdsForOption(option, LanguageNames.CSharp); // TODO handle VB
                    var severity = codeStyleOption.Notification switch
                    {
                        { Severity: ReportDiagnostic.Hidden } => "silent",
                        { Severity: ReportDiagnostic.Info } => "suggestion",
                        { Severity: ReportDiagnostic.Warn } => "warning",
                        { Severity: ReportDiagnostic.Error } => "error",
                        _ => string.Empty
                    };
                    return (true, optionName, optionValue, diagnosticIds, severity, language);
                }

                return (true, optionName, optionValue, ImmutableArray<string>.Empty, null, language);
            }
        }

        public static SourceText? TryUpdateAnalyzerConfigDocument(
            SourceText originalText,
            string filePath,
            IReadOnlyList<(string option, string value, ImmutableArray<string> ids, string? severity, Language language)> settingsToUpdate)
        {
            if (originalText is null)
                throw new ArgumentNullException(nameof(originalText));
            if (filePath is null)
                throw new ArgumentNullException(nameof(filePath));
            if (settingsToUpdate is null)
                throw new ArgumentNullException(nameof(settingsToUpdate));

            var updatedText = originalText;
            TextLine? lastValidHeaderSpanEnd;
            TextLine? lastValidSpecificHeaderSpanEnd;
            foreach (var (option, value, ids, severity, language) in settingsToUpdate)
            {
                SourceText? newText;
                (newText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd) = severity is not null && !ids.IsEmpty
                    ? UpdateMultiLineOptionIfExistsInFile(updatedText, filePath, option, value, ids, severity, language)
                    : UpdateSingleLineOptionIfExistsInFile(updatedText, filePath, option, value, language);

                if (newText != null)
                {
                    updatedText = newText;
                    continue;
                }

                (newText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd) = severity is not null && !ids.IsEmpty
                    ? AddMissingRuleMultiLine(updatedText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd, option, value, ids, severity, language)
                    : AddMissingRuleSingleLine(updatedText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd, option, value, language);

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

        private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) UpdateMultiLineOptionIfExistsInFile(
            SourceText editorConfigText,
            string filePath,
            string optionName,
            string optionValue,
            ImmutableArray<string> ids,
            string optionSeverity,
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

            var textChanges = new List<TextChange>();
            var foundOptionName = false;
            foreach (var curLine in editorConfigText.Lines)
            {
                var curLineText = curLine.ToString();
                if (IsOption(curLineText))
                {
                    var (untrimmedKey, key, value, severity, comment) = GetOptionParts(curLineText);
                    // Verify the most recent header is a valid header
                    if (IsValidHeader(mostRecentHeader, lastValidHeader))
                    {
                        // Verify the option name matches
                        if (string.Equals(key, optionName, StringComparison.OrdinalIgnoreCase))
                        {
                            var newEntry = GetStringForNewEntry(optionName, optionValue, ids, optionSeverity);
                            // We found the rule in the file -- replace it with updated option value.
                            textChanges.Add(new TextChange(curLine.Span, newEntry));
                            foundOptionName = true;
                            continue;
                        }

                        if (foundOptionName)
                        {
                            foreach (var id in ids)
                            {
                                var expectedString = $"dotnet_diagnostic.{id}.severity";
                                // if we've already updated a multi-line option remove any duplicate diagnostic ids we find
                                if (string.Equals(key, expectedString, StringComparison.OrdinalIgnoreCase))
                                {
                                    textChanges.Add(new TextChange(curLine.SpanIncludingLineBreak, string.Empty));
                                }
                            }
                        }
                    }
                }
                else if (IsHeader(curLineText))
                {
                    mostRecentHeader = curLine;
                    if (ShouldSetAsLastValidHeader(curLineText, out var mostRecentHeaderText))
                    {
                        lastValidHeader = mostRecentHeader;
                    }
                    else
                    {
                        var (fileName, splicedFileExtensions) = GetHeaderParts(mostRecentHeaderText);
                        if ((relativePath.IsEmpty() || FileNameMatchesRelativePath(fileName, relativePath)) &&
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
            if (textChanges.Any())
            {
                return (editorConfigText.WithChanges(textChanges), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
            }

            // Rule not found.
            return (null, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }

        private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) UpdateSingleLineOptionIfExistsInFile(
            SourceText editorConfigText,
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
                if (IsOption(curLineText))
                {
                    var (untrimmedKey, key, value, severity, comment) = GetOptionParts(curLineText);

                    // Verify the most recent header is a valid header
                    if (IsValidHeader(mostRecentHeader, lastValidHeader) &&
                        string.Equals(key, optionName, StringComparison.OrdinalIgnoreCase))
                    {
                        // We found the rule in the file -- replace it with updated option value.
                        textChange = new TextChange(curLine.Span, $"{untrimmedKey}={optionValue}{comment}");
                    }
                }
                else if (IsHeader(curLineText))
                {
                    mostRecentHeader = curLine;
                    if (ShouldSetAsLastValidHeader(curLineText, out var mostRecentHeaderText))
                    {
                        lastValidHeader = mostRecentHeader;
                    }
                    else
                    {
                        var (fileName, splicedFileExtensions) = GetHeaderParts(mostRecentHeaderText);
                        if ((relativePath.IsEmpty() || FileNameMatchesRelativePath(fileName, relativePath)) &&
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
        }

        private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) AddMissingRuleMultiLine(
            SourceText editorConfigText,
            TextLine? lastValidHeaderSpanEnd,
            TextLine? lastValidSpecificHeaderSpanEnd,
            string optionName,
            string optionValue,
            ImmutableArray<string> ids,
            string severity,
            Language language)
        {
            var newEntry = GetStringForNewEntry(optionName, optionValue, ids, severity);
            return AddMissingRule(editorConfigText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd, newEntry, language);
        }

        private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) AddMissingRuleSingleLine(
            SourceText editorConfigText,
            TextLine? lastValidHeaderSpanEnd,
            TextLine? lastValidSpecificHeaderSpanEnd,
            string optionName,
            string optionValue,
            Language language)
        {
            var newEntry = $"{optionName}={optionValue}";
            return AddMissingRule(editorConfigText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd, newEntry, language);
        }

        private static (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) AddMissingRule(
            SourceText editorConfigText,
            TextLine? lastValidHeaderSpanEnd,
            TextLine? lastValidSpecificHeaderSpanEnd,
            string newEntry,
            Language language)
        {
            if (lastValidSpecificHeaderSpanEnd.HasValue)
            {
                if (lastValidSpecificHeaderSpanEnd.Value.ToString().Trim().Length != 0)
                {
                    newEntry = "\r\n" + newEntry; // TODO(jmarolf): do we need to read in the users newline settings?
                }

                return (editorConfigText.WithChanges((TextChange)new TextChange(new TextSpan(lastValidSpecificHeaderSpanEnd.Value.Span.End, 0), newEntry)), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
            }
            else if (lastValidHeaderSpanEnd.HasValue)
            {
                if (lastValidHeaderSpanEnd.Value.ToString().Trim().Length != 0)
                {
                    newEntry = "\r\n" + newEntry; // TODO(jmarolf): do we need to read in the users newline settings?
                }

                return (editorConfigText.WithChanges((TextChange)new TextChange(new TextSpan(lastValidHeaderSpanEnd.Value.Span.End, 0), newEntry)), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
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

            var result = editorConfigText.WithChanges((TextChange)new TextChange(new TextSpan(editorConfigText.Length, 0), prefix + newEntry));
            return (result, lastValidHeaderSpanEnd, result.Lines[^2]);
        }

        private static string GetStringForNewEntry(string optionName, string optionValue, ImmutableArray<string> ids, string severity)
        {
            var sb = new StringBuilder($"{optionName}={optionValue}\r\n");
            var orderedIds = ids.OrderBy(StringComparer.OrdinalIgnoreCase).ToArray();
            foreach (var id in orderedIds.Take(orderedIds.Length - 1))
            {
                sb.AppendLine($"dotnet_diagnostic.{id}.severity={severity}");
            }
            // do not append an additional newline at the end of mult-line options
            sb.Append($"dotnet_diagnostic.{orderedIds.Last()}.severity={severity}");
            return sb.ToString();
        }

        private static (string untrimmedKey, string key, string value, string severitySuffixInValue, string commentValue) GetOptionParts(string? curLineText)
        {
            var groups = s_optionEntryPattern.Match(curLineText).Groups;
            var untrimmedKey = groups[1].Value.ToString();
            var key = untrimmedKey.Trim();
            var value = groups[2].Value.ToString();
            var severitySuffixInValue = groups[3].Value.ToString();
            var commentValue = groups[4].Value.ToString();
            return (untrimmedKey, key, value, severitySuffixInValue, commentValue);
        }

        private static bool IsValidHeader(TextLine? mostRecentHeader, TextLine? lastValidHeader)
        {
            return mostRecentHeader is not null &&
                   lastValidHeader is not null &&
                   mostRecentHeader.Equals(lastValidHeader);
        }

        private static bool ShouldSetAsLastValidHeader(string curLineText, out string mostRecentHeaderText)
        {
            var groups = s_headerPattern.Match(curLineText.Trim()).Groups;
            mostRecentHeaderText = groups[1].Value.ToString().ToLowerInvariant();
            return mostRecentHeaderText.Equals("*", StringComparison.Ordinal);
        }

        private static (string fileName, string[] splicedFileExtensions) GetHeaderParts(string mostRecentHeaderText)
        {
            // We splice on the last occurrence of '.' to account for filenames containing periods.
            var nameExtensionSplitIndex = mostRecentHeaderText.LastIndexOf('.');
            var fileName = string.Empty;
            if (nameExtensionSplitIndex == -1)
            {
                var fileName = mostRecentHeaderText.Substring(0, nameExtensionSplitIndex);
            }
            var splicedFileExtensions = mostRecentHeaderText[(nameExtensionSplitIndex + 1)..].Split(',', ' ', '{', '}');

            // Replacing characters in the header with the regex equivalent.
            fileName = fileName.Replace(".", @"\.");
            fileName = fileName.Replace("*", ".*");
            fileName = fileName.Replace("/", @"\/");

            return (fileName, splicedFileExtensions);
        }

        private static bool FileNameMatchesRelativePath(string fileName, string relativePath)
        {
            return new Regex(fileName).IsMatch(relativePath);
        }

        private static bool IsNotEmptyOrComment(string currentLineText)
        {
            return !string.IsNullOrWhiteSpace(currentLineText) && !currentLineText.Trim().StartsWith("#", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HeaderMatchesLanguageRequirements(Language language, string[] splicedFileExtensions)
        {
            return IsCSharpOnly(language, splicedFileExtensions) || IsVisualBasicOnly(language, splicedFileExtensions) || IsBothVisualBasicAndCSharp(language, splicedFileExtensions);
        }

        private static bool IsCSharpOnly(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.CSharp) && !language.HasFlag(Language.VisualBasic) && splicedFileExtensions.Contains("cs") && splicedFileExtensions.Length == 1;
        }

        private static bool IsVisualBasicOnly(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.VisualBasic) && !language.HasFlag(Language.CSharp) && splicedFileExtensions.Contains("vb") && splicedFileExtensions.Length == 1;
        }

        private static bool IsBothVisualBasicAndCSharp(Language language, string[] splicedFileExtensions)
        {
            return language.HasFlag(Language.VisualBasic) && language.HasFlag(Language.CSharp) && splicedFileExtensions.Contains("vb") && splicedFileExtensions.Contains("cs");
        }

        private static bool IsOption(string curLineText)
        {
            return s_optionEntryPattern.IsMatch(curLineText);
        }

        private static bool IsHeader(string curLineText)
        {
            return s_headerPattern.IsMatch(curLineText.Trim());
        }
    }
}
