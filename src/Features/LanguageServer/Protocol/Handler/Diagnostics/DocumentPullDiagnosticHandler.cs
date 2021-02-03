// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [ExportLspMethod(MSLSPMethods.DocumentPullDiagnosticName, mutatesSolutionState: false), Shared]
    internal class DocumentPullDiagnosticHandler : AbstractPullDiagnosticHandler<DocumentDiagnosticsParams, DiagnosticReport>
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentPullDiagnosticHandler(
            IDiagnosticService diagnosticService,
            IDiagnosticAnalyzerService analyzerService)
            : base(diagnosticService)
        {
            _analyzerService = analyzerService;
        }

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.TextDocument;

        protected override DiagnosticReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId)
            => new DiagnosticReport
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

        protected override DiagnosticParams[]? GetPreviousResults(DocumentDiagnosticsParams diagnosticsParams)
            => new[] { diagnosticsParams };

        protected override IProgress<DiagnosticReport[]>? GetProgress(DocumentDiagnosticsParams diagnosticsParams)
            => diagnosticsParams.PartialResultToken;

        protected override DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData)
            => ConvertTags(diagnosticData, potentialDuplicate: false);

        protected override ImmutableArray<Document> GetOrderedDocuments(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            //
            // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
            // handler treats those as separate worlds that they are responsible for.
            return context.Document != null && context.IsTracking(context.Document.GetURI())
                ? ImmutableArray.Create(context.Document)
                : ImmutableArray<Document>.Empty;
        }

        protected override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context, Document document, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken)
        {
            // For open documents, directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
            // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
            // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
            // and do not need to be adjusted.
            return _analyzerService.GetDiagnosticsAsync(document.Project.Solution, documentId: document.Id, cancellationToken: cancellationToken);
        }
    }
}
