// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

using WorkspaceDocumentDiagnosticReport = SumType<WorkspaceFullDocumentDiagnosticReport, WorkspaceUnchangedDocumentDiagnosticReport>;

[Method(ExperimentalMethods.WorkspaceDiagnostic)]
internal class ExperimentalWorkspacePullDiagnosticsHandler : AbstractPullDiagnosticHandler<WorkspaceDiagnosticParams, WorkspaceDiagnosticReport, WorkspaceDiagnosticReport?>
{
    public ExperimentalWorkspacePullDiagnosticsHandler(
        IDiagnosticAnalyzerService analyzerService,
        EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
        IGlobalOptionService globalOptions)
        : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
    {
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

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
    {
        return WorkspacePullDiagnosticHandler.GetWorkspacePullDocumentsAsync(context, GlobalOptions, cancellationToken);
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
