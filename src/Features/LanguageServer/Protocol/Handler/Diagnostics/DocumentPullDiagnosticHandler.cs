// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using Range = Microsoft.VisualStudio.LanguageServer.Protocol.Range;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport, VSInternalDiagnosticReport[]>
    {
        public DocumentPullDiagnosticHandler(
            IDiagnosticAnalyzerService analyzerService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource,
            IGlobalOptionService globalOptions)
            : base(analyzerService, editAndContinueDiagnosticUpdateSource, globalOptions)
        {
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
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            if (!context.IsTracking(context.Document.GetURI()))
            {
                context.TraceWarning($"Ignoring diagnostics request for untracked document: {context.Document.GetURI()}");
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            return ImmutableArray.Create<IDiagnosticSource>(new DocumentDiagnosticSource(context.Document));
        }

        private record struct DocumentDiagnosticSource(Document Document) : IDiagnosticSource
        {
            public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(IDiagnosticAnalyzerService diagnosticAnalyzerService, RequestContext context, DiagnosticMode diagnosticMode, CancellationToken cancellationToken)
            {
                // For open documents, directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
                // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
                // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
                // and do not need to be adjusted.
                var allSpanDiagnostics = await diagnosticAnalyzerService.GetDiagnosticsForSpanAsync(Document, range: null, cancellationToken: cancellationToken).ConfigureAwait(false);
                return allSpanDiagnostics;
            }
            public ProjectOrDocumentId GetId() => new(Document.Id);

            public Project GetProject() => Document.Project;

            public Uri GetUri() => Document.GetURI();
        }
    }
}
