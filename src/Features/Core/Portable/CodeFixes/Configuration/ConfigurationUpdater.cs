﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration
{
    /// <summary>
    /// Helper class to configure diagnostic severity or code style option value based on .editorconfig file
    /// </summary>
    internal sealed partial class ConfigurationUpdater
    {
        private enum ConfigurationKind
        {
            OptionValue,
            Severity,
            BulkConfigure
        }

        private const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
        private const string SeveritySuffix = ".severity";
        private const string BulkConfigureAllAnalyzerDiagnosticsOptionKey = "dotnet_analyzer_diagnostic.severity";
        private const string BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix = "dotnet_analyzer_diagnostic.category-";
        private const string AllAnalyzerDiagnosticsCategory = "";

        // Regular expression for .editorconfig header.
        // For example: "[*.cs]    # Optional comment"
        //              "[*.{vb,cs}]"
        //              "[*]    ; Optional comment"
        //              "[ConsoleApp/Program.cs]"
        private static readonly Regex s_headerPattern = new Regex(@"\[(\*|[^ #;\[\]]+\.({[^ #;{}\.\[\]]+}|[^ #;{}\.\[\]]+))\]\s*([#;].*)?");

        // Regular expression for .editorconfig code style option entry.
        // For example: "dotnet_style_object_initializer = true:suggestion   # Optional comment"
        private static readonly Regex s_optionBasedEntryPattern = new Regex(@"([\w ]+)=([\w, ]+):[ ]*([\w]+)([ ]*[;#].*)?");

        // Regular expression for .editorconfig diagnostic severity configuration entry.
        // For example:
        //  1. "dotnet_diagnostic.CA2000.severity = suggestion   # Optional comment"
        //  2. "dotnet_analyzer_diagnostic.category-Security.severity = suggestion   # Optional comment"
        //  3. "dotnet_analyzer_diagnostic.severity = suggestion   # Optional comment"
        private static readonly Regex s_severityBasedEntryPattern = new Regex(@"([\w- \.]+)=[ ]*([\w]+)([ ]*[;#].*)?");

        private readonly string? _optionNameOpt;
        private readonly string? _newOptionValueOpt;
        private readonly string _newSeverity;
        private readonly ConfigurationKind _configurationKind;
        private readonly Diagnostic? _diagnostic;
        private readonly string? _categoryToBulkConfigure;
        private readonly bool _isPerLanguage;
        private readonly Project _project;
        private readonly CancellationToken _cancellationToken;
        private readonly string _language;

        private ConfigurationUpdater(
            string? optionNameOpt,
            string? newOptionValueOpt,
            string newSeverity,
            ConfigurationKind configurationKind,
            Diagnostic? diagnosticToConfigure,
            string? categoryToBulkConfigure,
            bool isPerLanguage,
            Project project,
            CancellationToken cancellationToken)
        {
            Debug.Assert(configurationKind != ConfigurationKind.OptionValue || !string.IsNullOrEmpty(newOptionValueOpt));
            Debug.Assert(!string.IsNullOrEmpty(newSeverity));
            Debug.Assert(diagnosticToConfigure != null ^ categoryToBulkConfigure != null);
            Debug.Assert((categoryToBulkConfigure != null) == (configurationKind == ConfigurationKind.BulkConfigure));

            _optionNameOpt = optionNameOpt;
            _newOptionValueOpt = newOptionValueOpt;
            _newSeverity = newSeverity;
            _configurationKind = configurationKind;
            _diagnostic = diagnosticToConfigure;
            _categoryToBulkConfigure = categoryToBulkConfigure;
            _isPerLanguage = isPerLanguage;
            _project = project;
            _cancellationToken = cancellationToken;
            _language = project.Language;
        }

        /// <summary>
        /// Updates or adds an .editorconfig <see cref="AnalyzerConfigDocument"/> to the given <paramref name="project"/>
        /// so that the severity of the given <paramref name="diagnostic"/> is configured to be the given
        /// <paramref name="severity"/>.
        /// </summary>
        public static Task<Solution> ConfigureSeverityAsync(
            ReportDiagnostic severity,
            Diagnostic diagnostic,
            Project project,
            CancellationToken cancellationToken)
        {
            if (severity == ReportDiagnostic.Default)
            {
                severity = diagnostic.DefaultSeverity.ToReportDiagnostic();
            }

            return ConfigureSeverityAsync(severity.ToEditorConfigString(), diagnostic, project, cancellationToken);
        }

        /// <summary>
        /// Updates or adds an .editorconfig <see cref="AnalyzerConfigDocument"/> to the given <paramref name="project"/>
        /// so that the severity of the given <paramref name="diagnostic"/> is configured to be the given
        /// <paramref name="editorConfigSeverity"/>.
        /// </summary>
        public static Task<Solution> ConfigureSeverityAsync(
            string editorConfigSeverity,
            Diagnostic diagnostic,
            Project project,
            CancellationToken cancellationToken)
        {
            // For option based code style diagnostic, try to find the .editorconfig key-value pair for the
            // option setting.
            var codeStyleOptionValues = GetCodeStyleOptionValuesForDiagnostic(diagnostic, project);

            ConfigurationUpdater updater;
            if (!codeStyleOptionValues.IsEmpty)
            {
                return ConfigureCodeStyleOptionsAsync(
                    codeStyleOptionValues.Select(t => (t.optionName, t.currentOptionValue, editorConfigSeverity, t.isPerLanguage)),
                    diagnostic, project, configurationKind: ConfigurationKind.Severity, cancellationToken);
            }
            else
            {
                updater = new ConfigurationUpdater(optionNameOpt: null, newOptionValueOpt: null, editorConfigSeverity,
                    configurationKind: ConfigurationKind.Severity, diagnostic, categoryToBulkConfigure: null, isPerLanguage: false, project, cancellationToken);
                return updater.ConfigureAsync();
            }
        }

        /// <summary>
        /// Updates or adds an .editorconfig <see cref="AnalyzerConfigDocument"/> to the given <paramref name="project"/>
        /// so that the default severity of the diagnostics with the given <paramref name="category"/> is configured to be the given
        /// <paramref name="editorConfigSeverity"/>.
        /// </summary>
        public static Task<Solution> BulkConfigureSeverityAsync(
            string editorConfigSeverity,
            string category,
            Project project,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(!string.IsNullOrEmpty(category));
            return BulkConfigureSeverityCoreAsync(editorConfigSeverity, category, project, cancellationToken);
        }

        /// <summary>
        /// Updates or adds an .editorconfig <see cref="AnalyzerConfigDocument"/> to the given <paramref name="project"/>
        /// so that the default severity of all diagnostics is configured to be the given
        /// <paramref name="editorConfigSeverity"/>.
        /// </summary>
        public static Task<Solution> BulkConfigureSeverityAsync(
            string editorConfigSeverity,
            Project project,
            CancellationToken cancellationToken)
        {
            return BulkConfigureSeverityCoreAsync(editorConfigSeverity, category: AllAnalyzerDiagnosticsCategory, project, cancellationToken);
        }

        private static Task<Solution> BulkConfigureSeverityCoreAsync(
            string editorConfigSeverity,
            string category,
            Project project,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(category);
            var updater = new ConfigurationUpdater(optionNameOpt: null, newOptionValueOpt: null, editorConfigSeverity,
                configurationKind: ConfigurationKind.BulkConfigure, diagnosticToConfigure: null, category, isPerLanguage: false, project, cancellationToken);
            return updater.ConfigureAsync();
        }

        /// <summary>
        /// Updates or adds an .editorconfig <see cref="AnalyzerConfigDocument"/> to the given <paramref name="project"/>
        /// so that the given <paramref name="optionName"/> is configured to have the given <paramref name="optionValue"/>.
        /// </summary>
        public static Task<Solution> ConfigureCodeStyleOptionAsync(
            string optionName,
            string optionValue,
            string defaultSeverity,
            Diagnostic diagnostic,
            bool isPerLanguage,
            Project project,
            CancellationToken cancellationToken)
        => ConfigureCodeStyleOptionsAsync(
                SpecializedCollections.SingletonEnumerable((optionName, optionValue, defaultSeverity, isPerLanguage)),
                diagnostic, project, configurationKind: ConfigurationKind.OptionValue, cancellationToken);

        private static async Task<Solution> ConfigureCodeStyleOptionsAsync(
            IEnumerable<(string optionName, string optionValue, string optionSeverity, bool isPerLanguage)> codeStyleOptionValues,
            Diagnostic diagnostic,
            Project project,
            ConfigurationKind configurationKind,
            CancellationToken cancellationToken)
        {
            var currentProject = project;
            foreach (var (optionName, optionValue, severity, isPerLanguage) in codeStyleOptionValues)
            {
                Debug.Assert(!string.IsNullOrEmpty(optionName));
                Debug.Assert(optionValue != null);
                Debug.Assert(!string.IsNullOrEmpty(severity));

                var updater = new ConfigurationUpdater(optionName, optionValue, severity, configurationKind,
                    diagnostic, categoryToBulkConfigure: null, isPerLanguage, currentProject, cancellationToken);
                var solution = await updater.ConfigureAsync().ConfigureAwait(false);
                currentProject = solution.GetProject(project.Id)!;
            }

            return currentProject.Solution;
        }

        private async Task<Solution> ConfigureAsync()
        {
            // Find existing .editorconfig or generate a new one if none exists.
            var editorConfigDocument = FindOrGenerateEditorConfig();
            if (editorConfigDocument == null)
            {
                return _project.Solution;
            }

            var solution = editorConfigDocument.Project.Solution;
            var originalText = await editorConfigDocument.GetTextAsync(_cancellationToken).ConfigureAwait(false);

            // Compute the updated text for analyzer config document.
            var newText = GetNewAnalyzerConfigDocumentText(originalText, editorConfigDocument);

            if (newText == null)
            {
                return solution;
            }

            // Add the newly added analyzer config document as a solution item.
            // The analyzer config document is not yet created, so we just mark the file
            // path for tracking and add it as a solution item whenever the file gets created by the code fix application.
            var service = _project.Solution.Workspace.Services.GetService<IAddSolutionItemService>();
            service?.TrackFilePathAndAddSolutionItemWhenFileCreated(editorConfigDocument.FilePath);

            return solution.WithAnalyzerConfigDocumentText(editorConfigDocument.Id, newText);
        }

        private AnalyzerConfigDocument? FindOrGenerateEditorConfig()
        {
            var analyzerConfigPath = _diagnostic != null
                ? _project.TryGetAnalyzerConfigPathForDiagnosticConfiguration(_diagnostic)
                : _project.TryGetAnalyzerConfigPathForProjectConfiguration();
            if (analyzerConfigPath == null)
            {
                return null;
            }

            if (_project.Solution?.FilePath == null)
            {
                // Project has no solution or solution without a file path.
                // Add analyzer config to just the current project.
                return _project.GetOrCreateAnalyzerConfigDocument(analyzerConfigPath);
            }

            // Otherwise, add analyzer config document to all applicable projects for the current project's solution.
            AnalyzerConfigDocument? analyzerConfigDocument = null;
            var analyzerConfigDirectory = PathUtilities.GetDirectoryName(analyzerConfigPath) ?? throw ExceptionUtilities.Unreachable;
            var currentSolution = _project.Solution;
            foreach (var projectId in _project.Solution.ProjectIds)
            {
                var project = currentSolution.GetProject(projectId);
                if (project?.FilePath?.StartsWith(analyzerConfigDirectory) == true)
                {
                    var addedAnalyzerConfigDocument = project.GetOrCreateAnalyzerConfigDocument(analyzerConfigPath);
                    if (addedAnalyzerConfigDocument != null)
                    {
                        analyzerConfigDocument ??= addedAnalyzerConfigDocument;
                        currentSolution = addedAnalyzerConfigDocument.Project.Solution;
                    }
                }
            }

            return analyzerConfigDocument;
        }

        private static ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity, bool isPerLanguage)> GetCodeStyleOptionValuesForDiagnostic(
            Diagnostic diagnostic,
            Project project)
        {
            // For option based code style diagnostic, try to find the .editorconfig key-value pair for the
            // option setting.
            // For example, IDE diagnostics which are configurable with following code style option based .editorconfig entry:
            //      "%option_name% = %option_value%:%severity%
            // we return '(option_name, new_option_value, new_severity)'
            var codeStyleOptions = GetCodeStyleOptionsForDiagnostic(diagnostic, project);
            if (!codeStyleOptions.IsEmpty)
            {
                var optionSet = project.Solution.Workspace.Options;
                var builder = ArrayBuilder<(string optionName, string currentOptionValue, string currentSeverity, bool isPerLanguage)>.GetInstance();

                try
                {
                    foreach (var (_, codeStyleOption, editorConfigLocation, isPerLanguage) in codeStyleOptions)
                    {
                        if (!TryGetEditorConfigStringParts(codeStyleOption, editorConfigLocation, optionSet, out var parts))
                        {
                            // Did not find a match, bail out.
                            return ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity, bool isPerLanguage)>.Empty;
                        }
                        builder.Add((parts.optionName, parts.optionValue, parts.optionSeverity, isPerLanguage));
                    }

                    return builder.ToImmutable();
                }
                finally
                {
                    builder.Free();
                }
            }

            return ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity, bool isPerLanguage)>.Empty;
        }

        internal static bool TryGetEditorConfigStringParts(
            ICodeStyleOption codeStyleOption,
            IEditorConfigStorageLocation2 editorConfigLocation,
            OptionSet optionSet,
            out (string optionName, string optionValue, string optionSeverity) parts)
        {
            var editorConfigString = editorConfigLocation.GetEditorConfigString(codeStyleOption, optionSet);
            if (!string.IsNullOrEmpty(editorConfigString))
            {
                var match = s_optionBasedEntryPattern.Match(editorConfigString);
                if (match.Success)
                {
                    parts = (optionName: match.Groups[1].Value.Trim(),
                             optionValue: match.Groups[2].Value.Trim(),
                             optionSeverity: match.Groups[3].Value.Trim());
                    return true;
                }
            }

            parts = default;
            return false;
        }

        internal static ImmutableArray<(OptionKey optionKey, ICodeStyleOption codeStyleOptionValue, IEditorConfigStorageLocation2 location, bool isPerLanguage)> GetCodeStyleOptionsForDiagnostic(
            Diagnostic diagnostic,
            Project project)
        {
            if (IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnostic.Id, project.Language, out var options))
            {
                var optionSet = project.Solution.Workspace.Options;
                using var _ = ArrayBuilder<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2, bool)>.GetInstance(out var builder);

                foreach (var option in options.OrderBy(option => option.Name))
                {
                    var editorConfigLocation = option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                    if (editorConfigLocation != null)
                    {
                        var optionKey = new OptionKey(option, option.IsPerLanguage ? project.Language : null);
                        if (optionSet.GetOption(optionKey) is ICodeStyleOption codeStyleOption)
                        {
                            builder.Add((optionKey, codeStyleOption, editorConfigLocation, option.IsPerLanguage));
                            continue;
                        }
                    }

                    // Did not find a match.
                    return ImmutableArray<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2, bool)>.Empty;
                }

                return builder.ToImmutable();
            }

            return ImmutableArray<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2, bool)>.Empty;
        }

        private SourceText? GetNewAnalyzerConfigDocumentText(SourceText originalText, AnalyzerConfigDocument editorConfigDocument)
        {
            // Check if an entry to configure the rule severity already exists in the .editorconfig file.
            // If it does, we update the existing entry with the new severity.
            var (newText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd) = CheckIfRuleExistsAndReplaceInFile(originalText, editorConfigDocument);
            if (newText != null)
            {
                return newText;
            }

            // We did not find any existing entry in the in the .editorconfig file to configure rule severity.
            // So we add a new configuration entry to the .editorconfig file.
            return AddMissingRule(originalText, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }

        private (SourceText? newText, TextLine? lastValidHeaderSpanEnd, TextLine? lastValidSpecificHeaderSpanEnd) CheckIfRuleExistsAndReplaceInFile(
            SourceText result,
            AnalyzerConfigDocument editorConfigDocument)
        {
            // If there's an error finding the editorconfig directory, bail out.
            var editorConfigDirectory = PathUtilities.GetDirectoryName(editorConfigDocument.FilePath);
            if (editorConfigDirectory == null)
            {
                return (null, null, null);
            }

            var relativePath = string.Empty;
            var diagnosticFilePath = string.Empty;

            // If diagnostic SourceTree is null, it means either Location.None or Bulk configuration at root editorconfig, and thus no relative path.
            var diagnosticSourceTree = _diagnostic?.Location.SourceTree;
            if (diagnosticSourceTree != null)
            {
                // Finds the relative path between editorconfig directory and diagnostic filepath.
                diagnosticFilePath = diagnosticSourceTree.FilePath.ToLowerInvariant();
                relativePath = PathUtilities.GetRelativePath(editorConfigDirectory.ToLowerInvariant(), diagnosticFilePath);
                relativePath = PathUtilities.NormalizeWithForwardSlash(relativePath);
            }

            TextLine? mostRecentHeader = null;
            TextLine? lastValidHeader = null;
            TextLine? lastValidHeaderSpanEnd = null;

            TextLine? lastValidSpecificHeader = null;
            TextLine? lastValidSpecificHeaderSpanEnd = null;

            var textChange = new TextChange();

            foreach (var curLine in result.Lines)
            {
                var curLineText = curLine.ToString();

                // We might have a diagnostic ID configuration entry based on either s_optionBasedEntryPattern or
                // s_severityBasedEntryPattern. Both of these are considered valid severity configurations
                // and should be detected here.
                var isOptionBasedMatch = s_optionBasedEntryPattern.IsMatch(curLineText);
                var isSeverityBasedMatch = _configurationKind != ConfigurationKind.OptionValue &&
                    !isOptionBasedMatch &&
                    s_severityBasedEntryPattern.IsMatch(curLineText);
                if (isOptionBasedMatch || isSeverityBasedMatch)
                {
                    var groups = isOptionBasedMatch
                        ? s_optionBasedEntryPattern.Match(curLineText).Groups
                        : s_severityBasedEntryPattern.Match(curLineText).Groups;
                    var key = groups[1].Value.ToString().Trim();
                    var commentIndex = isOptionBasedMatch ? 4 : 3;
                    var commentValue = groups[commentIndex].Value.ToString();

                    // Verify the most recent header is a valid header
                    if (mostRecentHeader != null &&
                        lastValidHeader != null &&
                        mostRecentHeader.Equals(lastValidHeader))
                    {
                        // We found the rule in the file -- replace it with updated option value/severity.
                        if (isOptionBasedMatch && key.Equals(_optionNameOpt))
                        {
                            // We found a rule configuration entry of option based form:
                            //      "%option_name% = %option_value%:%severity%
                            var currentOptionValue = groups[2].Value.ToString().Trim();
                            var currentSeverityValue = groups[3].Value.ToString().Trim();
                            var newOptionValue = _configurationKind == ConfigurationKind.OptionValue ? _newOptionValueOpt : currentOptionValue;
                            var newSeverityValue = _configurationKind == ConfigurationKind.Severity ? _newSeverity : currentSeverityValue;

                            textChange = new TextChange(curLine.Span, $"{key} = {newOptionValue}:{newSeverityValue}{commentValue}");
                        }
                        else if (isSeverityBasedMatch)
                        {
                            // We found a rule configuration entry of severity based form:
                            //      "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%
                            //              OR
                            //      "dotnet_analyzer_diagnostic.severity = %severity%
                            //              OR
                            //      "dotnet_analyzer_diagnostic.category-<%DiagnosticCategory%>.severity = %severity%

                            switch (_configurationKind)
                            {
                                case ConfigurationKind.Severity:
                                    RoslynDebug.Assert(_diagnostic != null);
                                    if (key.StartsWith(DiagnosticOptionPrefix, StringComparison.Ordinal) &&
                                        key.EndsWith(SeveritySuffix, StringComparison.Ordinal))
                                    {
                                        var diagIdLength = key.Length - (DiagnosticOptionPrefix.Length + SeveritySuffix.Length);
                                        var diagId = key.Substring(DiagnosticOptionPrefix.Length, diagIdLength);
                                        if (string.Equals(diagId, _diagnostic.Id, StringComparison.OrdinalIgnoreCase))
                                        {
                                            textChange = new TextChange(curLine.Span, $"{key} = {_newSeverity}{commentValue}");
                                        }
                                    }

                                    break;

                                case ConfigurationKind.BulkConfigure:
                                    RoslynDebug.Assert(_categoryToBulkConfigure != null);
                                    if (_categoryToBulkConfigure == AllAnalyzerDiagnosticsCategory)
                                    {
                                        if (key == BulkConfigureAllAnalyzerDiagnosticsOptionKey)
                                        {
                                            textChange = new TextChange(curLine.Span, $"{key} = {_newSeverity}{commentValue}");
                                        }
                                    }
                                    else
                                    {
                                        if (key.StartsWith(BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix, StringComparison.Ordinal) &&
                                            key.EndsWith(SeveritySuffix, StringComparison.Ordinal))
                                        {
                                            var categoryLength = key.Length - (BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix.Length + SeveritySuffix.Length);
                                            var category = key.Substring(BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix.Length, categoryLength);
                                            if (string.Equals(category, _categoryToBulkConfigure, StringComparison.OrdinalIgnoreCase))
                                            {
                                                textChange = new TextChange(curLine.Span, $"{key} = {_newSeverity}{commentValue}");
                                            }
                                        }
                                    }

                                    break;
                            }
                        }
                    }
                }
                else if (s_headerPattern.IsMatch(curLineText.Trim()))
                {
                    // We found a header entry such as '[*.cs]', '[*.vb]', etc.
                    // Verify that header is valid.
                    mostRecentHeader = curLine;
                    var groups = s_headerPattern.Match(curLineText.Trim()).Groups;
                    var mostRecentHeaderText = groups[1].Value.ToString().ToLowerInvariant();

                    if (mostRecentHeaderText.Equals("*"))
                    {
                        lastValidHeader = mostRecentHeader;
                    }
                    else
                    {
                        // We splice on the last occurrence of '.' to account for filenames containing periods.
                        var nameExtensionSplitIndex = mostRecentHeaderText.LastIndexOf('.');
                        var fileName = mostRecentHeaderText.Substring(0, nameExtensionSplitIndex);
                        var splicedFileExtensions = mostRecentHeaderText.Substring(nameExtensionSplitIndex + 1).Split(',', ' ', '{', '}');

                        // Replacing characters in the header with the regex equivalent.
                        fileName = fileName.Replace(".", @"\.");
                        fileName = fileName.Replace("*", ".*");
                        fileName = fileName.Replace("/", @"\/");

                        // Creating the header regex string, ex. [*.{cs,vb}] => ((\.cs)|(\.vb))
                        var headerRegexStr = fileName + @"((\." + splicedFileExtensions[0] + ")";
                        for (var i = 1; i < splicedFileExtensions.Length; i++)
                        {
                            headerRegexStr += @"|(\." + splicedFileExtensions[i] + ")";
                        }
                        headerRegexStr += ")";

                        var headerRegex = new Regex(headerRegexStr);

                        // We check that the relative path of the .editorconfig file to the diagnostic file
                        // matches the header regex pattern.
                        if (headerRegex.IsMatch(relativePath))
                        {
                            var match = headerRegex.Match(relativePath).Value;
                            var matchWithoutExtension = match.Substring(0, match.LastIndexOf('.'));

                            // Edge case: The below statement checks that we correctly handle cases such as a header of [m.cs] and
                            // a file name of Program.cs.
                            if (matchWithoutExtension.Contains(PathUtilities.GetFileName(diagnosticFilePath, false)))
                            {
                                // If the diagnostic's isPerLanguage = true, the rule is valid for both C# and VB.
                                // For the purpose of adding missing rules later, we want to keep track of whether there is a
                                // valid header that contains both [*.cs] and [*.vb]. 
                                // If isPerLanguage = false or a compiler diagnostic, the rule is only valid for one of the languages.
                                // Thus, we want to keep track of whether there is an existing header that only contains [*.cs] or only
                                // [*.vb], depending on the language.
                                // We also keep track of the last valid header for the language.
                                var isLanguageAgnosticEntry = (_diagnostic == null || !SuppressionHelpers.IsCompilerDiagnostic(_diagnostic)) && _isPerLanguage;
                                if (isLanguageAgnosticEntry)
                                {
                                    if ((_language.Equals(LanguageNames.CSharp) || _language.Equals(LanguageNames.VisualBasic)) &&
                                        splicedFileExtensions.Contains("cs") && splicedFileExtensions.Contains("vb"))
                                    {
                                        lastValidSpecificHeader = mostRecentHeader;
                                    }
                                }
                                else if (splicedFileExtensions.Length == 1)
                                {
                                    if (_language.Equals(LanguageNames.CSharp) && splicedFileExtensions.Contains("cs"))
                                    {
                                        lastValidSpecificHeader = mostRecentHeader;
                                    }
                                    else if (_language.Equals(LanguageNames.VisualBasic) && splicedFileExtensions.Contains("vb"))
                                    {
                                        lastValidSpecificHeader = mostRecentHeader;
                                    }
                                }
                                lastValidHeader = mostRecentHeader;
                            }
                        }
                        // Location.None special case.
                        else if (relativePath.IsEmpty() && new Regex(fileName).IsMatch(relativePath))
                        {
                            if ((_language.Equals(LanguageNames.CSharp) && splicedFileExtensions.Contains("cs")) ||
                                    (_language.Equals(LanguageNames.VisualBasic) && splicedFileExtensions.Contains("vb")))
                            {
                                lastValidHeader = mostRecentHeader;
                            }
                        }
                    }
                }

                // We want to keep track of how far this (valid) section spans.
                if (mostRecentHeader != null &&
                    lastValidHeader != null &&
                    mostRecentHeader.Equals(lastValidHeader))
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
                return (result.WithChanges(textChange), lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
            }

            // Rule not found.
            return (null, lastValidHeaderSpanEnd, lastValidSpecificHeaderSpanEnd);
        }

        private SourceText? AddMissingRule(
            SourceText result,
            TextLine? lastValidHeaderSpanEnd,
            TextLine? lastValidSpecificHeaderSpanEnd)
        {
            // Create a new rule configuration entry for the given diagnostic ID or bulk configuration category.
            // If optionNameOpt and optionValueOpt are non-null, it indicates an option based diagnostic ID
            // which can be configured by a new entry such as: "%option_name% = %option_value%:%severity%
            // Otherwise, if diagnostic is non-null, it indicates a non-option diagnostic ID,
            // which can be configured by a new entry such as: "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%
            // Otherwise, it indicates a bulk configuration entry for default severity of a specific diagnostic category or all analyzer diagnostics,
            // which can be configured by a new entry such as:
            //  1. All analyzer diagnostics: "dotnet_analyzer_diagnostic.severity = %severity%
            //  2. Category configuration: "dotnet_analyzer_diagnostic.category-<%DiagnosticCategory%>.severity = %severity%

            var newEntry = !string.IsNullOrEmpty(_optionNameOpt) && !string.IsNullOrEmpty(_newOptionValueOpt)
                ? $"{_optionNameOpt} = {_newOptionValueOpt}:{_newSeverity}"
                : _diagnostic != null
                    ? $"{DiagnosticOptionPrefix}{_diagnostic.Id}{SeveritySuffix} = {_newSeverity}"
                    : _categoryToBulkConfigure == AllAnalyzerDiagnosticsCategory
                        ? $"{BulkConfigureAllAnalyzerDiagnosticsOptionKey} = {_newSeverity}"
                        : $"{BulkConfigureAnalyzerDiagnosticsByCategoryOptionPrefix}{_categoryToBulkConfigure}{SeveritySuffix} = {_newSeverity}";

            // Insert a new line and comment text above the new entry
            var commentPrefix = _diagnostic != null
                ? $"{_diagnostic.Id}: {_diagnostic.Descriptor.Title}"
                : _categoryToBulkConfigure == AllAnalyzerDiagnosticsCategory
                    ? "Default severity for all analyzer diagnostics"
                    : $"Default severity for analyzer diagnostics with category '{_categoryToBulkConfigure}'";

            newEntry = $"\r\n# {commentPrefix}\r\n{newEntry}\r\n";

            // Check if have a correct existing header for the new entry.
            //      - If the diagnostic's isPerLanguage = true, it means the rule is valid for both C# and VB.
            //        Thus, if there is a valid existing header containing both [*.cs] and [*.vb], then we prioritize it.
            //      - If isPerLanguage = false, it means the rule is only valid for one of the languages. Thus, we
            //        prioritize headers that contain only the file extension for the given language.
            //      - If neither of the above hold true, we choose the last existing valid header. 
            //      - If no valid existing headers, we generate a new header.
            if (lastValidSpecificHeaderSpanEnd.HasValue)
            {
                if (lastValidSpecificHeaderSpanEnd.Value.ToString().Trim().Length != 0)
                {
                    newEntry = "\r\n" + newEntry;
                }

                var textChange = new TextChange(new TextSpan(lastValidSpecificHeaderSpanEnd.Value.Span.End, 0), newEntry);
                return result.WithChanges(textChange);
            }
            else if (lastValidHeaderSpanEnd.HasValue)
            {
                if (lastValidHeaderSpanEnd.Value.ToString().Trim().Length != 0)
                {
                    newEntry = "\r\n" + newEntry;
                }

                var textChange = new TextChange(new TextSpan(lastValidHeaderSpanEnd.Value.Span.End, 0), newEntry);
                return result.WithChanges(textChange);
            }

            // We need to generate a new header such as '[*.cs]' or '[*.vb]':
            //      - For compiler diagnostic entries and code style entries which have per-language option = false, generate only [*.cs] or [*.vb].
            //      - For the remainder, generate [*.{cs,vb}]
            if (_language == LanguageNames.CSharp || _language == LanguageNames.VisualBasic)
            {
                // Insert a newline if not already present
                var lines = result.Lines;
                var lastLine = lines.Count > 0 ? lines[lines.Count - 1] : default;
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

                var compilerDiagOrNotPerLang = (_diagnostic != null && SuppressionHelpers.IsCompilerDiagnostic(_diagnostic)) || !_isPerLanguage;
                if (_language.Equals(LanguageNames.CSharp) && compilerDiagOrNotPerLang)
                {
                    prefix += "[*.cs]\r\n";
                }
                else if (_language.Equals(LanguageNames.VisualBasic) && compilerDiagOrNotPerLang)
                {
                    prefix += "[*.vb]\r\n";
                }
                else
                {
                    prefix += "[*.{cs,vb}]\r\n";
                }

                var textChange = new TextChange(new TextSpan(result.Length, 0), prefix + newEntry);
                return result.WithChanges(textChange);
            }

            return null;
        }
    }
}
