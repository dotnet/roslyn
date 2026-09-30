// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.DiagnosticSources;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
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
    : AbstractPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticPartialReport, DocumentDiagnosticReport>(
        diagnosticsRefresher, globalOptions), ITextDocumentIdentifierHandler<DocumentDiagnosticParams, TextDocumentIdentifier>
{
    private readonly IClientLanguageServerManager _clientLanguageServerManager = clientLanguageServerManager;
    private readonly IDiagnosticSourceManager _diagnosticSourceManager = diagnosticSourceManager;

    protected override string? GetRequestDiagnosticCategory(DocumentDiagnosticParams diagnosticsParams)
        => diagnosticsParams.Identifier;

    public TextDocumentIdentifier GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams)
        => diagnosticsParams.TextDocument;

    protected override async ValueTask<ImmutableArray<IDiagnosticSource>> GetOrderedDiagnosticSourcesAsync(DocumentDiagnosticParams diagnosticsParams, string? requestDiagnosticCategory, RequestContext context, CancellationToken cancellationToken)
    {
        // Note: the document may be null in the case where the client is asking about a document that we have
        // since removed from the workspace.  In this case, we don't really have anything to process.
        // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
        //
        // Only consider open documents here (and only closed ones in PublicWorkspacePullDiagnosticsHandler).  Each
        // handler treats those as separate worlds that they are responsible for.
        var identifier = GetTextDocumentIdentifier(diagnosticsParams);
        var textDocument = await context.GetTextDocumentAsync(cancellationToken).ConfigureAwait(false);
        if (textDocument is null)
        {
            context.TraceDebug("Ignoring diagnostics request because the text document was not found");
            return [];
        }

        if (!context.IsTracking(identifier.DocumentUri))
        {
            context.TraceWarning($"Ignoring diagnostics request for untracked document: {identifier.DocumentUri}");
            return [];
        }

        return await _diagnosticSourceManager.CreateDocumentDiagnosticSourcesAsync(context, requestDiagnosticCategory, cancellationToken).ConfigureAwait(false);
    }

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

    protected override DocumentDiagnosticReport CreateReturn(BufferedProgress<DocumentDiagnosticPartialReport> progress)
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

        return new RelatedFullDocumentDiagnosticReport
        {
            Items = []
        };
    }

    protected override ImmutableArray<PreviousPullResult>? GetPreviousResults(DocumentDiagnosticParams diagnosticsParams)
    {
        if (diagnosticsParams is { PreviousResultId: not null, TextDocument: not null })
        {
            return ImmutableArray.Create(new PreviousPullResult(diagnosticsParams.PreviousResultId, diagnosticsParams.TextDocument));
        }

        return null;
    }
}
