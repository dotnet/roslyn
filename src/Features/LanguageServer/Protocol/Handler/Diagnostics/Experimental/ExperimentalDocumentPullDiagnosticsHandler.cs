// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;

using DocumentDiagnosticReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka the sumtype of changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
using DocumentDiagnosticPartialReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport, DocumentDiagnosticPartialResult>;

[Method(ExperimentalMethods.TextDocumentDiagnostic)]
internal class ExperimentalDocumentPullDiagnosticsHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport?>
{
    public ExperimentalDocumentPullDiagnosticsHandler(
        IDiagnosticAnalyzerService analyzerService,
        EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
        IGlobalOptionService globalOptions)
        : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
    {
    }

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.TextDocument;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
    {
        return ConvertTags(diagnosticData, potentialDuplicate: false);
    }

    protected override DocumentDiagnosticPartialReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[] diagnostics, string resultId)
        => new DocumentDiagnosticReport(new FullDocumentDiagnosticReport(resultId, diagnostics));

    protected override DocumentDiagnosticPartialReport CreateRemovedReport(TextDocumentIdentifier identifier)
        => new DocumentDiagnosticReport(new FullDocumentDiagnosticReport(resultId: null, Array.Empty<VisualStudio.LanguageServer.Protocol.Diagnostic>()));

    protected override DocumentDiagnosticPartialReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
        => new DocumentDiagnosticReport(new UnchangedDocumentDiagnosticReport(resultId));

    protected override DocumentDiagnosticReport? CreateReturn(BufferedProgress<DocumentDiagnosticPartialReport> progress)
    {
        // We only ever report one result for document diagnostics, which is the first DocumentDiagnosticReport.
        var progressValues = progress.GetValues();
        if (progressValues != null && progressValues.Length > 0)
        {
            if (progressValues.Single().TryGetFirst(out var value))
            {
                return value;
            }

            return progressValues.Single().Second;
        }

        return null;
    }

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(RequestContext context, CancellationToken cancellationToken)
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
