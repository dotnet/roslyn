// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics
{
    /// <summary>
    /// Root type for both document and workspace diagnostic pull requests.
    /// </summary>
    internal abstract class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport> : IRequestHandler<TDiagnosticsParams, TReport[]?>
        where TReport : DiagnosticReport
    {
        /// <summary>
        /// Special value we use to designate workspace diagnostics vs document diagnostics.  Document diagnostics
        /// should always <see cref="DiagnosticReport.Supersedes"/> a workspace diagnostic as the former are 'live'
        /// while the latter are cached and may be stale.
        /// </summary>
        protected const int WorkspaceDiagnosticIdentifier = 1;
        protected const int DocumentDiagnosticIdentifier = 2;

        protected readonly IDiagnosticService DiagnosticService;

        /// <summary>
        /// Lock to protect <see cref="_documentIdToLastResultId"/> and <see cref="_nextDocumentResultId"/>.
        /// </summary>
        private readonly object _gate = new();

        /// <summary>
        /// Mapping of a document to the last result id we reported for it.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), string> _documentIdToLastResultId = new();

        /// <summary>
        /// The next available id to label results with.  Note that results are tagged on a per-document bases.  That
        /// way we can update diagnostics with the client with per-doc granularity.
        /// </summary>
        private long _nextDocumentResultId;

        public abstract string Method { get; }

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        protected AbstractPullDiagnosticHandler(
            IDiagnosticService diagnosticService)
        {
            DiagnosticService = diagnosticService;
            DiagnosticService.DiagnosticsUpdated += OnDiagnosticsUpdated;
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Gets the progress object to stream results to.
        /// </summary>
        protected abstract IProgress<TReport[]>? GetProgress(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
        /// used so we can report which documents were removed and can have all their diagnostics cleared.
        /// </summary>
        protected abstract DiagnosticParams[]? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Returns all the documents that should be processed in the desired order to process them in.
        /// </summary>
        protected abstract ImmutableArray<Document> GetOrderedDocuments(RequestContext context);

        /// <summary>
        /// Creates the <see cref="DiagnosticReport"/> instance we'll report back to clients to let them know our
        /// progress.  Subclasses can fill in data specific to their needs as appropriate.
        /// </summary>
        protected abstract TReport CreateReport(TextDocumentIdentifier? identifier, VSDiagnostic[]? diagnostics, string? resultId);

        /// <summary>
        /// Produce the diagnostics for the specified document.
        /// </summary>
        protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken);

        /// <summary>
        /// Generate the right diagnostic tags for a particular diagnostic.
        /// </summary>
        protected abstract DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData);

        private void OnDiagnosticsUpdated(object? sender, DiagnosticsUpdatedArgs updateArgs)
        {
            if (updateArgs.DocumentId == null)
                return;

            lock (_documentIdToLastResultId)
            {
                // Whenever we hear about changes to a document, drop the data we've stored for it.  We'll recompute it as
                // necessary on the next request.
                _documentIdToLastResultId.Remove((updateArgs.Workspace, updateArgs.DocumentId));
            }
        }

        public async Task<TReport[]?> HandleRequestAsync(
            TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            context.TraceInformation($"{this.GetType()} started getting diagnostics");

            // The progress object we will stream reports to.
            using var progress = BufferedProgress.Create(GetProgress(diagnosticsParams));

            // Get the set of results the request said were previously reported.  We can use this to determine both
            // what to skip, and what files we have to tell the client have been removed.
            var previousResults = GetPreviousResults(diagnosticsParams) ?? Array.Empty<DiagnosticParams>();
            context.TraceInformation($"previousResults.Length={previousResults.Length}");

            // First, let the client know if any workspace documents have gone away.  That way it can remove those for
            // the user from squiggles or error-list.
            HandleRemovedDocuments(context, previousResults, progress);

            // Create a mapping from documents to the previous results the client says it has for them.  That way as we
            // process documents we know if we should tell the client it should stay the same, or we can tell it what
            // the updated diagnostics are.
            var documentToPreviousDiagnosticParams = GetDocumentToPreviousDiagnosticParams(context, previousResults);

            // Next process each file in priority order. Determine if diagnostics are changed or unchanged since the
            // last time we notified the client.  Report back either to the client so they can update accordingly.
            var orderedDocuments = GetOrderedDocuments(context);
            context.TraceInformation($"Processing {orderedDocuments.Length} documents");

            foreach (var document in orderedDocuments)
            {
                context.TraceInformation($"Processing: {document.FilePath}");

                if (!IncludeDocument(document, context.ClientName))
                {
                    context.TraceInformation($"Ignoring document '{document.FilePath}' because of razor/client-name mismatch");
                    continue;
                }

                if (DiagnosticsAreUnchanged(documentToPreviousDiagnosticParams, document))
                {
                    context.TraceInformation($"Diagnostics were unchanged for document: {document.FilePath}");

                    // Nothing changed between the last request and this one.  Report a (null-diagnostics,
                    // same-result-id) response to the client as that means they should just preserve the current
                    // diagnostics they have for this file.
                    var previousParams = documentToPreviousDiagnosticParams[document];
                    progress.Report(CreateReport(previousParams.TextDocument, diagnostics: null, previousParams.PreviousResultId));
                }
                else
                {
                    context.TraceInformation($"Diagnostics were changed for document: {document.FilePath}");
                    await ComputeAndReportCurrentDiagnosticsAsync(context, progress, document, cancellationToken).ConfigureAwait(false);
                }
            }

            // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
            // collecting and return that.
            context.TraceInformation($"{this.GetType()} finished getting diagnostics");
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

        private static Dictionary<Document, DiagnosticParams> GetDocumentToPreviousDiagnosticParams(
            RequestContext context, DiagnosticParams[] previousResults)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<Document, DiagnosticParams>();
            foreach (var diagnosticParams in previousResults)
            {
                if (diagnosticParams.TextDocument != null)
                {
                    var document = context.Solution.GetDocument(diagnosticParams.TextDocument);
                    if (document != null)
                        result[document] = diagnosticParams;
                }
            }

            return result;
        }

        private async Task ComputeAndReportCurrentDiagnosticsAsync(
            RequestContext context,
            BufferedProgress<TReport> progress,
            Document document,
            CancellationToken cancellationToken)
        {
            // Being asked about this document for the first time.  Or being asked again and we have different
            // diagnostics.  Compute and report the current diagnostics info for this document.

            // Razor has a separate option for determining if they should be in push or pull mode.
            var diagnosticMode = document.IsRazorDocument()
                ? InternalDiagnosticsOptions.RazorDiagnosticMode
                : InternalDiagnosticsOptions.NormalDiagnosticMode;

            var workspace = document.Project.Solution.Workspace;
            var isPull = workspace.IsPullDiagnostics(diagnosticMode);

            context.TraceInformation($"Getting '{(isPull ? "pull" : "push")}' diagnostics with mode '{diagnosticMode}'");

            using var _ = ArrayBuilder<VSDiagnostic>.GetInstance(out var result);

            if (isPull)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = await GetDiagnosticsAsync(context, document, diagnosticMode, cancellationToken).ConfigureAwait(false);
                context.TraceInformation($"Got {diagnostics.Length} diagnostics");

                foreach (var diagnostic in diagnostics)
                    result.Add(ConvertDiagnostic(document, text, diagnostic));
            }

            progress.Report(RecordDiagnosticReport(document, result.ToArray()));
        }

        private void HandleRemovedDocuments(RequestContext context, DiagnosticParams[] previousResults, BufferedProgress<TReport> progress)
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
                        context.TraceInformation($"Clearing diagnostics for removed document: {textDocument.Uri}");

                        // Client is asking server about a document that no longer exists (i.e. was removed/deleted from
                        // the workspace). Report a (null-diagnostics, null-result-id) response to the client as that
                        // means they should just consider the file deleted and should remove all diagnostics
                        // information they've cached for it.
                        progress.Report(CreateReport(textDocument, diagnostics: null, resultId: null));
                    }
                }
            }
        }

        private bool DiagnosticsAreUnchanged(Dictionary<Document, DiagnosticParams> documentToPreviousDiagnosticParams, Document document)
        {
            lock (_gate)
            {
                var workspace = document.Project.Solution.Workspace;
                return documentToPreviousDiagnosticParams.TryGetValue(document, out var previousParams) &&
                       _documentIdToLastResultId.TryGetValue((workspace, document.Id), out var lastReportedResultId) &&
                       lastReportedResultId == previousParams.PreviousResultId;
            }
        }

        private TReport RecordDiagnosticReport(Document document, VSDiagnostic[] diagnostics)
        {
            lock (_gate)
            {
                // Keep track of the diagnostics we reported here so that we can short-circuit producing diagnostics for
                // the same diagnostic set in the future.  Use a custom result-id per type (doc diagnostics or workspace
                // diagnostics) so that clients of one don't errantly call into the other.  For example, a client
                // getting document diagnostics should not ask for workspace diagnostics with the result-ids it got for
                // doc-diagnostics.  The two systems are different and cannot share results, or do things like report
                // what changed between each other.
                var resultId = $"{GetType().Name}:{_nextDocumentResultId++}";
                _documentIdToLastResultId[(document.Project.Solution.Workspace, document.Id)] = resultId;
                return CreateReport(ProtocolConversions.DocumentToTextDocumentIdentifier(document), diagnostics, resultId);
            }
        }

        private VSDiagnostic ConvertDiagnostic(Document document, SourceText text, DiagnosticData diagnosticData)
        {
            Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");
            Contract.ThrowIfNull(diagnosticData.DataLocation, $"Got a document diagnostic that did not have a {nameof(diagnosticData.DataLocation)}");

            var project = document.Project;

            // Razor wants to handle all span mapping themselves.  So if we are in razor, return the raw doc spans, and
            // do not map them.
            var useMappedSpan = !document.IsRazorDocument();
            return new VSDiagnostic
            {
                Source = GetType().Name,
                Code = diagnosticData.Id,
                Message = diagnosticData.Message,
                Severity = ConvertDiagnosticSeverity(diagnosticData.Severity),
                Range = ProtocolConversions.LinePositionToRange(DiagnosticData.GetLinePositionSpan(diagnosticData.DataLocation, text, useMappedSpan)),
                Tags = ConvertTags(diagnosticData),
                DiagnosticType = diagnosticData.Category,
                Projects = new[]
                {
                    new ProjectAndContext
                    {
                        ProjectIdentifier = project.Id.Id.ToString(),
                        ProjectName = project.Name,
                    },
                },
            };
        }

        private static LSP.DiagnosticSeverity ConvertDiagnosticSeverity(DiagnosticSeverity severity)
            => severity switch
            {
                // Hidden is translated in ConvertTags to pass along appropriate _ms tags
                // that will hide the item in a client that knows about those tags.
                DiagnosticSeverity.Hidden => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Info => LSP.DiagnosticSeverity.Hint,
                DiagnosticSeverity.Warning => LSP.DiagnosticSeverity.Warning,
                DiagnosticSeverity.Error => LSP.DiagnosticSeverity.Error,
                _ => throw ExceptionUtilities.UnexpectedValue(severity),
            };

        /// <summary>
        /// If you make change in this method, please also update the corresponding file in
        /// src\VisualStudio\Xaml\Impl\Implementation\LanguageServer\Handler\Diagnostics\AbstractPullDiagnosticHandler.cs
        /// </summary>
        protected static DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData, bool potentialDuplicate)
        {
            using var _ = ArrayBuilder<DiagnosticTag>.GetInstance(out var result);

            if (diagnosticData.Severity == DiagnosticSeverity.Hidden)
            {
                result.Add(VSDiagnosticTags.HiddenInEditor);
                result.Add(VSDiagnosticTags.HiddenInErrorList);
                result.Add(VSDiagnosticTags.SuppressEditorToolTip);
            }
            else
            {
                result.Add(VSDiagnosticTags.VisibleInErrorList);
            }

            if (potentialDuplicate)
                result.Add(VSDiagnosticTags.PotentialDuplicate);

            result.Add(diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Build)
                ? VSDiagnosticTags.BuildError
                : VSDiagnosticTags.IntellisenseError);

            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary))
                result.Add(DiagnosticTag.Unnecessary);

            return result.ToArray();
        }
    }
}
