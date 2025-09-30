// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/#documentDiagnosticParams
using DocumentDiagnosticPartialReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>;
using DocumentDiagnosticReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>;

[Method(Methods.TextDocumentDiagnosticName)]
internal sealed partial class PublicDocumentPullDiagnosticsHandler(
    IClientLanguageServerManager clientLanguageServerManager,
    IDiagnosticSourceManager diagnosticSourceManager,
    IDiagnosticsRefresher diagnosticsRefresher,
    IGlobalOptionService globalOptions)
    : AbstractDocumentPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport?>(
        diagnosticsRefresher, diagnosticSourceManager, globalOptions)
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager = clientLanguageServerManager;

    protected override string? GetRequestDiagnosticCategory(DocumentDiagnosticParams diagnosticsParams)
        => diagnosticsParams.Identifier;

    public override TextDocumentIdentifier GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams)
        => diagnosticsParams.TextDocument;

    protected override DocumentDiagnosticPartialReport CreateReport(TextDocumentIdentifier identifier, Roslyn.LanguageServer.Protocol.Diagnostic[] diagnostics, string resultId)
        => new(new RelatedFullDocumentDiagnosticReport
        {
            ResultId = resultId,
            Items = diagnostics,
        });

    protected override DocumentDiagnosticPartialReport CreateRemovedReport(TextDocumentIdentifier identifier)
        => new(new RelatedFullDocumentDiagnosticReport
        {
            ResultId = null,
            Items = [],
        });

    protected override bool TryCreateUnchangedReport(TextDocumentIdentifier identifier, string resultId, out DocumentDiagnosticPartialReport report)
    {
        report = new RelatedUnchangedDocumentDiagnosticReport
        {
            ResultId = resultId
        };
        return true;
    }

    protected override DocumentDiagnosticReport? CreateReturn(BufferedProgress<DocumentDiagnosticPartialReport> progress)
    {
        // We only ever report one result for document diagnostics, which is the first DocumentDiagnosticReport.
        var progressValues = progress.GetValues();
        if (progressValues != null && progressValues.Length > 0)
        {
            // The first report will always be the full report (either changed or unchanged).
            DocumentDiagnosticPartialReport firstReport;
            try
            {
                firstReport = progressValues.Single();
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                firstReport = progressValues[0];
            }

            if (firstReport.TryGetFirst(out var changedReport))
                return changedReport;

            if (firstReport.TryGetSecond(out var unchangedReport))
                return unchangedReport;

            // It is unexpected to have the first report be a partial result.
            throw ExceptionUtilities.UnexpectedValue(firstReport.Third);
        }

        return null;
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
