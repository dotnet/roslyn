// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics.Experimental;
using System.Collections.Immutable;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.EditorConfig.Features
{
    [ExportStatelessLspService(typeof(PullDiagnosticHandler), ProtocolConstants.EditorConfigLanguageContract), Shared]
    [Method(Methods.TextDocumentPublishDiagnosticsName)]
    internal sealed class PullDiagnosticHandler : IRequestHandler<DocumentDiagnosticParams, FullDocumentDiagnosticReport[]?>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PullDiagnosticHandler(IGlobalOptionService globalOptions)
        {
        }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier? GetTextDocumentIdentifier(DocumentDiagnosticParams request) => request.TextDocument;

        public async Task<FullDocumentDiagnosticReport[]?> HandleRequestAsync(DocumentDiagnosticParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Solution);

            using var progress = BufferedProgress.Create(GetProgress(diagnosticsParams));

            // Get the set of results the request said were previously reported.
            var previousResults = GetPreviousResults(diagnosticsParams);

            var documentToPreviousResultId = new Dictionary<TextDocument, string?>();
            if (previousResults != null)
            {
                // Go through the previousResults and check if we need to remove diagnostic information for any documents
                foreach (var previousResult in previousResults)
                {
                    if (previousResult.TextDocument != null)
                    {
                        var document = context.Solution.GetAdditionalDocument(previousResult.TextDocument);
                        if (document == null)
                        {
                            // We can no longer get this document, return null for both diagnostics and resultId
                            progress.Report(CreateReport(resultId: null, diagnostics: null));
                        }
                        else
                        {
                            // Cache the document to previousResultId mapping so we can easily retrieve the resultId later.
                            documentToPreviousResultId[document] = previousResult.PreviousResultId;
                        }
                    }
                }
            }

            foreach (var document in GetDocuments(context))
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var documentId = document.Id;

                var previousResultId = documentToPreviousResultId.TryGetValue(document, out var id) ? id : null;
                progress.Report(CreateReport(" ", diagnostics: null));
            }

            return progress.GetValues();
        }

        /// <summary>
        /// Gets the progress object to stream results to.
        /// </summary>
        public static IProgress<FullDocumentDiagnosticReport[]>? GetProgress(DocumentDiagnosticParams diagnosticsParams) => diagnosticsParams.PartialResultToken as IProgress<FullDocumentDiagnosticReport[]>;

        /// <summary>
        /// Retrieve the previous results we reported.
        /// </summary>
        public static DocumentDiagnosticParams[]? GetPreviousResults(DocumentDiagnosticParams diagnosticsParams) => new[] { diagnosticsParams };

        /// <summary>
        /// Returns all the documents that should be processed.
        /// </summary>
        public static ImmutableArray<TextDocument> GetDocuments(RequestContext context)
        {
            return context.AdditionalDocument == null ? ImmutableArray<TextDocument>.Empty : ImmutableArray.Create(context.AdditionalDocument);
        }

        /// <summary>
        /// Creates the <see cref="VSInternalDiagnosticReport"/> instance we'll report back to clients to let them know our
        /// progress. 
        /// </summary>
        public static FullDocumentDiagnosticReport CreateReport(string? resultId, LSP.Diagnostic[]? diagnostics)
            => new FullDocumentDiagnosticReport(resultId, diagnostics);
    }
}
