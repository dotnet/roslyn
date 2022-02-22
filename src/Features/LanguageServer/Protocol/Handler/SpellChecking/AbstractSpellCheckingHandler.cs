// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SpellCheck;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SpellChecking
{
    /// <summary>
    /// Root type for both document and workspace spell checking requests.
    /// </summary>
    internal abstract class AbstractSpellCheckingHandler<TParams, TReport>
        : IRequestHandler<TParams, TReport[]?>
        where TParams : IPartialResultParams<TReport[]>
        where TReport : VSInternalSpellCheckableRangeReport
    {
        protected record struct PreviousResult(string PreviousResultId, TextDocumentIdentifier TextDocument);

        /// <summary>
        /// Lock to protect <see cref="_documentIdToLastResult"/> and <see cref="_nextDocumentResultId"/>. Since this is
        /// a non-mutating request handler it is possible for calls to <see cref="HandleRequestAsync"/> to run
        /// concurrently.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// Mapping of a document to the data used to make the last spell checking report which contains:
        /// <list type="bullet">
        ///   <item>The resultId reported to the client.</item>
        ///   <item>A checksum combining the project options <see cref="ProjectState.GetParseOptionsChecksum"/> and the <see cref="DocumentStateChecksums.Text"/>.</item>
        /// </list>
        /// This is used to determine if we need to re-calculate spans.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), (string resultId, (Checksum parseOptionsChecksum, Checksum textChecksum) checksums)> _documentIdToLastResult = new();

        /// <summary>
        /// The next available id to label results with.  Note that results are tagged on a per-document bases.  That
        /// way we can update spell checking with the client with per-doc granularity.
        /// </summary>
        private long _nextDocumentResultId;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        protected AbstractSpellCheckingHandler()
        {
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TParams requestParams);

        /// <summary>
        /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
        /// used so we can report which documents were removed and can have all their spell checking results cleared.
        /// </summary>
        protected abstract ImmutableArray<PreviousResult>? GetPreviousResults(TParams requestParams);

        /// <summary>
        /// Returns all the documents that should be processed in the desired order to process them in.
        /// </summary>
        protected abstract ImmutableArray<Document> GetOrderedDocuments(RequestContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the <see cref="VSInternalSpellCheckableRangeReport"/> instance we'll report back to clients to let them know our
        /// progress.  Subclasses can fill in data specific to their needs as appropriate.
        /// </summary>
        protected abstract TReport CreateReport(TextDocumentIdentifier identifier, VSInternalSpellCheckableRange[]? ranges, string? resultId);

        public async Task<TReport[]?> HandleRequestAsync(
            TParams requestParams, RequestContext context, CancellationToken cancellationToken)
        {
            context.TraceInformation($"{this.GetType()} started getting spell checking spans");

            // The progress object we will stream reports to.
            using var progress = BufferedProgress.Create(requestParams.PartialResultToken);

            // Get the set of results the request said were previously reported.  We can use this to determine both
            // what to skip, and what files we have to tell the client have been removed.
            var previousResults = GetPreviousResults(requestParams) ?? ImmutableArray<PreviousResult>.Empty;
            context.TraceInformation($"previousResults.Length={previousResults.Length}");

            // First, let the client know if any workspace documents have gone away.  That way it can remove those for
            // the user from squiggles or error-list.
            HandleRemovedDocuments(context, previousResults, progress);

            // Create a mapping from documents to the previous results the client says it has for them.  That way as we
            // process documents we know if we should tell the client it should stay the same, or we can tell it what
            // the updated spans are.
            var documentToPreviousParams = GetDocumentToPreviousParams(context, previousResults);

            // Next process each file in priority order. Determine if spans are changed or unchanged since the
            // last time we notified the client.  Report back either to the client so they can update accordingly.
            var orderedDocuments = GetOrderedDocuments(context, cancellationToken);
            context.TraceInformation($"Processing {orderedDocuments.Length} documents");

            foreach (var document in orderedDocuments)
            {
                context.TraceInformation($"Processing: {document.FilePath}");

                var languageService = document.GetLanguageService<ISpellCheckSpanService>();
                if (languageService == null)
                {
                    context.TraceInformation($"Ignoring document '{document.FilePath}' because it does not support spell checking");
                    continue;
                }

                if (!IncludeDocument(document, context.ClientName))
                {
                    context.TraceInformation($"Ignoring document '{document.FilePath}' because of razor/client-name mismatch");
                    continue;
                }

                var newResultId = await GetNewResultIdAsync(documentToPreviousParams, document, cancellationToken).ConfigureAwait(false);
                if (newResultId != null)
                {
                    context.TraceInformation($"Spans were changed for document: {document.FilePath}");
                    progress.Report(await ComputeAndReportCurrentSpansAsync(
                        document, languageService, newResultId, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    context.TraceInformation($"Spans were unchanged for document: {document.FilePath}");

                    // Nothing changed between the last request and this one.  Report a (null-spans,
                    // same-result-id) response to the client as that means they should just preserve the current
                    // diagnostics they have for this file.
                    var previousParams = documentToPreviousParams[document];
                    progress.Report(CreateReport(previousParams.TextDocument, ranges: null, previousParams.PreviousResultId));
                }
            }

            // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
            // collecting and return that.
            context.TraceInformation($"{this.GetType()} finished getting spans");
            return progress.GetValues();
        }

        private static bool IncludeDocument(Document document, string? clientName)
        {
            // Documents either belong to Razor or not.  We can determine this by checking if the doc has a span-mapping
            // service or not.  If we're not in razor, we do not include razor docs.  If we are in razor, we only
            // include razor docs.
            var isRazorDoc = document.IsRazorDocument();
            var wantsRazorDoc = clientName != null;

            return wantsRazorDoc == isRazorDoc;
        }

        private static Dictionary<Document, PreviousResult> GetDocumentToPreviousParams(
            RequestContext context, ImmutableArray<PreviousResult> previousResults)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<Document, PreviousResult>();
            foreach (var requestParams in previousResults)
            {
                if (requestParams.TextDocument != null)
                {
                    var document = context.Solution.GetDocument(requestParams.TextDocument);
                    if (document != null)
                        result[document] = requestParams;
                }
            }

            return result;
        }

        private async Task<TReport> ComputeAndReportCurrentSpansAsync(
            Document document,
            ISpellCheckSpanService service,
            string resultId,
            CancellationToken cancellationToken)
        {

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var spans = await service.GetSpansAsync(document, cancellationToken).ConfigureAwait(false);

            using var _ = ArrayBuilder<LSP.VSInternalSpellCheckableRange>.GetInstance(spans.Length, out var result);

            foreach (var span in spans)
                result.Add(ConvertSpan(text, span));

            return CreateReport(ProtocolConversions.DocumentToTextDocumentIdentifier(document), result.ToArray(), resultId);
        }

        private void HandleRemovedDocuments(
            RequestContext context, ImmutableArray<PreviousResult> previousResults, BufferedProgress<TReport> progress)
        {
            Contract.ThrowIfNull(context.Solution);

            foreach (var previousResult in previousResults)
            {
                var textDocument = previousResult.TextDocument;
                if (textDocument != null)
                {
                    var document = context.Solution.GetDocument(textDocument);
                    if (document == null)
                    {
                        context.TraceInformation($"Clearing spans for removed document: {textDocument.Uri}");

                        // Client is asking server about a document that no longer exists (i.e. was removed/deleted from
                        // the workspace). Report a (null-spans, null-result-id) response to the client as that means
                        // they should just consider the file deleted and should remove all spans information they've
                        // cached for it.
                        progress.Report(CreateReport(textDocument, ranges: null, resultId: null));
                    }
                }
            }
        }

        /// <summary>
        /// If Spans have changed since the last request this calculates and returns a new non-null resultId to use for
        /// subsequent computation and caches it.
        /// </summary>
        /// <param name="documentToPreviousParams">the resultIds the client sent us.</param>
        /// <param name="document">the document we are currently calculating results for.</param>
        /// <returns>Null when spans are unchanged, otherwise returns a non-null new resultId.</returns>
        private async Task<string?> GetNewResultIdAsync(
            Dictionary<Document, PreviousResult> documentToPreviousParams,
            Document document,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var checksums = await ComputeChecksumsAsync(document, cancellationToken).ConfigureAwait(false);
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (documentToPreviousParams.TryGetValue(document, out var previousParams) &&
                    previousParams.PreviousResultId != null &&
                    _documentIdToLastResult.TryGetValue((workspace, document.Id), out var lastResult) &&
                    lastResult.resultId == previousParams.PreviousResultId)
                {
                    if (lastResult.checksums == checksums)
                    {
                        // The client's resultId matches our cached resultId and the checksums are an exact match for
                        // what we've cached. We return early here to avoid calculating checksums as we know nothing is
                        // changed.
                        return null;
                    }
                }

                // Client didn't give us a resultId, we have nothing cached, or what we had cached didn't match the current project.
                // We need to calculate spans and store what we calculated the spans for.

                // Keep track of the spans we reported here so that we can short-circuit producing spans for the same
                // document in the future.  Use a custom result-id per type (doc spans or workspace spans) so that
                // clients of one don't errantly call into the other.  For example, a client getting document spans
                // should not ask for workspace spans with the result-ids it got for doc-spans.  The two systems are
                // different and cannot share results, or do things like report what changed between each other.
                //
                // Note that we can safely update the map before computation as any cancellation or exception
                // during computation means that the client will never receive this resultId and so cannot ask us for it.
                var newResultId = $"{GetType().Name}:{_nextDocumentResultId++}";
                _documentIdToLastResult[(document.Project.Solution.Workspace, document.Id)] = (newResultId, checksums);
                return newResultId;
            }
        }

        private static async Task<(Checksum parseOptionsChecksum, Checksum textChecksum)> ComputeChecksumsAsync(Document document, CancellationToken cancellationToken)
        {
            var project = document.Project;
            var parseOptionsChecksum = project.State.GetParseOptionsChecksum();

            var documentChecksumState = await document.State.GetStateChecksumsAsync(cancellationToken).ConfigureAwait(false);
            var textChecksum = documentChecksumState.Text;

            return (parseOptionsChecksum, textChecksum);
        }

        private static LSP.VSInternalSpellCheckableRange ConvertSpan(SourceText text, SpellCheckSpan spellCheckSpan)
        {
            var range = ProtocolConversions.TextSpanToRange(spellCheckSpan.TextSpan, text);
            return new VSInternalSpellCheckableRange
            {
                Start = range.Start,
                End = range.End,
                Kind = spellCheckSpan.Kind switch
                {
                    SpellCheckKind.Identifier => VSInternalSpellCheckableRangeKind.Identifier,
                    SpellCheckKind.Comment => VSInternalSpellCheckableRangeKind.Comment,
                    SpellCheckKind.String => VSInternalSpellCheckableRangeKind.String,
                    _ => throw ExceptionUtilities.UnexpectedValue(spellCheckSpan.Kind),
                },
            };
        }
    }
}
