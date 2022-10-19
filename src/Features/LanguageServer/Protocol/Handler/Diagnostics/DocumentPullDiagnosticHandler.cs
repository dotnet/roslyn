// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport, VSInternalDiagnosticReport[]>
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;

        public DocumentPullDiagnosticHandler(
            WellKnownLspServerKinds serverKind,
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource)
            : base(serverKind, diagnosticService, editAndContinueDiagnosticUpdateSource)
        {
            _analyzerService = analyzerService;
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(VSInternalDocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.TextDocument;

        protected override VSInternalDiagnosticReport CreateReport(TextDocumentIdentifier identifier, VisualStudio.LanguageServer.Protocol.Diagnostic[]? diagnostics, string? resultId)
            => new VSInternalDiagnosticReport
            {
                Diagnostics = diagnostics,
                ResultId = resultId,
                Identifier = DocumentDiagnosticIdentifier,
                // Mark these diagnostics as superseding any diagnostics for the same document from the
                // WorkspacePullDiagnosticHandler. We are always getting completely accurate and up to date diagnostic
                // values for a particular file, so our results should always be preferred over the workspace-pull
                // values which are cached and may be out of date.
                Supersedes = WorkspaceDiagnosticIdentifier,
            };

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
            => ConvertTags(diagnosticData, potentialDuplicate: false);

        protected override ValueTask<ImmutableArray<Document>> GetOrderedDocumentsAsync(RequestContext context, CancellationToken cancellationToken)
        {
            return ValueTaskFactory.FromResult(GetRequestedDocument(context));
        }

        protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context, Document document, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
        {
            // For open documents, directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
            // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
            // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
            // and do not need to be adjusted.
            return _analyzerService.GetDiagnosticsForSpanAsync(document, range: null, cancellationToken: cancellationToken);
        }

        protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static ImmutableArray<Document> GetRequestedDocument(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            //
            // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
            // handler treats those as separate worlds that they are responsible for.
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
    }
}
