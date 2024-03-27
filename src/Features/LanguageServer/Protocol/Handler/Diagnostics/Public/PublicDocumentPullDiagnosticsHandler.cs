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
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Public;

using DocumentDiagnosticReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport>;

// A document diagnostic partial report is defined as having the first literal send = DocumentDiagnosticReport (aka changed / unchanged) followed
// by n DocumentDiagnosticPartialResult literals.
// See https://github.com/microsoft/vscode-languageserver-node/blob/main/protocol/src/common/proposed.diagnostics.md#textDocument_diagnostic
using DocumentDiagnosticPartialReport = SumType<RelatedFullDocumentDiagnosticReport, RelatedUnchangedDocumentDiagnosticReport, DocumentDiagnosticReportPartialResult>;

[Method(Methods.TextDocumentDiagnosticName)]
internal sealed partial class PublicDocumentPullDiagnosticsHandler : AbstractDocumentPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport?>
{
    private readonly string _nonLocalDiagnosticsSourceRegistrationId;
    private readonly IClientLanguageServerManager _clientLanguageServerManager;

    public PublicDocumentPullDiagnosticsHandler(
        IClientLanguageServerManager clientLanguageServerManager,
        IDiagnosticAnalyzerService analyzerService,
        IDiagnosticsRefresher diagnosticsRefresher,
        IGlobalOptionService globalOptions)
        : base(analyzerService, diagnosticsRefresher, globalOptions)
    {
        _nonLocalDiagnosticsSourceRegistrationId = Guid.NewGuid().ToString();
        _clientLanguageServerManager = clientLanguageServerManager;
    }

    /// <summary>
    /// Public API doesn't support categories (yet).
    /// </summary>
    protected override string? GetDiagnosticCategory(DocumentDiagnosticParams diagnosticsParams)
        => null;

    public override TextDocumentIdentifier GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.TextDocument;

    protected override string? GetDiagnosticSourceIdentifier(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.Identifier;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool isLiveSource)
        => ConvertTags(diagnosticData, isLiveSource, potentialDuplicate: false);

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
            if (progressValues.Single().TryGetFirst(out var value))
            {
                return value;
            }

            return progressValues.Single().Second;
        }

        return null;
    }

    protected override ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(DocumentDiagnosticParams diagnosticParams, RequestContext context, CancellationToken cancellationToken)
    {
        var nonLocalDocumentDiagnostics = diagnosticParams.Identifier == DocumentNonLocalDiagnosticIdentifier.ToString();

        // Task list items are not reported through the public LSP diagnostic API.
        return ValueTaskFactory.FromResult(DocumentPullDiagnosticHandler.GetDiagnosticSources(DiagnosticKind.All, nonLocalDocumentDiagnostics, taskList: false, context, GlobalOptions));
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
