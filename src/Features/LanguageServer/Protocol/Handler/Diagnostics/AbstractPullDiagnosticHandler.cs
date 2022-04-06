// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EditAndContinue;
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
    /// <typeparam name="TDiagnosticsParams">The LSP input param type</typeparam>
    /// <typeparam name="TReport">The LSP type that is reported via IProgress</typeparam>
    /// <typeparam name="TReturn">The LSP type that is returned on completion of the request.</typeparam>
    internal abstract class AbstractPullDiagnosticHandler<TDiagnosticsParams, TReport, TReturn> : IRequestHandler<TDiagnosticsParams, TReturn?> where TDiagnosticsParams : IPartialResultParams<TReport[]>
    {
        /// <summary>
        /// Diagnostic mode setting for Razor.  This should always be <see cref="DiagnosticMode.Pull"/> as there is no push support in Razor.
        /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
        /// </summary>
        private static readonly Option2<DiagnosticMode> s_razorDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "RazorDiagnosticMode", defaultValue: DiagnosticMode.Pull);

        /// <summary>
        /// Diagnostic mode setting for Live Share.  This should always be <see cref="DiagnosticMode.Pull"/> as there is no push support in Live Share.
        /// This option is only for passing to the diagnostics service and can be removed when we switch all of Roslyn to LSP pull.
        /// </summary>
        private static readonly Option2<DiagnosticMode> s_liveShareDiagnosticMode = new(nameof(InternalDiagnosticsOptions), "LiveShareDiagnosticMode", defaultValue: DiagnosticMode.Pull);

        /// <summary>
        /// Special value we use to designate workspace diagnostics vs document diagnostics.  Document diagnostics
        /// should always <see cref="VSInternalDiagnosticReport.Supersedes"/> a workspace diagnostic as the former are 'live'
        /// while the latter are cached and may be stale.
        /// </summary>
        protected const int WorkspaceDiagnosticIdentifier = 1;
        protected const int DocumentDiagnosticIdentifier = 2;

        private readonly EditAndContinueDiagnosticUpdateSource _editAndContinueDiagnosticUpdateSource;

        protected readonly IDiagnosticService DiagnosticService;

        /// <summary>
        /// Cache where we store the data produced by prior requests so that they can be returned if nothing of significance 
        /// changed. The VersionStamp is produced by <see cref="Project.GetDependentVersionAsync(CancellationToken)"/> while the 
        /// Checksum is produced by <see cref="Project.GetDependentChecksumAsync(CancellationToken)"/>.  The former is faster
        /// and works well for us in the normal case.  The latter still allows us to reuse diagnostics when changes happen that
        /// update the version stamp but not the content (for example, forking LSP text).
        /// </summary>
        private readonly VersionedPullCache<(int, VersionStamp?), (int, Checksum)> _versionedCache;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        protected AbstractPullDiagnosticHandler(
            IDiagnosticService diagnosticService,
            EditAndContinueDiagnosticUpdateSource editAndContinueDiagnosticUpdateSource)
        {
            DiagnosticService = diagnosticService;
            _editAndContinueDiagnosticUpdateSource = editAndContinueDiagnosticUpdateSource;
            _versionedCache = new(this.GetType().Name);
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
        /// used so we can report which documents were removed and can have all their diagnostics cleared.
        /// </summary>
        protected abstract ImmutableArray<PreviousPullResult>? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Returns all the documents that should be processed in the desired order to process them in.
        /// </summary>
        protected abstract ValueTask<ImmutableArray<Document>> GetOrderedDocumentsAsync(RequestContext context, CancellationToken cancellationToken);

        /// <summary>
        /// Creates the <see cref="VSInternalDiagnosticReport"/> instance we'll report back to clients to let them know our
        /// progress.  Subclasses can fill in data specific to their needs as appropriate.
        /// </summary>
        protected abstract TReport CreateReport(TextDocumentIdentifier identifier, LSP.Diagnostic[]? diagnostics, string? resultId);

        protected abstract TReturn? CreateReturn(BufferedProgress<TReport> progress);

        /// <summary>
        /// Produce the diagnostics for the specified document.
        /// </summary>
        protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, DiagnosticMode diagnosticMode, CancellationToken cancellationToken);

        /// <summary>
        /// Generate the right diagnostic tags for a particular diagnostic.
        /// </summary>
        protected abstract DiagnosticTag[] ConvertTags(DiagnosticData diagnosticData);

        public async Task<TReturn?> HandleRequestAsync(
            TDiagnosticsParams diagnosticsParams, RequestContext context, CancellationToken cancellationToken)
        {
            context.TraceInformation($"{this.GetType()} started getting diagnostics");

            // The progress object we will stream reports to.
            using var progress = BufferedProgress.Create(diagnosticsParams.PartialResultToken);

            // Get the set of results the request said were previously reported.  We can use this to determine both
            // what to skip, and what files we have to tell the client have been removed.
            var previousResults = GetPreviousResults(diagnosticsParams) ?? ImmutableArray<PreviousPullResult>.Empty;
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
            var orderedDocuments = await GetOrderedDocumentsAsync(context, cancellationToken).ConfigureAwait(false);
            context.TraceInformation($"Processing {orderedDocuments.Length} documents");

            foreach (var document in orderedDocuments)
            {
                context.TraceInformation($"Processing: {document.FilePath}");

                // not be asked for workspace docs in razor.
                // not send razor docs in workspace docs for c#

                var encVersion = _editAndContinueDiagnosticUpdateSource.Version;

                var project = document.Project;
                var newResultId = await _versionedCache.GetNewResultIdAsync(
                    documentToPreviousDiagnosticParams,
                    document,
                    computeCheapVersionAsync: async () => (encVersion, await project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false)),
                    computeExpensiveVersionAsync: async () => (encVersion, await project.GetDependentChecksumAsync(cancellationToken).ConfigureAwait(false)),
                    cancellationToken).ConfigureAwait(false);
                if (newResultId != null)
                {
                    context.TraceInformation($"Diagnostics were changed for document: {document.FilePath}");
                    progress.Report(await ComputeAndReportCurrentDiagnosticsAsync(context, document, newResultId, context.ClientCapabilities, cancellationToken).ConfigureAwait(false));
                }
                else
                {
                    context.TraceInformation($"Diagnostics were unchanged for document: {document.FilePath}");

                    // Nothing changed between the last request and this one.  Report a (null-diagnostics,
                    // same-result-id) response to the client as that means they should just preserve the current
                    // diagnostics they have for this file.
                    var previousParams = documentToPreviousDiagnosticParams[document];
                    progress.Report(CreateReport(previousParams.TextDocument, diagnostics: null, previousParams.PreviousResultId));
                }
            }

            // If we had a progress object, then we will have been reporting to that.  Otherwise, take what we've been
            // collecting and return that.
            context.TraceInformation($"{this.GetType()} finished getting diagnostics");
            return CreateReturn(progress);
        }

        private static Dictionary<Document, PreviousPullResult> GetDocumentToPreviousDiagnosticParams(
            RequestContext context, ImmutableArray<PreviousPullResult> previousResults)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<Document, PreviousPullResult>();
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

        private async Task<TReport> ComputeAndReportCurrentDiagnosticsAsync(
            RequestContext context,
            Document document,
            string resultId,
            ClientCapabilities clientCapabilities,
            CancellationToken cancellationToken)
        {
            var diagnosticModeOption = context.ServerKind switch
            {
                WellKnownLspServerKinds.LiveShareLspServer => s_liveShareDiagnosticMode,
                WellKnownLspServerKinds.RazorLspServer => s_razorDiagnosticMode,
                _ => InternalDiagnosticsOptions.NormalDiagnosticMode,
            };

            var diagnosticMode = context.GlobalOptions.GetDiagnosticMode(diagnosticModeOption);
            var isPull = diagnosticMode == DiagnosticMode.Pull;

            context.TraceInformation($"Getting '{(isPull ? "pull" : "push")}' diagnostics with mode '{diagnosticMode}'");

            using var _ = ArrayBuilder<LSP.Diagnostic>.GetInstance(out var result);

            if (isPull)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var diagnostics = await GetDiagnosticsAsync(context, document, diagnosticMode, cancellationToken).ConfigureAwait(false);
                context.TraceInformation($"Got {diagnostics.Length} diagnostics");

                foreach (var diagnostic in diagnostics)
                    result.Add(ConvertDiagnostic(document, text, diagnostic, clientCapabilities));
            }

            return CreateReport(ProtocolConversions.DocumentToTextDocumentIdentifier(document), result.ToArray(), resultId);
        }

        private void HandleRemovedDocuments(RequestContext context, ImmutableArray<PreviousPullResult> previousResults, BufferedProgress<TReport> progress)
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

        private LSP.Diagnostic ConvertDiagnostic(Document document, SourceText text, DiagnosticData diagnosticData, ClientCapabilities capabilities)
        {
            Contract.ThrowIfNull(diagnosticData.Message, $"Got a document diagnostic that did not have a {nameof(diagnosticData.Message)}");
            Contract.ThrowIfNull(diagnosticData.DataLocation, $"Got a document diagnostic that did not have a {nameof(diagnosticData.DataLocation)}");

            var project = document.Project;

            // We currently do not map diagnostics spans as
            //   1.  Razor handles span mapping for razor files on their side.
            //   2.  LSP does not allow us to report document pull diagnostics for a different file path.
            //   3.  The VS LSP client does not support document pull diagnostics for files outside our content type.
            //   4.  This matches classic behavior where we only squiggle the original location anyway.
            var useMappedSpan = false;
            if (!capabilities.HasVisualStudioLspCapability())
            {
                var diagnostic = CreateBaseLspDiagnostic();
                return diagnostic;
            }
            else
            {
                var vsDiagnostic = CreateBaseLspDiagnostic();
                vsDiagnostic.DiagnosticType = diagnosticData.Category;
                vsDiagnostic.Projects = new[]
                {
                    new VSDiagnosticProjectInformation
                    {
                        ProjectIdentifier = project.Id.Id.ToString(),
                        ProjectName = project.Name,
                    },
                };

                return vsDiagnostic;
            }

            // We can just use VSDiagnostic as it doesn't have any default properties set that
            // would get automatically serialized.
            LSP.VSDiagnostic CreateBaseLspDiagnostic()
            {
                return new LSP.VSDiagnostic
                {
                    Source = "Roslyn",
                    Code = diagnosticData.Id,
                    CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.GetValidHelpLinkUri()),
                    Message = diagnosticData.Message,
                    Severity = ConvertDiagnosticSeverity(diagnosticData.Severity),
                    Range = ProtocolConversions.LinePositionToRange(DiagnosticData.GetLinePositionSpan(diagnosticData.DataLocation, text, useMappedSpan)),
                    Tags = ConvertTags(diagnosticData),
                };
            }
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

            if (diagnosticData.CustomTags.Contains(WellKnownDiagnosticTags.EditAndContinue))
                result.Add(VSDiagnosticTags.EditAndContinueError);

            return result.ToArray();
        }
    }
}
