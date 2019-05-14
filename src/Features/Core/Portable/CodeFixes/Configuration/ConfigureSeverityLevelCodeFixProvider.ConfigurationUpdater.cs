// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Configuration
{
    internal sealed partial class ConfigureSeverityLevelCodeFixProvider : ISuppressionOrConfigurationFixProvider
    {
        private sealed partial class ConfigurationUpdater
        {
            private const string DiagnosticOptionPrefix = "dotnet_diagnostic.";
            private const string DiagnosticOptionSuffix = ".severity";

            // Regular expression for .editorconfig header.
            // For example: "[*.cs]    # Optional comment"
            private static readonly Regex s_headerPattern = new Regex(@"\[\*([^#\[]*)\]([ ]*[;#].*)?");

            // Regular expression for .editorconfig code style option entry.
            // For example: "dotnet_style_object_initializer = true:suggestion   # Optional comment"
            private static readonly Regex s_optionBasedEntryPattern = new Regex(@"([\w ]+)=([\w ]+):[ ]*([\w]+)([ ]*[;#].*)?");

            // Regular expression for .editorconfig diagnosticID severity configuration entry.
            // For example: "dotnet_diagnostic.CA2000.severity = suggestion   # Optional comment"
            private static readonly Regex s_severityBasedEntryPattern = new Regex(@"([\w \.]+)=[ ]*([\w]+)([ ]*[;#].*)?");

            private readonly string _severity;
            private readonly Diagnostic _diagnostic;
            private readonly Project _project;
            private readonly CancellationToken _cancellationToken;
            private readonly string _language;

            private ConfigurationUpdater(
                string severity,
                Diagnostic diagnostic,
                Project project,
                CancellationToken cancellationToken)
            {
                _severity = severity;
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
            public static async Task<Solution> ConfigureEditorConfig(
                string severity,
                Diagnostic diagnostic,
                Project project,
                CancellationToken cancellationToken)
            {
                var updater = new ConfigurationUpdater(severity, diagnostic, project, cancellationToken);
                return await updater.Configure().ConfigureAwait(false);
            }

            private async Task<Solution> Configure()
            {
                var solution = _project.Solution;

                // Find existing .editorconfig or generate a new one if none exists.
                var editorConfigDocument = FindOrGenerateEditorConfig(ref solution);
                if (editorConfigDocument == null)
                {
                    return solution;
                }

                var result = await editorConfigDocument.GetTextAsync(_cancellationToken).ConfigureAwait(false);
                var headers = new Dictionary<string, TextLine>();

                // For option based code style diagnostic, try to find the .editorconfig key-value pair for the
                // option setting.
                var (optionNameOpt, optionValueOpt) = FindEditorConfigOptionKeyValueForRule();

                // Check if an entry to configure the rule severity already exists in the .editorconfig file.
                // If it does, we update the existing entry with the new severity.
                var configureExistingRuleText = CheckIfRuleExistsAndReplaceInFile(optionNameOpt, result, headers);
                if (configureExistingRuleText != null)
                {
                    return solution.WithAnalyzerConfigDocumentText(editorConfigDocument.Id, configureExistingRuleText);
                }

                // We did not find any existing entry in the in the .editorconfig file to configure rule severity.
                // So we add a new configuration entry to the .editorconfig file.
                var addMissingRuleText = AddMissingRule(optionNameOpt, optionValueOpt, result, headers);
                if (addMissingRuleText != null)
                {
                    return solution.WithAnalyzerConfigDocumentText(editorConfigDocument.Id, addMissingRuleText);
                }

                // Bail out and return the old solution for error cases.
                return solution;
            }

            private AnalyzerConfigDocument FindOrGenerateEditorConfig(ref Solution solution)
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
                solution = solution.AddAnalyzerConfigDocuments(ImmutableArray.Create(documentInfo));
                return solution.GetProject(_project.Id).GetAnalyzerConfigDocument(id);
            }

            private (string optionName, string optionValue) FindEditorConfigOptionKeyValueForRule()
            {
                // For option based code style diagnostic, try to find the .editorconfig key-value pair for the
                // option setting.
                // For example, IDE diagnostics which are configurable with following code style option based .editorconfig entry:
                //      "%option_name% = %option_value%:%severity%
                // we return '(option_name, option_value)'
                var diagnosticIdToEditorConfigMappingService = _project.LanguageServices.GetService<IDiagnosticIdToEditorConfigOptionMappingService>();
                var editorConfigOption = diagnosticIdToEditorConfigMappingService.GetMappedEditorConfigOption(_diagnostic.Id);
                if (editorConfigOption != null)
                {
                    var editorConfigLocation = editorConfigOption.StorageLocations.OfType<IEditorConfigStorageLocation2>().FirstOrDefault();
                    if (editorConfigLocation != null)
                    {
                        var optionKey = new OptionKey(editorConfigOption, editorConfigOption.IsPerLanguage ? _language : null);
                        var optionSet = _project.Solution.Workspace.Options;
                        var value = optionSet.GetOption(optionKey);
                        var editorConfigString = editorConfigLocation.GetEditorConfigString(value, optionSet);
                        if (!string.IsNullOrEmpty(editorConfigString))
                        {
                            var match = s_optionBasedEntryPattern.Match(editorConfigString);
                            if (match.Success)
                            {
                                return (optionName: match.Groups[1].Value.Trim(), optionValue: match.Groups[2].Value.Trim());
                            }
                        }
                    }
                }

                return (null, null);
            }

            private SourceText CheckIfRuleExistsAndReplaceInFile(
                string nameOpt,
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
                    var isSeverityBasedMatch = !isOptionBasedMatch && s_severityBasedEntryPattern.IsMatch(curLineText);
                    if (isOptionBasedMatch || isSeverityBasedMatch)
                    {
                        var groups = isOptionBasedMatch
                            ? s_optionBasedEntryPattern.Match(curLineText).Groups
                            : s_severityBasedEntryPattern.Match(curLineText).Groups;
                        var key = groups[1].Value.ToString().Trim();
                        var optionValue = groups[2].Value.ToString().Trim();
                        var commentIndex = isOptionBasedMatch ? 4 : 3;
                        var commentValue = groups[commentIndex].Value.ToString();

                        // Check if the rule configuration entry we found is under a valid file header
                        // such as '[*.cs]', '[*.vb]', etc. based on current project's langauge.
                        var validRule = true;
                        if (mostRecentHeader != null &&
                            mostRecentHeader.Length != 0)
                        {
                            var allHeaders = mostRecentHeader.Split(',', '.', ' ', '{', '}');
                            if ((_language == LanguageNames.CSharp && !allHeaders.Contains("cs")) ||
                                (_language == LanguageNames.VisualBasic && !allHeaders.Contains("vb")))
                            {
                                validRule = false;
                            }
                        }

                        if (validRule)
                        {
                            // We found the rule in the file -- replace it with updated severity.
                            if (isOptionBasedMatch && key.Equals(nameOpt))
                            {
                                // We found a rule configuration entry of option based form:
                                //      "%option_name% = %option_value%:%severity%
                                var textChange = new TextChange(curLine.Span, $"{key} = {optionValue}:{_severity}{commentValue}");
                                return result.WithChanges(textChange);
                            }
                            else if (isSeverityBasedMatch)
                            {
                                // We found a rule configuration entry of severity based form:
                                //      "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%
                                int diagIdLength = -1;
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
                                        var textChange = new TextChange(curLine.Span, $"{key} = {_severity}{commentValue}");
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
                string optionNameOpt,
                string optionValueOpt,
                SourceText result,
                Dictionary<string, TextLine> headers)
            {
                // Create a new rule configuration entry for the given diagnostic ID.
                // If optionNameOpt and optionValueOpt are non-null, it indicates an option based diagnostic ID
                // which can be configured by a new entry such as: "%option_name% = %option_value%:%severity%
                // Otherwise, it indicates a non-option diagnostic ID,
                // which can be configured by a new entry such as: "dotnet_diagnostic.<%DiagnosticId%>.severity = %severity%

                var newEntry = !string.IsNullOrEmpty(optionNameOpt) && !string.IsNullOrEmpty(optionValueOpt)
                    ? $"{optionNameOpt} = {optionValueOpt}:{_severity}"
                    : $"{DiagnosticOptionPrefix}{_diagnostic.Id}{DiagnosticOptionSuffix} = {_severity}";

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
}
