// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.EditorConfigSettings.Data;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExtractMethod;

namespace Microsoft.CodeAnalysis.LanguageServer.EditorConfig.Features
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

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = new List<DiagnosticData>();

                var definedSettings = new HashSet<string>();

                foreach (var line in text.Lines)
                {
                    var lineText = line.ToString().Trim();

                    // If line only contains a comment ignore
                    // # A comment
                    if (lineText.StartsWith("#"))
                    {
                        continue;
                    }

                    // Check if the line contains a setting definition and a comment
                    // setting_name = setting_value # A comment
                    if (lineText.Contains('#'))
                    {
                        lineText = lineText.Split('#').First().Trim();
                    }

                    // Clears the definedSettings map if a new group of settings is defined
                    if (lineText.StartsWith("[") && lineText.EndsWith("]"))
                    {
                        definedSettings.Clear();
                        continue;
                    }

                    if (lineText.Contains('='))
                    {
                        var splitted = lineText.Split('=');
                        string leftSide = splitted[0].Trim(), rightSide = splitted[1].Replace(" ", "");

                        var settingsSnapshots = SettingsHelper.GetSettingsSnapshots(workspace, filePath);
                        var settingsItems = settingsSnapshots.Select(sett => sett.GetSettingName()).WhereNotNull();

                        // Check that setting definition doesn't have more than 1 '='
                        if (lineText.Where(c => c == '=').Count() > 1 && !CanContainAnyString(leftSide))
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.IncorrectSettingDefinition, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                        }

                        // Check that the setting has not been defined
                        if (!definedSettings.Add(leftSide))
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SettingAlreadyDefined, DiagnosticSeverity.Warning, false, 1, Document.Project.Id, leftSide));
                        }

                        // Check for settings that define severities
                        if (rightSide.Contains(':') && !CanContainAnyString(leftSide))
                        {
                            var values = rightSide.Split(':');
                            // Check if it has more than one ':'
                            if (values.Length > 2)
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.IncorrectSettingDefinition, DiagnosticSeverity.Error, false, 1, Document.Project.Id));
                                continue;
                            }

                            // Check if the setting is allowd to define severities
                            if (!SettingDefinesSeverities(leftSide, settingsSnapshots))
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.SeveritiesNotSupported, DiagnosticSeverity.Error, false, 1, Document.Project.Id, leftSide));
                                continue;
                            }

                            // Check that the setting has the specified value
                            if (!SettingHasValue(leftSide, values[0], settingsSnapshots) && !values[0].IsEmpty())
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id, leftSide, values[0]));
                                continue;
                            }

                            continue;
                        }

                        // Check for settings that allow multiple values
                        if (rightSide.Contains(',') && !CanContainAnyString(leftSide))
                        {
                            // Check if setting allows multiple values
                            if (!SettingAllowsMultipleValues(leftSide, settingsSnapshots))
                            {
                                diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.MultipleValuesNotSupported, DiagnosticSeverity.Error, false, 1, Document.Project.Id, leftSide));
                                continue;
                            }

                            var values = rightSide.Split(',');
                            var definedValues = new HashSet<string>();

                            // Iterate over every defined value
                            foreach (var value in values)
                            {
                                // Check that the setting has the specified value
                                if (!SettingHasValue(leftSide, value.Trim(), settingsSnapshots) && !value.Trim().IsEmpty())
                                {
                                    diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id, leftSide, value));
                                }

                                // Check that the setting value has not been defined
                                if (definedValues.Contains(value))
                                {
                                    diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueAlreadyAssigned, DiagnosticSeverity.Warning, false, 1, Document.Project.Id, leftSide, value));
                                }
                                definedValues.Add(value);
                            }
                            continue;
                        }

                        // Check if value exists for the setting
                        if (!SettingHasValue(leftSide, rightSide, settingsSnapshots) && !rightSide.IsEmpty())
                        {
                            diagnostics.Add(CreateDiagnosticData(document, line, EditorConfigDiagnosticIds.ValueNotDefinedInSetting, DiagnosticSeverity.Error, false, 1, Document.Project.Id, leftSide, rightSide));
                        }
                    }
                }

                return diagnostics.ToImmutableArray();

                static DiagnosticData CreateDiagnosticData(TextDocument document, TextLine line, string editorConfigId, DiagnosticSeverity severity, bool isEnabledByDefault, int warningLevel, ProjectId projectId, string settingName = "", string settingValue = "")
                {
                    var location = new DiagnosticDataLocation(
                            document.Id, sourceSpan: line.Span, originalFilePath: document.FilePath,
                            originalStartLine: line.LineNumber, originalStartColumn: 0, originalEndLine: line.LineNumber, originalEndColumn: line.ToString().Length);
                    return new DiagnosticData(
                        id: editorConfigId,
                        category: EditorConfigDiagnosticIds.GetCategoryFromId(editorConfigId),
                        message: EditorConfigDiagnosticIds.GetMessageFromId(editorConfigId, settingName, settingValue),
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

                static bool SettingDefinesSeverities(string settingName, ImmutableArray<IEditorConfigSettingInfo> settingsSnapshots)
                {
                    var foundSetting = settingsSnapshots.Where(setting => setting.GetSettingName() == settingName);
                    if (foundSetting.Any())
                    {
                        return foundSetting.First().SupportsSeverities();
                    }

                    return true;
                }

                static bool SettingAllowsMultipleValues(string settingName, ImmutableArray<IEditorConfigSettingInfo> settingsSnapshots)
                {
                    var foundSetting = settingsSnapshots.Where(setting => setting.GetSettingName() == settingName);
                    if (foundSetting.Any())
                    {
                        return foundSetting.First().AllowsMultipleValues();
                    }

                    return true;
                }

                static bool SettingHasValue(string settingName, string settingValue, ImmutableArray<IEditorConfigSettingInfo> settingsSnapshots)
                {
                    var foundSetting = settingsSnapshots.Where(setting => setting.GetSettingName() == settingName);
                    if (foundSetting.Any())
                    {
                        var setting = foundSetting.First();
                        return setting.GetSettingValues() != null && setting.IsValueValid(settingValue);
                    }

                    return true;
                }

                static bool CanContainAnyString(string settingName) => settingName == "file_header_template";
            }
        }
    }
}
