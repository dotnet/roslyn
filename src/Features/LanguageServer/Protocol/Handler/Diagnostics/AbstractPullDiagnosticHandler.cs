// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        internal record PreviousResult(string PreviousResultId, TextDocumentIdentifier TextDocument);

        /// <summary>
        /// Special value we use to designate workspace diagnostics vs document diagnostics.  Document diagnostics
        /// should always <see cref="VSInternalDiagnosticReport.Supersedes"/> a workspace diagnostic as the former are 'live'
        /// while the latter are cached and may be stale.
        /// </summary>
        protected const int WorkspaceDiagnosticIdentifier = 1;
        protected const int DocumentDiagnosticIdentifier = 2;

        protected readonly IDiagnosticService DiagnosticService;

        /// <summary>
        /// Lock to protect <see cref="_documentIdToLastResult"/>, <see cref="_nextDocumentResultId"/> and <see cref="_projectToProjectDependentChecksum"/>.
        /// Since this is a non-mutating request handler it is possible for
        /// calls to <see cref="HandleRequestAsync(TDiagnosticsParams, RequestContext, CancellationToken)"/>
        /// to run concurrently.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        /// <summary>
        /// Mapping of a document to the data used to make the last diagnostic report which contains:
        /// <list type="bullet">
        ///   <item>The resultId reported to the client.</item>
        ///   <item>The <see cref="Project.GetDependentVersionAsync(CancellationToken)"/> of the project snapshot that was used to calculate diagnostics.
        ///       <para>Note that this version can change even when nothing has actually changed (for example, forking the LSP text, reloading the same project).
        ///       So we additionally store:</para></item>
        ///   <item>A checksum representing the project and its dependencies from <see cref="CalculateDependentProjectChecksumAsync(Project, CancellationToken)"/>.</item>
        /// </list>
        /// This is used to determine if we need to re-calculate diagnostics.
        /// </summary>
        private readonly Dictionary<(Workspace workspace, DocumentId documentId), (string resultId, VersionStamp projectDependentVersion, Checksum projectDependentChecksum)> _documentIdToLastResult = new();

        /// <summary>
        /// A weak table holding the checksums computed by <see cref="CalculateDependentProjectChecksumAsync(Project, CancellationToken)"/>.
        /// Individual project checksums are cached separately, but this lets us generally calculate the aggregate checksum for a particular
        /// project only once.  This is helpful when the client continues to poll us when nothing has changed and we have the same project instance.
        /// </summary>
        private readonly ConditionalWeakTable<Project, AsyncLazy<Checksum>> _projectToProjectDependentChecksum = new();

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
        }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Retrieve the previous results we reported.  Used so we can avoid resending data for unchanged files. Also
        /// used so we can report which documents were removed and can have all their diagnostics cleared.
        /// </summary>
        protected abstract PreviousResult[]? GetPreviousResults(TDiagnosticsParams diagnosticsParams);

        /// <summary>
        /// Returns all the documents that should be processed in the desired order to process them in.
        /// </summary>
        protected abstract ImmutableArray<Document> GetOrderedDocuments(RequestContext context);

        /// <summary>
        /// Creates the <see cref="VSInternalDiagnosticReport"/> instance we'll report back to clients to let them know our
        /// progress.  Subclasses can fill in data specific to their needs as appropriate.
        /// </summary>
        protected abstract TReport CreateReport(TextDocumentIdentifier identifier, LSP.Diagnostic[]? diagnostics, string? resultId);

        protected abstract TReturn? CreateReturn(BufferedProgress<TReport> progress);

        /// <summary>
        /// Produce the diagnostics for the specified document.
        /// </summary>
        protected abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, Document document, Option2<DiagnosticMode> diagnosticMode, CancellationToken cancellationToken);

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
            var previousResults = GetPreviousResults(diagnosticsParams) ?? Array.Empty<PreviousResult>();
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

                var newResultId = await GetNewResultIdAsync(documentToPreviousDiagnosticParams, document, cancellationToken).ConfigureAwait(false);
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

        private static bool IncludeDocument(Document document, string? clientName)
        {
            // Documents either belong to Razor or not.  We can determine this by checking if the doc has a span-mapping
            // service or not.  If we're not in razor, we do not include razor docs.  If we are in razor, we only
            // include razor docs.
            var isRazorDoc = document.IsRazorDocument();
            var wantsRazorDoc = clientName != null;

            return wantsRazorDoc == isRazorDoc;
        }

        private static Dictionary<Document, PreviousResult> GetDocumentToPreviousDiagnosticParams(
            RequestContext context, PreviousResult[] previousResults)
        {
            Contract.ThrowIfNull(context.Solution);

            var result = new Dictionary<Document, PreviousResult>();
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
            // Being asked about this document for the first time.  Or being asked again and we have different
            // diagnostics.  Compute and report the current diagnostics info for this document.

            // Razor has a separate option for determining if they should be in push or pull mode.
            var diagnosticMode = document.IsRazorDocument()
                ? InternalDiagnosticsOptions.RazorDiagnosticMode
                : InternalDiagnosticsOptions.NormalDiagnosticMode;

            var isPull = context.GlobalOptions.IsPullDiagnostics(diagnosticMode);

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

        private void HandleRemovedDocuments(RequestContext context, PreviousResult[] previousResults, BufferedProgress<TReport> progress)
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

        /// <summary>
        /// If diagnostics have changed since the last request this calculates and returns a new
        /// non-null resultId to use for subsequent computation and caches it.
        /// </summary>
        /// <param name="documentToPreviousDiagnosticParams">the resultIds the client sent us.</param>
        /// <param name="document">the document we are currently calculating results for.</param>
        /// <returns>Null when diagnostics are unchanged, otherwise returns a non-null new resultId.</returns>
        private async Task<string?> GetNewResultIdAsync(
            Dictionary<Document, PreviousResult> documentToPreviousDiagnosticParams,
            Document document,
            CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;
            var currentProjectDependentVersion = await document.Project.GetDependentVersionAsync(cancellationToken).ConfigureAwait(false);
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (documentToPreviousDiagnosticParams.TryGetValue(document, out var previousParams) &&
                    previousParams.PreviousResultId != null &&
                    _documentIdToLastResult.TryGetValue((workspace, document.Id), out var lastResult) &&
                    lastResult.resultId == previousParams.PreviousResultId)
                {
                    if (lastResult.projectDependentVersion == currentProjectDependentVersion)
                    {
                        // The client's resultId matches our cached resultId and the project dependent version is an
                        // exact match for our current project dependent version (meaning the project and none of its dependencies
                        // have changed, or even forked, since we last calculated diagnostics).
                        // We return early here to avoid calculating checksums as we know nothing is changed.
                        return null;
                    }

                    // The current project dependent version does not match the last reported.  This may be because we've forked
                    // or reloaded a project, so fall back to calculating project checksums to determine if anything is actually changed.
                    var aggregateChecksum = await GetDependentChecksumAsync(document.Project, cancellationToken).ConfigureAwait(false);
                    if (lastResult.projectDependentChecksum == aggregateChecksum)
                    {
                        // Checksums match which means content has not changed and we do not need to re-calculate.
                        return null;
                    }
                }

                // Client didn't give us a resultId, we have nothing cached, or what we had cached didn't match the current project.
                // We need to calculate diagnostics and store what we calculated the diagnostics for.

                // Keep track of the diagnostics we reported here so that we can short-circuit producing diagnostics for
                // the same diagnostic set in the future.  Use a custom result-id per type (doc diagnostics or workspace
                // diagnostics) so that clients of one don't errantly call into the other.  For example, a client
                // getting document diagnostics should not ask for workspace diagnostics with the result-ids it got for
                // doc-diagnostics.  The two systems are different and cannot share results, or do things like report
                // what changed between each other.
                //
                // Note that we can safely update the map before computation as any cancellation or exception
                // during computation means that the client will never recieve this resultId and so cannot ask us for it.
                var newResultId = $"{GetType().Name}:{_nextDocumentResultId++}";
                var currentProjectDependentChecksum = await GetDependentChecksumAsync(document.Project, cancellationToken).ConfigureAwait(false);
                _documentIdToLastResult[(document.Project.Solution.Workspace, document.Id)] = (newResultId, currentProjectDependentVersion, currentProjectDependentChecksum);
                return newResultId;

                async Task<Checksum> GetDependentChecksumAsync(Project project, CancellationToken cancellationToken)
                {
                    var aggregateChecksum = _projectToProjectDependentChecksum.GetValue(project, static p => new AsyncLazy<Checksum>(c => CalculateDependentProjectChecksumAsync(p, c), cacheResult: true));
                    return await aggregateChecksum.GetValueAsync(cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Calculates a checksum that contains a project's checksum along with a checksum for each of the project's transitive dependencies.
        /// </summary>
        /// <remarks>
        /// This checksum calculation is used to determine if a diagnostics need to be recalculated based on the last reported checksum.
        /// The goal is to ensure that changes to
        /// <list type="bullet">
        ///    <item>Files inside the current project</item>
        ///    <item>Project properties of the current project</item>
        ///    <item>Visible files in referenced projects</item>
        ///    <item>Project properties in referenced projects</item>
        /// </list>
        /// are reflected in the metadata we keep so that comparing solutions accurately tells us when we need to recompute diagnostics.   
        /// 
        /// <para>This method of checking for changes has a few important properties that differentiate it from other methods of determining project version.
        /// <list type="bullet">
        ///    <item>Changes to methods inside the current project will be reflected to compute updated diagnostics.
        ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> does not change as it only returns top level changes.</item>
        ///    <item>Reloading a project without making any changes will re-use cached diagnostics.
        ///        <see cref="Project.GetDependentSemanticVersionAsync(CancellationToken)"/> changes as the project is removed, then added resulting in a version change.</item>
        /// </list>   
        /// Since diagnostic calculations happen OOP, these checksums already have been (or will be) created to do the diagnostics calculation anyway.
        /// </para>
        /// </remarks>
        private static async Task<Checksum> CalculateDependentProjectChecksumAsync(Project project, CancellationToken cancellationToken)
        {
            using var tempChecksumArray = TemporaryArray<Checksum>.Empty;

            // Get the checksum for the project itself.
            var projectChecksum = await project.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
            tempChecksumArray.Add(projectChecksum);

            // Calculate a checksum this project and for each dependent project that could affect diagnostics for this project.
            // Ensure that the checksum calculation orders the projects consistently so that order changes (like unload / reload) don't change checksums.
            var transitiveDependencies = project.Solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(project.Id);
            var orderedProjectIds = transitiveDependencies.Add(project.Id).OrderBy(p => p.Id);
            foreach (var projectId in orderedProjectIds)
            {
                var referencedProject = project.Solution.GetRequiredProject(projectId);

                // Note that these checksums should only actually be calculated once, if the project is unchanged
                // the same checksum will be returned.
                var referencedProjectChecksum = await referencedProject.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);
                tempChecksumArray.Add(referencedProjectChecksum);
            }

            return Checksum.Create(tempChecksumArray.ToImmutableAndClear());
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
                var diagnostic = CreateBaseLspDiagnostic<LSP.Diagnostic>();
                return diagnostic;
            }
            else
            {
                var vsDiagnostic = CreateBaseLspDiagnostic<VSDiagnostic>();
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

            T CreateBaseLspDiagnostic<T>() where T : LSP.Diagnostic, new()
            {
                return new T()
                {
                    Source = "Roslyn",
                    Code = diagnosticData.Id,
                    CodeDescription = ProtocolConversions.HelpLinkToCodeDescription(diagnosticData.HelpLink),
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
