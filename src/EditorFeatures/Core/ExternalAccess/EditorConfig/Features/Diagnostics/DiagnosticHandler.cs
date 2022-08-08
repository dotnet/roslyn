// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using System.Collections.Immutable;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using System.Linq;
using Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(PullDiagnosticHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal sealed class PullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport, VSInternalDiagnosticReport[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PullDiagnosticHandler(
            IDiagnosticAnalyzerService diagnosticAnalyzerService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
            IGlobalOptionService globalOptions)
            : base(diagnosticAnalyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
        {
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalDocumentDiagnosticsParams request) => request.TextDocument;

        protected override VSInternalDiagnosticReport CreateReport(TextDocumentIdentifier? _, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new() { Diagnostics = diagnostics, ResultId = resultId };

        protected override VSInternalDiagnosticReport CreateRemovedReport(TextDocumentIdentifier identifier)
            => CreateReport(identifier, diagnostics: null, resultId: null);

        protected override VSInternalDiagnosticReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
            => CreateReport(identifier, diagnostics: null, resultId);

        protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(VSInternalDocumentDiagnosticsParams diagnosticsParams)
        {
            if (diagnosticsParams.PreviousResultId != null && diagnosticsParams.TextDocument != null)
            {
                return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
            }

            // The client didn't provide us with a previous result to look for, so we can't lookup anything.
            return null;
        }

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
        {
            using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

            if (diagnosticData.Severity == DiagnosticSeverity.Hidden)
            {
                result.Add(VSDiagnosticTags.HiddenInEditor);
                result.Add(VSDiagnosticTags.HiddenInErrorList);
                result.Add(VSDiagnosticTags.SuppressEditorToolTip);
            }
            else
            {
                result.Add(VSDiagnosticTags.VisibleInErrorList);
            }

            result.Add(diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Build)
                ? VSDiagnosticTags.BuildError
                : VSDiagnosticTags.IntellisenseError);

            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
                result.Add(DiagnosticTag.Unnecessary);

            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
                result.Add(VSDiagnosticTags.EditAndContinueError);

            return result.ToArray();
        }

        protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return ValueTaskFactory.FromResult(GetRequestedDocument(context));
        }

        protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static ImmutableArray<IDiagnosticSource> GetRequestedDocument(RequestContext context)
        {
            if (context.AdditionalDocument == null)
            {
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            if (!context.IsTracking(context.AdditionalDocument.GetURI()))
            {
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            return ImmutableArray.Create<IDiagnosticSource>(new DocumentDiagnosticSource(context.AdditionalDocument));
        }

        private record struct DocumentDiagnosticSource(TextDocument Document) : IDiagnosticSource
        {
            public ProjectOrDocumentId GetId() => new(Document.Id);

            public Project GetProject() => Document.Project;

            public Uri GetUri() => Document.GetURI();

            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            {
                var document = context.AdditionalDocument;
                Contract.ThrowIfNull(document);

                var workspace = document.Project.Solution.Workspace;

                var filePath = document.FilePath;
                Contract.ThrowIfNull(filePath);

                var optionSet = workspace.Options;
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = new List<DiagnosticData>();

                var definedSettings = new HashSet<string>();

                foreach (var line in text.Lines)
                {
                    var lineText = line.ToString();

                    // Check that setting definition doesn't have more than 1 '='
                    if (lineText.Where(c => c == '=').Count() > 1)
                    {
                        diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.IncorrectSettingDefinition, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                    }
                    else if (lineText.Contains('='))
                    {
                        var splitted = lineText.Split('=');
                        string leftSide = splitted[0].Trim(), rightSide = splitted[1].Replace(" ", "");

                        var settingsSnapshots = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
                        var codeStyleSettingsItems = settingsSnapshots.codeStyleSnapshot?.Select(sett => sett.GetSettingName());
                        var whitespaceSettingsItems = settingsSnapshots.whitespaceSnapshot?.Select(sett => sett.GetSettingName());
                        var analyzerSettingsItems = settingsSnapshots.analyzerSnapshot?.Select(sett => sett.GetSettingName());
                        var settingsItems = codeStyleSettingsItems.Concat(whitespaceSettingsItems).Concat(analyzerSettingsItems).WhereNotNull();

                        // Checkt that the setting name exists
                        if (!settingsItems.Contains(leftSide))
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SettingNotFound, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                            continue;
                        }

                        // Check for repeated settings
                        if (definedSettings.Contains(leftSide))
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SettingAlreadyDefined, DiagnosticSeverity.Warning, false, 1, Document.Project.Id));
                        }
                        definedSettings.Add(leftSide);

                        // Check for settings that define severities
                        if (rightSide.Contains(':'))
                        {
                            var values = rightSide.Split(':');
                            if (values.Length > 2)
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.IncorrectSettingDefinition, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }

                            if (!SettingDefinesSeverities(leftSide, settingsSnapshots))
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SeveritiesNotSupported, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }

                            if (!SettingHasValue(leftSide, values[0], settingsSnapshots, workspace.Options) && !values[0].IsEmpty())
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }

                            if (!IsSeverity(values[1]))
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SeverityNotDefined, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }
                            continue;
                        }

                        // Check for settings that allow multiple values
                        if (rightSide.Contains(','))
                        {
                            if (!SettingAllowsMultipleValues(leftSide))
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.MultipleValuesNotSupported, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }

                            var values = rightSide.Split(',');
                            var definedValues = new HashSet<string>();
                            foreach (var value in values)
                            {
                                if (!SettingHasValue(leftSide, value.Trim(), settingsSnapshots, workspace.Options) && !value.Trim().IsEmpty())
                                {
                                    diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                }

                                if (definedValues.Contains(value))
                                {
                                    diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueAlreadyAssigned, DiagnosticSeverity.Warning, false, 1, Document.Project.Id));
                                }
                                definedValues.Add(value);
                            }
                            continue;
                        }

                        // Check if value exists for the setting
                        if (!SettingHasValue(leftSide, rightSide, settingsSnapshots, workspace.Options) && !rightSide.IsEmpty())
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                        }
                    }
                }

                return diagnostics.ToImmutableArray();

                static DiagnosticData CreateDiagnosticData(TextDocument document, TextLine line, string editorConfigId, DiagnosticSeverity severity, bool isEnabledByDefault, int warningLevel, ProjectId projectId)
                {
                    var location = new DiagnosticDataLocation(
                            document.Id, sourceSpan: line.Span, originalFilePath: document.FilePath,
                            originalStartLine: line.LineNumber, originalStartColumn: 0, originalEndLine: line.LineNumber, originalEndColumn: line.ToString().Length);
                    return new DiagnosticData(
                        id: editorConfigId,
                        category: EditorConfigDiagnosticIds.GetCategoryFromId(editorConfigId),
                        message: EditorConfigDiagnosticIds.GetMessageFromId(editorConfigId),
                        severity: severity,
                        defaultSeverity: DiagnosticSeverity.Error,
                        isEnabledByDefault: isEnabledByDefault,
                        warningLevel: warningLevel,
                        customTags: ImmutableArray<string>.Empty,
                        properties: ImmutableDictionary<string, string?>.Empty,
                        location: location,
                        projectId: projectId
                    );
                }

                static bool SettingDefinesSeverities(string settingName, SettingsHelper.SettingsSnapshots settingsSnapshots)
                {
                    var codestyleSettings = settingsSnapshots.codeStyleSnapshot?.Where(setting => setting.GetSettingName() == settingName);
                    if (codestyleSettings.Any())
                    {
                        return true;
                    }

                    return false;
                }

                static bool IsSeverity(string severity)
                {
                    var severities = ImmutableArray.Create(new string[] { "none", "silent", "suggestion", "warning", "error" });
                    return severities.Contains(severity);
                }

                static bool SettingAllowsMultipleValues(string settingName)
                {
                    return settingName == "csharp_new_line_before_open_brace" || settingName == "csharp_space_between_parentheses";
                }

                static bool SettingHasValue(string settingName, string settingValue, SettingsHelper.SettingsSnapshots settingsSnapshots, OptionSet optionSet)
                {
                    var codestyleSettings = settingsSnapshots.codeStyleSnapshot?.Where(setting => setting.GetSettingName() == settingName);
                    if (codestyleSettings.Any())
                    {
                        var values = codestyleSettings.First().GetSettingValues(optionSet);
                        return values != null && values.Contains(settingValue);
                    }

                    var whitespaceSettings = settingsSnapshots.whitespaceSnapshot?.Where(setting => setting.GetSettingName() == settingName);
                    if (whitespaceSettings.Any())
                    {
                        var values = whitespaceSettings.First().GetSettingValues(optionSet);
                        return values != null && values.Contains(settingValue);
                    }

                    var analyzerSettings = settingsSnapshots.analyzerSnapshot?.Where(setting => setting.GetSettingName() == settingName);
                    if (analyzerSettings.Any())
                    {
                        var values = analyzerSettings.First().GetSettingValues(optionSet);
                        return values != null && values.Contains(settingValue);
                    }

                    return false;
                }
            }
        }
    }
}
