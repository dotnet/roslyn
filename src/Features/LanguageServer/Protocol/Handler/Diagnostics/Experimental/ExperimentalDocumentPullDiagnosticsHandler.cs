// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

using DocumentDiagnosticReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka the sumtype of changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
using DocumentDiagnosticPartialReport = SumType<SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>, DocumentDiagnosticPartialResult>;

[Method(ExperimentalMethods.TextDocumentDiagnostic)]
internal class ExperimentalDocumentPullDiagnosticsHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport?>
{
    private readonly IDiagnosticAnalyzerService _analyzerService;

    public ExperimentalDocumentPullDiagnosticsHandler(
        WellKnownLspServerKinds serverKind,
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService,
        EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource)
        : base(serverKind, diagnosticService, editAndContinueDiagnosticUpdateSource)
    {
        _analyzerService = analyzerService;
    }

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.TextDocument;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
    {
        return ConvertTags(diagnosticData, potentialDuplicate: false);
    }

    protected override DocumentDiagnosticPartialReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
    {
        // We will only report once for document pull, so we only need to return the first literal send = DocumentDiagnosticReport.
        var report = diagnostics == null
            ? new DocumentDiagnosticReport(new UnchangedDocumentDiagnosticReport(resultId))
            : new DocumentDiagnosticReport(new FullDocumentDiagnosticReport(resultId, diagnostics));
        return report;
    }

    protected override DocumentDiagnosticReport? CreateReturn(BufferedProgress<DocumentDiagnosticPartialReport> progress)
    {
        // We only ever report one result for document diagnostics, which is the first DocumentDiagnosticReport.
        var progressValues = progress.GetValues();
        if (progressValues != null && progressValues.Length > 0)
        {
            return progressValues.Single().First;
        }

        return null;
    }

    protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
    {
        // We are intentionally getting diagnostics for the up to date LSP snapshot instead of from the IDiagnosticService
        // as the solution crawler does not run in VSCode.  When the solution crawler is removed from VS, the VS LSP diagnostics
        // implementation will switch over to up to date calculation.
        return _analyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken: cancellationToken);
    }

    protected override ValueTask<ImmutableArray<Document>> GetOrderedDocumentsAsync(RequestContext context, CancellationToken cancellationToken)
    {
        return ValueTaskFactory.FromResult(DocumentPullDiagnosticHandler.GetRequestedDocument(context));
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(DocumentDiagnosticParams diagnosticsParams)
    {
        if (diagnosticsParams.PreviousResultId != null && diagnosticsParams.TextDocument != null)
        {
            return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
        }

        return null;
    }
}
