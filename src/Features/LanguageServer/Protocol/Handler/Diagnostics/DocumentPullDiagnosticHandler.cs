﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    [Method(VSInternalMethods.DocumentPullDiagnosticName)]
    internal partial class DocumentPullDiagnosticHandler : AbstractDocumentPullDiagnosticHandler<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport, VSInternalDiagnosticReport[]>
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

        protected override VSInternalDiagnosticReport CreateRemovedReport(TextDocumentIdentifier identifier)
            => CreateReport(identifier, diagnostics: null, resultId: null);

        protected override VSInternalDiagnosticReport CreateUnchangedReport(TextDocumentIdentifier identifier, string resultId)
            => CreateReport(identifier, diagnostics: null, resultId);

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
            return ValueTaskFactory.FromResult(GetDiagnosticSources(context));
        }

        protected override VSInternalDiagnosticReport[]? CreateReturn(BufferedProgress<VSInternalDiagnosticReport> progress)
        {
            return progress.GetValues();
        }

        internal static ImmutableArray<IDiagnosticSource> GetDiagnosticSources(RequestContext context)
        {
            // For the single document case, that is the only doc we want to process.
            //
            // Note: context.Document may be null in the case where the client is asking about a document that we have
            // since removed from the workspace.  In this case, we don't really have anything to process.
            // GetPreviousResults will be used to properly realize this and notify the client that the doc is gone.
            //
            // Only consider open documents here (and only closed ones in the WorkspacePullDiagnosticHandler).  Each
            // handler treats those as separate worlds that they are responsible for.
            var document = context.Document;
            if (document is null)
            {
                context.TraceWarning("Ignoring diagnostics request because no document was provided");
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            if (!context.IsTracking(document.GetURI()))
            {
                context.TraceWarning($"Ignoring diagnostics request for untracked document: {document.GetURI()}");
                return ImmutableArray<IDiagnosticSource>.Empty;
            }

            return ImmutableArray.Create<IDiagnosticSource>(new DocumentDiagnosticSource(document));
        }
    }
}
