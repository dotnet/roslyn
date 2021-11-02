// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Proposed;

using DocumentDiagnosticReport = SumType<FullDocumentDiagnosticReport, UnchangedDocumentDiagnosticReport>;

[ExportRoslynLanguagesLspRequestHandlerProvider, Shared]
[ProvidesMethod(ProposedMethods.TextDocumentDiagnostic)]
internal class DocumentDiagnosticHandlerProvider : AbstractRequestHandlerProvider
{
    private readonly IDiagnosticService _diagnosticService;
    private readonly IDiagnosticAnalyzerService _analyzerService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DocumentDiagnosticHandlerProvider(
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService)
    {
        _diagnosticService = diagnosticService;
        _analyzerService = analyzerService;
    }

    public override ImmutableArray<IRequestHandler> CreateRequestHandlers()
    {
        return ImmutableArray.Create<IRequestHandler>(new DocumentDiagnosticHandler(_diagnosticService, _analyzerService));
    }
}

internal class DocumentDiagnosticHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticParams, DocumentDiagnosticReport, DocumentDiagnosticReport?>
{
    private readonly IDiagnosticAnalyzerService _analyzerService;

    public DocumentDiagnosticHandler(
        IDiagnosticService diagnosticService,
        IDiagnosticAnalyzerService analyzerService)
        : base(diagnosticService)
    {
        _analyzerService = analyzerService;
    }

    public override string Method => ProposedMethods.TextDocumentDiagnostic;

    public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.TextDocument;

    protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
    {
        return ConvertTags(diagnosticData, potentialDuplicate: false);
    }

    protected override DocumentDiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
    {
        if (diagnostics == null)
        {
            return new UnchangedDocumentDiagnosticReport
            {
                ResultId = resultId,
            };
        }
        else
        {
            return new FullDocumentDiagnosticReport
            {
                Items = diagnostics,
                ResultId = resultId
            };
        }
    }

    protected override DocumentDiagnosticReport? CreateReturn(BufferedProgress<DocumentDiagnosticReport> progress)
    {
        return progress.GetValues()?.Last();
    }

    protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
    {
        return _analyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken: cancellationToken);
    }

    protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context)
    {
        if (context.Document == null)
        {
            context.TraceInformation("Ignoring diagnostics request because no document was provided");
            return ImmutableArray<Document>.Empty;
        }

        if (!context.IsTracking(context.Document.GetURI()))
        {
            context.TraceInformation($"Ignoring diagnostics request for untracked document: {context.Document.GetURI()}");
            return ImmutableArray<Document>.Empty;
        }

        return ImmutableArray.Create(context.Document);
    }

    protected override VSInternalDiagnosticParams[]? GetPreviousResults(DocumentDiagnosticParams diagnosticsParams)
    {
        return new VSInternalDiagnosticParams[]
        {
            new VSInternalDiagnosticParams
            {
                PreviousResultId = diagnosticsParams.PreviousResultId,
                TextDocument = diagnosticsParams.TextDocument
            }
        };
    }

    protected override IProgress<DocumentDiagnosticReport[]>? GetProgress(DocumentDiagnosticParams diagnosticsParams)
    {
        return diagnosticsParams.PartialResultToken;
    }
}
