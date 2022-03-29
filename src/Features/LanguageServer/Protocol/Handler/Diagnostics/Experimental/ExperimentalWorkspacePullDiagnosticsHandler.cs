// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

using WorkspaceDocumentDiagnosticReport = SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>;

[Method(ExperimentalMethods.WorkspaceDiagnostic)]
internal class ExperimentalWorkspacePullDiagnosticsHandler : AbstractPullDiagnosticHandler<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport, WorkspaceDiagnosticReport?>
{
    private readonly IDiagnosticAnalyzerService _analyzerService;

    public ExperimentalWorkspacePullDiagnosticsHandler(
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource)
        : base(diagnosticService, editAndContinueDiagnosticUpdateSource)
    {
        _analyzerService = analyzerService;
    }

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(WorkspaceDiagnosticParams diagnosticsParams) => null;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
    {
        return ConvertTags(diagnosticData, potentialDuplicate: false);
    }

    protected override WorkspaceDiagnosticReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
    {
        var itemToReport = diagnostics == null
            ? new WorkspaceDocumentDiagnosticReport(new WorkspaceUnchangedDocumentDiagnosticReport(identifier.Uri, resultId, version: null))
            : new WorkspaceDocumentDiagnosticReport(new WorkspaceFullDocumentDiagnosticReport(identifier.Uri, diagnostics, version: null, resultId));
        return new WorkspaceDiagnosticReport(new[] { itemToReport });
    }

    protected override WorkspaceDiagnosticReport? CreateReturn(BufferedProgress<WorkspaceDiagnosticReport> progress)
    {
        var progressValues = progress.GetValues();
        if (progressValues != null)
            return new WorkspaceDiagnosticReport(progressValues.SelectMany(report => report.Items).ToArray());

        return null;
    }

    protected override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
    {
        var diagnostics = await _analyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        return diagnostics;
    }

    protected override ValueTask<ImmutableArray<Document>> GetOrderedDocumentsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        return WorkspacePullDiagnosticHandler.GetWorkspacePullDocumentsAsync(context, DiagnosticService.GlobalOptions, cancellationToken);
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(WorkspaceDiagnosticParams diagnosticsParams)
    {
        return diagnosticsParams.PreviousResultIds.Select(id => new PreviousPullResult(id.Value, new TextDocumentIdentifier { Uri = id.Uri })
        {
            PreviousResultId = id.Value,
            TextDocument = new TextDocumentIdentifier
            {
                Uri = id.Uri!
            }
        }).ToImmutableArray();
    }
}
