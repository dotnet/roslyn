// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
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
            Severity
        }

        private const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
        private const string DiagnosticOptionSuffix = ".severity";

        // Regular expression for .editorconfig header.
        // For example: "[*.cs]    # Optional comment"
        //              "[*.{vb,cs}]"
        //              "[*]    ; Optional comment"
        //              "[ConsoleApp/Program.cs]"
        private static readonly Regex s_headerPattern = new Regex(@"\[(\*|[^ #;\[\]]+\.({[^ #;{}\[\]]+}|[^ #;{}\[\]]+))\]\s*([#;].*)?");

        // Regular expression for .editorconfig code style option entry.
        // For example: "dotnet_style_object_initializer = true:suggestion   # Optional comment"
        private static readonly Regex s_optionBasedEntryPattern = new Regex(@"([\w ]+)=([\w ]+):[ ]*([\w]+)([ ]*[;#].*)?");

        // Regular expression for .editorconfig diagnosticID severity configuration entry.
        // For example: "dotnet_diagnostic.CA2000.severity = suggestion   # Optional comment"
        private static readonly Regex s_severityBasedEntryPattern = new Regex(@"([\w \.]+)=[ ]*([\w]+)([ ]*[;#].*)?");

        private readonly string _optionNameOpt;
        private readonly string _newOptionValueOpt;
        private readonly string _newSeverity;
        private readonly ConfigurationKind _configurationKind;
        private readonly Diagnostic _diagnostic;
        private readonly Project _project;
        private readonly CancellationToken _cancellationToken;
        private readonly string _language;

        private ConfigurationUpdater(
            string optionNameOpt,
            string newOptionValueOpt,
            string newSeverity,
            ConfigurationKind configurationKind,
            Diagnostic diagnostic,
            Project project,
            CancellationToken cancellationToken)
        {
            Debug.Assert(configurationKind != ConfigurationKind.OptionValue || !string.IsNullOrEmpty(newOptionValueOpt));
            Debug.Assert(!string.IsNullOrEmpty(newSeverity));

            _optionNameOpt = optionNameOpt;
            _newOptionValueOpt = newOptionValueOpt;
            _newSeverity = newSeverity;
            _configurationKind = configurationKind;
            _diagnostic = diagnostic;
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
            string severity,
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
                    codeStyleOptionValues.Select(t => (t.optionName, t.currentOptionValue, severity)),
                    diagnostic, project, configurationKind: ConfigurationKind.Severity, cancellationToken);
            }
            else
            {
                updater = new ConfigurationUpdater(optionNameOpt: null, newOptionValueOpt: null, severity,
                    configurationKind: ConfigurationKind.Severity, diagnostic, project, cancellationToken);
                return updater.ConfigureAsync();
            }
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
            Project project,
            CancellationToken cancellationToken)
        => ConfigureCodeStyleOptionsAsync(
                SpecializedCollections.SingletonEnumerable((optionName, optionValue, defaultSeverity)),
                diagnostic, project, configurationKind: ConfigurationKind.OptionValue, cancellationToken);

        private static async Task<Solution> ConfigureCodeStyleOptionsAsync(
            IEnumerable<(string optionName, string optionValue, string optionSeverity)> codeStyleOptionValues,
            Diagnostic diagnostic,
            Project project,
            ConfigurationKind configurationKind,
            CancellationToken cancellationToken)
        {
            var currentProject = project;
            foreach (var (optionName, optionValue, severity) in codeStyleOptionValues)
            {
                Debug.Assert(!string.IsNullOrEmpty(optionName));
                Debug.Assert(optionValue != null);
                Debug.Assert(!string.IsNullOrEmpty(severity));

                var updater = new ConfigurationUpdater(optionName, optionValue, severity, configurationKind, diagnostic, currentProject, cancellationToken);
                var solution = await updater.ConfigureAsync().ConfigureAwait(false);
                currentProject = solution.GetProject(project.Id);
            }

            return currentProject.Solution;
        }

        private async Task<Solution> ConfigureAsync()
        {
            var solution = _project.Solution;

            // Find existing .editorconfig or generate a new one if none exists.
            var editorConfigDocument = FindOrGenerateEditorConfig(solution);
            if (editorConfigDocument == null)
            {
                return solution;
            }

            solution = editorConfigDocument.Project.Solution;

            var headers = new Dictionary<string, TextLine>();
            var originalText = await editorConfigDocument.GetTextAsync(_cancellationToken).ConfigureAwait(false);

            // Compute the updated text for analyzer config document.
            var newText = GetNewAnalyzerConfigDocumentText(originalText, headers);

            return newText != null
                ? solution.WithAnalyzerConfigDocumentText(editorConfigDocument.Id, newText)
                : solution;
        }

        private AnalyzerConfigDocument FindOrGenerateEditorConfig(Solution solution)
        {
            if (_project.AnalyzerConfigDocuments.Any())
            {
                var diagnosticFilePath = PathUtilities.GetDirectoryName(_diagnostic.Location.SourceTree?.FilePath ?? _project.FilePath);
                if (!PathUtilities.IsAbsolute(diagnosticFilePath))
                {
                    return null;
                }

                // Currently, we use a simple heuristic to find existing .editorconfig file.
                // We start from the directory of the source file where the diagnostic was reported and walk up
                // the directory tree to find an .editorconfig file.
                // In future, we might change this algorithm, or allow end users to customize it based on options.

                var bestPath = string.Empty;
                AnalyzerConfigDocument bestAnalyzerConfigDocument = null;
                foreach (var analyzerConfigDocument in _project.AnalyzerConfigDocuments)
                {
                    var analyzerConfigDirectory = PathUtilities.GetDirectoryName(analyzerConfigDocument.FilePath);
                    if (diagnosticFilePath.StartsWith(analyzerConfigDirectory) &&
                        analyzerConfigDirectory.Length > bestPath.Length)
                    {
                        bestPath = analyzerConfigDirectory;
                        bestAnalyzerConfigDocument = analyzerConfigDocument;
                    }
                }

                if (bestAnalyzerConfigDocument != null)
                {
                    return bestAnalyzerConfigDocument;
                }
            }

            // Did not find any existing .editorconfig, so create one at root of the project.
            if (!PathUtilities.IsAbsolute(_project.FilePath))
            {
                return null;
            }

            var projectFilePath = PathUtilities.GetDirectoryName(_project.FilePath);
            var newEditorConfigPath = PathUtilities.CombineAbsoluteAndRelativePaths(projectFilePath, ".editorconfig");
            var id = DocumentId.CreateNewId(_project.Id);
            var documentInfo = DocumentInfo.Create(id, ".editorconfig", filePath: newEditorConfigPath);
            var newSolution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo));
            return newSolution.GetProject(_project.Id).GetAnalyzerConfigDocument(id);
        }

        private static ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity)> GetCodeStyleOptionValuesForDiagnostic(
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
                var builder = ArrayBuilder<(string optionName, string currentOptionValue, string currentSeverity)>.GetInstance();

                try
                {
                    foreach (var (_, codeStyleOption, editorConfigLocation) in codeStyleOptions)
                    {
                        if (!TryGetEditorConfigStringParts(codeStyleOption, editorConfigLocation, optionSet, out var parts))
                        {
                            // Did not find a match, bail out.
                            return ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity)>.Empty;
                        }

                        builder.Add(parts);
                    }

                    return builder.ToImmutable();
                }
                finally
                {
                    builder.Free();
                }
            }

            return ImmutableArray<(string optionName, string currentOptionValue, string currentSeverity)>.Empty;
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


        internal static ImmutableArray<(OptionKey optionKey, ICodeStyleOption codeStyleOptionValue, IEditorConfigStorageLocation2 location)> GetCodeStyleOptionsForDiagnostic(
            Diagnostic diagnostic,
            Project project)
        {
            if (IDEDiagnosticIdToOptionMappingHelper.TryGetMappedOptions(diagnostic.Id, project.Language, out var options))
            {
                var optionSet = project.Solution.Workspace.Options;
                var builder = ArrayBuilder<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2)>.GetInstance();

                try
                {
                    foreach (var option in options)
                    {
                        var editorConfigLocation = option.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                        if (editorConfigLocation != null)
                        {
                            var optionKey = new OptionKey(option, option.IsPerLanguage ? project.Language : null);
                            if (optionSet.GetOption(optionKey) is ICodeStyleOption codeStyleOption)
                            {
                                builder.Add((optionKey, codeStyleOption, editorConfigLocation));
                                continue;
                            }
                        }

                        // Did not find a match.
                        return ImmutableArray<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2)>.Empty;
                    }

                    return builder.ToImmutable();
                }
                finally
                {
                    builder.Free();
                }
            }

            return ImmutableArray<(OptionKey, ICodeStyleOption, IEditorConfigStorageLocation2)>.Empty;
        }

        private SourceText GetNewAnalyzerConfigDocumentText(
            SourceText originalText,
            Dictionary<string, TextLine> headers)
        {
            // Check if an entry to configure the rule severity already exists in the .editorconfig file.
            // If it does, we update the existing entry with the new severity.
            var configureExistingRuleText = CheckIfRuleExistsAndReplaceInFile(originalText, headers);
            if (configureExistingRuleText != null)
            {
                return configureExistingRuleText;
            }

            // We did not find any existing entry in the in the .editorconfig file to configure rule severity.
            // So we add a new configuration entry to the .editorconfig file.
            return AddMissingRule(originalText, headers);
        }

        private SourceText CheckIfRuleExistsAndReplaceInFile(
            SourceText result,
            Dictionary<string, TextLine> headers)
        {
            string mostRecentHeader = null;

            foreach (var curLine in result.Lines)
            {
                var curLineText = curLine.ToString();

                // We might have a diagnostic ID configuration entry based on either s_optionBasedEntryPattern or
                // s_severityBasedEntryPattern. Both of these are considered valid severity configurations
                // and should be detected here.
                var isOptionBasedMatch = s_optionBasedEntryPattern.IsMatch(curLineText);
                var isSeverityBasedMatch = _configurationKind == ConfigurationKind.Severity &&
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

                    // Check if the rule configuration entry we found is under a valid file header
                    // such as '[*]', '[*.cs]', '[*.vb]', etc.
                    var validRule = true;
                    if (mostRecentHeader == null)
                    {
                        validRule = false;
                    }
                    else if (mostRecentHeader.Length != 0 &&
                        !mostRecentHeader.Equals("*"))
                    {
                        // Finds the relative path between editorconfig directory and diagnostic filepath
                        var editorConfigDirectory = PathUtilities.GetDirectoryName(FindOrGenerateEditorConfig(_project.Solution).FilePath).ToLowerInvariant();
                        var diagnosticFilePath = _diagnostic.Location.SourceTree.FilePath.ToLowerInvariant();
                        var relativePath = PathUtilities.GetRelativePath(editorConfigDirectory, diagnosticFilePath);
                        relativePath = PathUtilities.NormalizeWithForwardSlash(relativePath);

                        // Verify that editorconfig header regex matches filename
                        var brokenUpHeader = mostRecentHeader.Split(new[] { '.' }, 2);
                        var brokenUpFileExtensions = brokenUpHeader[1].Split(',', ' ', '{', '}');

                        brokenUpHeader[0] = brokenUpHeader[0].Replace("*", ".*");
                        brokenUpHeader[0] = brokenUpHeader[0].Replace("/", @"\/");
                        var headerRegexStr = brokenUpHeader[0] + @"((\." + brokenUpFileExtensions[0] + ")";
                        for (var i = 1; i < brokenUpFileExtensions.Length; i++)
                        {
                            headerRegexStr += @"|(\." + brokenUpFileExtensions[i] + ")";
                        }
                        headerRegexStr += ")";

                        var headerRegex = new Regex(headerRegexStr);
                        if (!headerRegex.IsMatch(relativePath))
                        {
                            validRule = false;
                        }
                        else
                        {
                            var match = headerRegex.Match(relativePath).Value.Split('.');
                            if (!match[0].Contains(PathUtilities.GetFileName(diagnosticFilePath, false)))
                            {
                                validRule = false;
                            }
                        }
                    }

                    if (validRule)
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

                            var textChange = new TextChange(curLine.Span, $"{key} = {newOptionValue}:{newSeverityValue}{commentValue}");
                            return result.WithChanges(textChange);
                        }
                        else if (isSeverityBasedMatch)
                        {
                            // We found a rule configuration entry of severity based form:
                            //      "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%
                            var diagIdLength = -1;
                            if (key.StartsWith(DiagnosticOptionPrefix, StringComparison.Ordinal) &&
                                key.EndsWith(DiagnosticOptionSuffix, StringComparison.Ordinal))
                            {
                                diagIdLength = key.Length - (DiagnosticOptionPrefix.Length + DiagnosticOptionSuffix.Length);
                            }

                            if (diagIdLength >= 0)
                            {
                                var diagId = key.Substring(
                                    DiagnosticOptionPrefix.Length,
                                    diagIdLength);
                                if (string.Equals(diagId, _diagnostic.Id, StringComparison.OrdinalIgnoreCase))
                                {
                                    var textChange = new TextChange(curLine.Span, $"{key} = {_newSeverity}{commentValue}");
                                    return result.WithChanges(textChange);
                                }
                            }
                        }
                    }
                }
                else if (s_headerPattern.IsMatch(curLineText.Trim()))
                {
                    // We found a header entry such as '[*.cs]', '[*.vb]', etc.
                    // Update the most recent header.
                    var groups = s_headerPattern.Match(curLineText.Trim()).Groups;
                    mostRecentHeader = groups[1].Value.ToString().ToLowerInvariant();
                }

                if (mostRecentHeader != null)
                {
                    headers[mostRecentHeader] = curLine;
                }
            }

            return null;
        }

        private SourceText AddMissingRule(
            SourceText result,
            Dictionary<string, TextLine> headers)
        {
            // Create a new rule configuration entry for the given diagnostic ID.
            // If optionNameOpt and optionValueOpt are non-null, it indicates an option based diagnostic ID
            // which can be configured by a new entry such as: "%option_name% = %option_value%:%severity%
            // Otherwise, it indicates a non-option diagnostic ID,
            // which can be configured by a new entry such as: "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%

            var newEntry = !string.IsNullOrEmpty(_optionNameOpt) && !string.IsNullOrEmpty(_newOptionValueOpt)
                ? $"{_optionNameOpt} = {_newOptionValueOpt}:{_newSeverity}"
                : $"{DiagnosticOptionPrefix}{_diagnostic.Id}{DiagnosticOptionSuffix} = {_newSeverity}";

            // Insert a new line and comment text with diagnostic title above the new entry
            newEntry = $"\r\n# {_diagnostic.Id}: {_diagnostic.Descriptor.Title}\r\n{newEntry}\r\n";

            // Check if have a correct existing header for the new entry.
            // If so, we don't need to generate a header guarding the new entry to specific langauge.
            TextLine? existingHeaderOpt = null;
            if (_language == LanguageNames.CSharp && headers.Any(header => header.Key.Contains(".cs")))
            {
                existingHeaderOpt = headers.FirstOrDefault(header => header.Key.Contains(".cs")).Value;
            }
            else if (_language == LanguageNames.VisualBasic && headers.Any(header => header.Key.Contains(".vb")))
            {
                existingHeaderOpt = headers.FirstOrDefault(header => header.Key.Contains(".vb")).Value;
            }

            if (existingHeaderOpt.HasValue)
            {
                if (existingHeaderOpt.Value.ToString().Trim().Length != 0)
                {
                    newEntry = "\r\n" + newEntry;
                }

                var textChange = new TextChange(new TextSpan(existingHeaderOpt.Value.Span.End, 0), newEntry);
                return result.WithChanges(textChange);
            }

            if (_language == LanguageNames.CSharp || _language == LanguageNames.VisualBasic)
            {
                // We need to generate a new header such as '[*.cs]' or '[*.vb]'
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

                if (_language == LanguageNames.CSharp)
                {
                    prefix += "[*.cs]\r\n";
                }
                else
                {
                    prefix += "[*.vb]\r\n";
                }

                var textChange = new TextChange(new TextSpan(result.Length, 0), prefix + newEntry);
                return result.WithChanges(textChange);
            }

            return null;
        }
    }
}
