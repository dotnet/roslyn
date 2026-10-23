// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Manages the registered workspaces and corresponding LSP solutions for an LSP server.
/// This type is tied to a particular server.
/// </summary>
/// <remarks>
/// This type provides an LSP view of the registered workspace solutions so that all LSP requests operate
/// on the state of the world that matches the LSP requests we've received.  
/// 
/// This is done by storing the LSP text as provided by client didOpen/didClose/didChange requests.  When asked for a document we provide either
/// <list type="bullet">
///     <item> The exact workspace solution instance if all the LSP text matches what is currently in the workspace.</item>
///     <item> A fork from the workspace current solution with the LSP text applied if the LSP text does not match.  This can happen since
///     LSP text sync is asynchronous and not guaranteed to match the text in the workspace (though the majority of the time in VS it does).</item>
/// </list>
/// 
/// Doing the forking like this has a few nice properties.
/// <list type="bullet">
///   <item>99% of the time the VS workspace matches the LSP text.  In those cases we do 0 re-parsing, share compilations, versions, checksum calcs, etc.</item>
///   <item>In the 1% of the time that we do not match, we can simply and easily compute a fork.</item>
///   <item>The code is relatively straightforward</item>
/// </list>
/// </remarks>
internal sealed class LspWorkspaceManager : IDocumentChangeTracker, ILspService
{
    /// <summary>
    /// A cache from workspace to the last solution we returned for LSP.
    /// <para/> The <see cref="CachedLspSolution.ForkedFromVersion"/> is not null when the solution was created from a fork of the workspace with LSP
    /// text applied on top. It is null when LSP reuses the workspace solution (the LSP text matches the contents of the
    /// workspace).
    /// <para/> Access to this is guaranteed to be serial by the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    private readonly Dictionary<Workspace, CachedLspSolution> _cachedLspSolutions = [];

    /// <summary>
    /// Stores the current source text for each URI that is being tracked by LSP. Each time an LSP text sync
    /// notification comes in, this source text is updated to match. Used as the backing implementation for the <see
    /// cref="IDocumentChangeTracker"/>.
    /// <para/> Note that the text here is tracked regardless of whether or not we found a matching roslyn document for
    /// the URI.
    /// <para/> Access to this is guaranteed to be serial by the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    private ImmutableDictionary<DocumentUri, TrackedDocumentInfo> _trackedDocuments = ImmutableDictionary<DocumentUri, TrackedDocumentInfo>.Empty;

    private readonly ILspLogger _logger;
    private readonly ILspMiscellaneousFilesWorkspaceProvider? _lspMiscellaneousFilesWorkspaceProvider;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly ILanguageInfoProvider _languageInfoProvider;
    private readonly RequestTelemetryLogger _requestTelemetryLogger;
    private readonly IOnDemandProjectLoader? _onDemandProjectLoader;

    public LspWorkspaceManager(
        ILspLogger logger,
        ILspMiscellaneousFilesWorkspaceProvider? lspMiscellaneousFilesWorkspace,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        ILanguageInfoProvider languageInfoProvider,
        RequestTelemetryLogger requestTelemetryLogger,
        IOnDemandProjectLoader? onDemandProjectLoader)
    {
        _lspMiscellaneousFilesWorkspaceProvider = lspMiscellaneousFilesWorkspace;
        _logger = logger;
        _requestTelemetryLogger = requestTelemetryLogger;
        _onDemandProjectLoader = onDemandProjectLoader;

        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        _languageInfoProvider = languageInfoProvider;
    }

    public EventHandler<EventArgs>? LspTextChanged;

    #region Implementation of IDocumentChangeTracker

    private static async ValueTask ApplyChangeToMutatingWorkspaceAsync(Workspace workspace, DocumentUri uri, Func<ILspWorkspace, DocumentId, ValueTask> change)
    {
        if (workspace is not ILspWorkspace { SupportsMutation: true } mutatingWorkspace)
            return;

        foreach (var documentId in workspace.CurrentSolution.GetDocumentIds(uri))
            await change(mutatingWorkspace, documentId).ConfigureAwait(false);
    }

    /// <summary>
    /// Called by the <see cref="DidOpenHandler"/> when a document is opened in LSP.
    /// 
    /// <see cref="DidOpenHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    public async ValueTask StartTrackingAsync(DocumentUri uri, SourceText documentText, string languageId, int lspVersion, CancellationToken cancellationToken)
    {
        // First, store the LSP view of the text as the uri is now owned by the LSP client.
        Contract.ThrowIfTrue(_trackedDocuments.ContainsKey(uri), $"didOpen received for {uri} which is already open.");

        if (uri.ParsedDocumentUri is null)
        {
            _logger.LogError($"Unable to parse URI {uri}");
        }

        _trackedDocuments = _trackedDocuments.Add(uri, new(documentText, languageId, lspVersion));

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();

        LspTextChanged?.Invoke(this, EventArgs.Empty);

        // Attempt to open the doc if we find it in a workspace.  Note: if we don't (because we've heard from lsp about
        // the doc before we've heard from the project system), that's ok.  We'll still attempt to open it later in
        // SynchronizeWorkspaceAndGetLspSolutionAsync
        await TryOpenDocumentsInMutatingWorkspaceAsync(uri).ConfigureAwait(false);

        return;

        async ValueTask TryOpenDocumentsInMutatingWorkspaceAsync(DocumentUri uri)
        {
            var registeredWorkspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
            foreach (var workspace in registeredWorkspaces)
            {
                await ApplyChangeToMutatingWorkspaceAsync(workspace, uri, (_, documentId) =>
                    workspace.TryOnDocumentOpenedAsync(documentId, documentText.Container, isCurrentContext: false, cancellationToken)).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Called by the <see cref="DidCloseHandler"/> when a document is closed in LSP.
    /// 
    /// <see cref="DidCloseHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    public async ValueTask StopTrackingAsync(DocumentUri uri, CancellationToken cancellationToken)
    {
        // First, stop tracking this URI and source text as it is no longer owned by LSP.
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(uri), $"didClose received for {uri} which is not open.");
        _trackedDocuments = _trackedDocuments.Remove(uri);

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();

        // Also remove it from our loose files if it is still there.
        if (_lspMiscellaneousFilesWorkspaceProvider is not null)
        {
            try
            {
                await _lspMiscellaneousFilesWorkspaceProvider.CloseDocumentAsync(uri).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                this._logger.LogException(ex);
            }
        }

        LspTextChanged?.Invoke(this, EventArgs.Empty);

        // Attempt to close the doc, if it is currently open in a workspace.
        await TryCloseDocumentsInMutatingWorkspaceAsync(uri).ConfigureAwait(false);

        return;

        async ValueTask TryCloseDocumentsInMutatingWorkspaceAsync(DocumentUri uri)
        {
            var registeredWorkspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
            foreach (var workspace in registeredWorkspaces)
            {
                await ApplyChangeToMutatingWorkspaceAsync(workspace, uri, async (_, documentId) =>
                {
                    if (documentId.IsSourceGenerated)
                    {
                        // Source generated documents cannot go through OnDocumentOpened/Closed.
                        // There is a separate OnSourceGeneratedDocumentOpened/Closed method, but there is no need
                        // for us to call it in LSP - it deals with mapping TextBuffers to text containers.
                        return;
                    }
                    await workspace.TryOnDocumentClosedAsync(documentId, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Called by the <see cref="DidChangeHandler"/> when a document's text is updated in LSP.
    /// 
    /// <see cref="DidChangeHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    public void UpdateTrackedDocument(DocumentUri uri, SourceText newSourceText, int lspVersion)
    {
        // Store the updated LSP view of the source text.
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(uri), $"didChange received for {uri} which is not open.");
        var (_, language, _) = _trackedDocuments[uri];
        _trackedDocuments = _trackedDocuments.SetItem(uri, new(newSourceText, language, lspVersion));

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();

        LspTextChanged?.Invoke(this, EventArgs.Empty);
    }

    public ImmutableDictionary<DocumentUri, TrackedDocumentInfo> GetTrackedDocuments() => _trackedDocuments;

    #endregion

    #region LSP Solution Retrieval

    /// <summary>
    /// Captures request-time document state during serialized dispatch. On a lookup miss, captures the miscellaneous
    /// fallback and optionally starts project loading. Resolve the captured context outside dispatch.
    /// </summary>
    internal async ValueTask<CapturedLspWorkspaceContext?> CaptureLspDocumentContextAsync(
        TextDocumentIdentifier textDocumentIdentifier,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        bool allowProjectLoading,
        CancellationToken cancellationToken)
    {
        var uri = textDocumentIdentifier.DocumentUri;
        var documentContext = await FindDocumentAsync(textDocumentIdentifier, trackedDocuments, cancellationToken).ConfigureAwait(false);
        if (documentContext is not { } initialContext)
        {
            RecordDocumentMissingResult(uri);
            return null;
        }

        var canLoadOnDemand = allowProjectLoading && _onDemandProjectLoader?.CanLoad(uri) == true;

        if (initialContext.Workspace.Kind == WorkspaceKind.Host || !canLoadOnDemand)
            return new(RecordDocumentFoundResult(initialContext));

        var projectLoadTask = _onDemandProjectLoader!.TryLoadProjectsAsync(uri);

        return new(async () =>
        {
            var finalContext = await FindDocumentAfterProjectLoadAsync(
                projectLoadTask, textDocumentIdentifier, trackedDocuments, cancellationToken).ConfigureAwait(false);

            return RecordDocumentFoundResult(finalContext ?? initialContext);
        });

        async Task<(Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked)?> FindDocumentAsync(
            TextDocumentIdentifier textDocumentIdentifier,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            CancellationToken cancellationToken)
        {
            var workspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
            ImmutableArray<Workspace> sortedWorkspaces =
            [
                .. workspaces.Where(workspace => workspace.Kind != WorkspaceKind.MiscellaneousFiles),
                .. workspaces.Where(workspace => workspace.Kind == WorkspaceKind.MiscellaneousFiles),
            ];

            // Search through all workspaces in two passes: first for regular workspaces, then for miscellaneous files workspaces.

            foreach (var workspace in sortedWorkspaces)
            {
                var (solution, isForked) = await GetSynchronizedSolutionAsync(workspace, trackedDocuments, cancellationToken).ConfigureAwait(false);
                var document = await solution.GetTextDocumentAsync(textDocumentIdentifier, cancellationToken).ConfigureAwait(false);
                if (document is not null)
                {
                    if (workspace.Kind != WorkspaceKind.MiscellaneousFiles && _lspMiscellaneousFilesWorkspaceProvider is not null)
                    {
                        // Found the document in a non-miscellaneous files workspace.
                        // Unload it from the miscellaneous files workspace.
                        await _lspMiscellaneousFilesWorkspaceProvider.TryRemoveMiscellaneousDocumentAsync(uri).ConfigureAwait(false);
                    }

                    return (workspace, document.Project.Solution, document, isForked);
                }
            }

            return await TryGetOrCreateMiscellaneousDocumentAsync(textDocumentIdentifier.DocumentUri, trackedDocuments).ConfigureAwait(false);
        }

        async Task<(Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked)?> TryGetOrCreateMiscellaneousDocumentAsync(
            DocumentUri uri,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments)
        {
            if (_lspMiscellaneousFilesWorkspaceProvider is null)
                return null;

            // The provider may add tracked documents to a workspace or return an untracked URI in a transient solution.
            trackedDocuments.TryGetValue(uri, out var trackedDocument);

            try
            {
                var document = await _lspMiscellaneousFilesWorkspaceProvider.AddDocumentAsync(uri, trackedDocument).ConfigureAwait(false);
                if (document is not null)
                {
                    // Since the document may have been added to the HostWorkspace by the FileBasedProgramsProjectSystem,
                    // we need to apply tracked text to the solution.
                    var (solution, isForked) = await GetSolutionWithTrackedTextAsync(
                        document.Project.Solution, trackedDocuments, cachedFork: null, cancellationToken).ConfigureAwait(false);
                    document = await solution.GetRequiredTextDocumentAsync(document.Id, cancellationToken).ConfigureAwait(false);
                    return (solution.Workspace, solution, document, isForked);
                }
            }
            catch (Exception exception) when (FatalError.ReportAndCatchUnlessCanceled(exception))
            {
                _logger.LogException(exception);
            }

            return null;
        }

        async Task<(Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked)?> FindDocumentAfterProjectLoadAsync(
            Task projectLoadTask,
            TextDocumentIdentifier textDocumentIdentifier,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            CancellationToken cancellationToken)
        {
            await projectLoadTask.WithCancellation(cancellationToken).ConfigureAwait(false);

            var workspace = _lspWorkspaceRegistrationService.GetAllRegistrations()
                .FirstOrDefault(static workspace => workspace.Kind == WorkspaceKind.Host);
            if (workspace is null)
                return null;

            var loadedSolution = workspace.CurrentSolution;

            var (solution, isForked) = await GetSolutionWithTrackedTextAsync(
                loadedSolution, trackedDocuments, cachedFork: null, cancellationToken).ConfigureAwait(false);
            var document = await solution.GetTextDocumentAsync(textDocumentIdentifier, cancellationToken).ConfigureAwait(false);
            if (document is null)
                return null;

            return (workspace, document.Project.Solution, document, isForked);
        }
    }

    private LspWorkspaceContext RecordDocumentFoundResult((Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked) documentContext)
    {
        // Record metadata on how we got this document.
        var workspaceKind = documentContext.Solution.WorkspaceKind;
        _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspaceKind);
        _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(documentContext.IsForked);
        _logger.LogDebug($"{documentContext.Document.FilePath} found in workspace {workspaceKind}; project {documentContext.Document.Project.Name}");

        return new(documentContext.Workspace, documentContext.Solution, documentContext.Document);
    }

    private void RecordDocumentMissingResult(DocumentUri uri)
    {
        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        // Depending on the host, this can be entirely normal (for example, opening a loose file).
        var searchedWorkspaceKinds = string.Join(
            ";", _lspWorkspaceRegistrationService.GetAllRegistrations().SelectAsArray(static workspace => workspace.Kind));
        _logger.LogDebug($"Could not find '{uri}'.  Searched {searchedWorkspaceKinds}");
        _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);
    }

    /// <summary>
    /// Captures request-time state for the registered workspace with workspace kind <see cref="WorkspaceKind.Host"/>.
    /// The captured context is used for LSP requests that pertain to the entire workspace, for example code search
    /// or workspace diagnostics.
    /// </summary>
    /// <remarks>
    /// Wait for deferred resolution outside dispatch to project the captured text onto the loaded solution; 
    /// completed results can be consumed immediately.
    /// </remarks>
    internal async ValueTask<CapturedLspWorkspaceContext?> CaptureLspWorkspaceContextAsync(
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        bool allowProjectLoading,
        CancellationToken cancellationToken)
    {
        var hostWorkspace = _lspWorkspaceRegistrationService.GetAllRegistrations()
            .FirstOrDefault(static workspace => workspace.Kind == WorkspaceKind.Host);
        if (hostWorkspace is null)
            return null;

        var (solution, isForked) = await GetSynchronizedSolutionAsync(hostWorkspace, trackedDocuments, cancellationToken).ConfigureAwait(false);
        var initialContext = new LspWorkspaceContext(hostWorkspace, solution, Document: null);

        if (!allowProjectLoading
            || _onDemandProjectLoader?.WaitForActiveLoadsAsync() is not { } activeLoads
            || activeLoads.Status == TaskStatus.RanToCompletion)
        {
            _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);
            return new(initialContext);
        }

        return new(() => GetSolutionAfterActiveLoadsAsync(initialContext, trackedDocuments, activeLoads, cancellationToken));

        async Task<LspWorkspaceContext> GetSolutionAfterActiveLoadsAsync(
            LspWorkspaceContext initialContext,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            Task activeLoads,
            CancellationToken cancellationToken)
        {
            var hostWorkspace = initialContext.Workspace;

            await activeLoads.WithCancellation(cancellationToken).ConfigureAwait(false);

            var (solution, isForked) = await GetSolutionWithTrackedTextAsync(
                hostWorkspace.CurrentSolution, trackedDocuments, cachedFork: null, cancellationToken).ConfigureAwait(false);
            _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);

            return new(hostWorkspace, solution, Document: null);
        }
    }

    private async Task<(Solution Solution, bool IsForked)> GetSynchronizedSolutionAsync(
        Workspace workspace,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        CancellationToken cancellationToken)
    {
        // At a high level these are the steps we take to compute the desired LSP solution:
        //
        //  1. Check whether the workspace's current solution is the same solution we last verified matched the LSP
        //     text. If so, nothing has changed and we can return it without comparing document text. Importantly,
        //     this only returns an unforked solution; a cached fork must still be validated against current LSP text.
        //
        //  2. Push tracked changes into the underlying workspace if it is a mutating workspace, bringing that
        //     workspace into sync with everything received from LSP.
        //
        //  3. Compare the captured LSP text with the workspace text. If everything matches, return the workspace
        //     solution. Computing checksums is a reasonable cost here; for example, VS already does this for OOP calls.
        //
        //  4. If the text does not match, reuse a cached fork for this workspace version and source-generator state
        //     when one exists. This avoids recreating trees, source-generated documents, and other forked state.
        //
        //  5. Otherwise, create a new fork from the current workspace solution with the captured LSP text applied.
        //
        // Steps 3 through 5 are performed by GetSolutionWithTrackedTextAsync so deferred request resolution can use
        // the same pure snapshot projection without mutating the workspace or updating this cache. IsForked is propagated
        // so telemetry is reported only when the resulting solution is actually requested.
        var workspaceCurrentSolution = workspace.CurrentSolution;

        // Step 1: Check if nothing has changed and we already verified that the workspace text matches our LSP text.
        var hasCachedSolution = _cachedLspSolutions.TryGetValue(workspace, out var cachedSolution);
        if (hasCachedSolution && cachedSolution.Solution == workspaceCurrentSolution)
            return (workspaceCurrentSolution, IsForked: false);

        // Step 2: Push through any changes to the underlying workspace if it's a mutating workspace.
        await TryOpenAndEditDocumentsInMutatingWorkspaceAsync(workspace, trackedDocuments, cancellationToken).ConfigureAwait(false);

        // Because the workspace may have been mutated, go back and retrieve its current snapshot so we're operating
        // against that view.
        workspaceCurrentSolution = workspace.CurrentSolution;

        var cachedFork = hasCachedSolution ? cachedSolution.TryGetReusableFork(workspaceCurrentSolution) : null;

        var result = await GetSolutionWithTrackedTextAsync(workspaceCurrentSolution, trackedDocuments, cachedFork, cancellationToken).ConfigureAwait(false);

        if (result.IsForked)
            _cachedLspSolutions[workspace] = new(workspaceCurrentSolution.SolutionStateContentVersion, workspaceCurrentSolution.CompilationState.SourceGeneratorExecutionVersionMap.GetChecksum(), result.Solution);
        else
            _cachedLspSolutions[workspace] = new(null, null, result.Solution);

        return result;

        async ValueTask TryOpenAndEditDocumentsInMutatingWorkspaceAsync(Workspace workspace, ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments, CancellationToken cancellationToken)
        {
            foreach (var (uri, (sourceText, _, _)) in trackedDocuments)
            {
                await ApplyChangeToMutatingWorkspaceAsync(workspace, uri, async (mutatingWorkspace, documentId) =>
                {
                    if (documentId.IsSourceGenerated)
                    {
                        // Source generated documents cannot go through OnDocumentOpened/Closed.
                        // There is a separate OnSourceGeneratedDocumentOpened/Closed method, but there is no need
                        // for us to call it in LSP - it deals with mapping TextBuffers to text containers.
                        return;
                    }
                    // This may be the first time this workspace is hearing that this document is open from LSP's
                    // perspective. Attempt to open it there.
                    //
                    // TODO(cyrusn): Do we need to pass a correct value for isCurrentContext?  Or will that fall out from
                    // something else in lsp.
                    await workspace.TryOnDocumentOpenedAsync(
                        documentId, sourceText.Container, isCurrentContext: false, cancellationToken).ConfigureAwait(false);

                    // Note: there is a race here in that we might see/change/return here based on the
                    // relationship of 'sourceText' and 'currentSolution' while some other entity outside of the
                    // confines of lsp queue might update the workspace externally.  That's completely fine
                    // though.  The caller will always grab the 'current solution' again off of the workspace
                    // and check the checksums of all documents against the ones this workspace manager is
                    // tracking.  If there are any differences, it will fork and use that fork.
                    await mutatingWorkspace.UpdateTextIfPresentAsync(documentId, sourceText, cancellationToken).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }
    }

    private async Task<(Solution Solution, bool IsForked)> GetSolutionWithTrackedTextAsync(
        Solution workspaceCurrentSolution,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        Solution? cachedFork,
        CancellationToken cancellationToken)
    {
        // Step 3: Check to see if the LSP text matches the workspace text.
        var documentsInWorkspace = GetDocumentsForUris([.. trackedDocuments.Keys], workspaceCurrentSolution);
        using var _ = ArrayBuilder<(SourceGeneratedDocumentIdentity Identity, DateTime Generated, SourceText Text)>.GetInstance(out var sourceGeneratedDocumentsBuilder);
        foreach (var (uri, trackedDocument) in trackedDocuments)
        {
            if (uri.IsSourceGeneratedUri() &&
                SourceGeneratedDocumentUri.DeserializeIdentity(workspaceCurrentSolution, uri.GetRequiredParsedUri()) is { } identity)
            {
                sourceGeneratedDocumentsBuilder.Add((identity, DateTime.Now, trackedDocument.SourceText));
            }
        }

        var sourceGeneratedDocuments = sourceGeneratedDocumentsBuilder.ToImmutableAndClear();

        // First we check if normal document text matches the workspace solution.
        // This does not look at source generated documents.
        var doesAllTextMatch = await DoesAllDocumentTextMatchWorkspaceSolutionAsync(documentsInWorkspace, trackedDocuments, _logger, cancellationToken).ConfigureAwait(false);

        // Then we check if source generated document text matches the workspace solution.
        // This is intentionally done differently from normal documents because the normal method will cause
        // source generators to run which we do not want to do in queue dispatch.
        var doesAllSourceGeneratedTextMatch = DoesAllSourceGeneratedTextMatchWorkspaceSolution(sourceGeneratedDocuments, workspaceCurrentSolution);
        if (doesAllTextMatch && doesAllSourceGeneratedTextMatch)
            return (workspaceCurrentSolution, IsForked: false);

        // Step 4: See if we can reuse a previously forked solution.
        if (cachedFork is not null)
            return (cachedFork, IsForked: true);

        // Step 5: Fork a new solution from the workspace with the LSP text applied.
        var lspSolution = workspaceCurrentSolution;
        // If the workspace text matched we can leave the normal documents as-is
        if (!doesAllTextMatch)
        {
            foreach (var (uri, workspaceDocuments) in documentsInWorkspace)
                lspSolution = lspSolution.WithDocumentText(workspaceDocuments.Select(d => d.Id), trackedDocuments[uri].SourceText);
        }

        // If the source generated documents matched we can leave the source generated documents as-is
        if (!doesAllSourceGeneratedTextMatch)
            lspSolution = lspSolution.WithFrozenSourceGeneratedDocuments(sourceGeneratedDocuments);

        return (lspSolution, IsForked: true);

        // Using the workspace's current solutions, find the matching documents in for each URI.
        static ImmutableDictionary<DocumentUri, ImmutableArray<TextDocument>> GetDocumentsForUris(ImmutableArray<DocumentUri> trackedDocuments, Solution workspaceCurrentSolution)
        {
            using var _ = PooledDictionary<DocumentUri, ImmutableArray<TextDocument>>.GetInstance(out var documentsInSolution);
            foreach (var trackedDoc in trackedDocuments)
            {
                var documents = workspaceCurrentSolution.GetTextDocuments(trackedDoc);
                if (!documents.IsEmpty)
                {
                    documentsInSolution[trackedDoc] = documents;
                }
            }

            return documentsInSolution.ToImmutableDictionary();
        }

        // Checks if the open source generator document contents matches the contents of the workspace solution.
        // This looks at the source generator state explicitly to avoid actually running source generators
        static bool DoesAllSourceGeneratedTextMatchWorkspaceSolution(
            ImmutableArray<(SourceGeneratedDocumentIdentity Identity, DateTime Generated, SourceText Text)> sourceGeneratedDocuments,
            Solution workspaceSolution)
        {
            var compilationState = workspaceSolution.CompilationState;
            foreach (var (identity, _, text) in sourceGeneratedDocuments)
            {
                var existingState = compilationState.TryGetSourceGeneratedDocumentStateForAlreadyGeneratedId(identity.DocumentId);
                if (existingState is null)
                {
                    // We don't have existing state for at least one of the documents, so the text cannot match.
                    return false;
                }

                var newState = existingState.WithText(text);
                if (newState != existingState)
                {
                    return false;
                }
            }

            return true;
        }

        // Given a set of documents from the workspace current solution, verify that the LSP text is the same as the document contents.
        static async Task<bool> DoesAllDocumentTextMatchWorkspaceSolutionAsync(
            ImmutableDictionary<DocumentUri, ImmutableArray<TextDocument>> documentsInWorkspace,
            ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
            ILspLogger logger,
            CancellationToken cancellationToken)
        {
            foreach (var (uriInWorkspace, documentsForUri) in documentsInWorkspace)
            {
                var lspText = trackedDocuments[uriInWorkspace].SourceText;
                foreach (var document in documentsForUri)
                {
                    // Linked documents can temporarily have different text when only part of the linked set has been updated.
                    var isTextEquivalent = await DoesDocumentTextMatchAsync(document, lspText, cancellationToken).ConfigureAwait(false);
                    if (!isTextEquivalent)
                    {
                        logger.LogWarning($"Text for {uriInWorkspace} did not match document text {document.Id} in workspace's {document.Project.Solution.WorkspaceKind} current solution");
                        return false;
                    }
                }
            }

            return true;
        }

        static async ValueTask<bool> DoesDocumentTextMatchAsync(TextDocument document, SourceText lspText, CancellationToken cancellationToken)
        {
            var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            if (documentText == lspText)
                return true;

            return lspText.GetContentHash().AsSpan().SequenceEqual(documentText.GetContentHash().AsSpan());
        }
    }

    #endregion

    /// <summary>
    /// Returns a Roslyn language name for the given URI.
    /// </summary>
    internal bool TryGetLanguageForUri(DocumentUri uri, [NotNullWhen(true)] out string? language)
    {
        string? languageId = null;
        if (_trackedDocuments.TryGetValue(uri, out var trackedDocument))
        {
            languageId = trackedDocument.LanguageId;
        }

        if (_languageInfoProvider.TryGetLanguageInformation(uri, languageId, out var languageInfo))
        {
            language = languageInfo.LanguageName;
            return true;
        }

        language = null;
        return false;
    }

    private readonly record struct CachedLspSolution(
        int? ForkedFromVersion,
        Checksum? SourceGeneratorChecksum,
        Solution Solution)
    {
        public Solution? TryGetReusableFork(Solution workspaceSolution)
            => ForkedFromVersion == workspaceSolution.SolutionStateContentVersion &&
               SourceGeneratorChecksum == workspaceSolution.CompilationState.SourceGeneratorExecutionVersionMap.GetChecksum()
                ? Solution
                : null;
    }

    internal TestAccessor GetTestAccessor()
            => new(this);

    internal readonly struct TestAccessor
    {
        private readonly LspWorkspaceManager _manager;

        public TestAccessor(LspWorkspaceManager manager)
            => _manager = manager;

        public ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument document)
        {
            return ValueTask.FromResult(document.Project.Solution.WorkspaceKind == WorkspaceKind.MiscellaneousFiles);
        }

        public async IAsyncEnumerable<T> GetMiscellaneousDocumentsAsync<T>(Func<Project, IEnumerable<T>> documentSelector) where T : TextDocument
        {
            foreach (var workspace in _manager._lspWorkspaceRegistrationService.GetAllRegistrations())
            {
                foreach (var document in workspace.CurrentSolution.Projects.SelectMany(documentSelector))
                {
                    if (await IsMiscellaneousFilesDocumentAsync(document).ConfigureAwait(false) && !document.FilePath?.Contains("roslyn-canonical-misc") == true)
                        yield return document;
                }
            }
        }

        public bool IsWorkspaceRegistered(Workspace workspace)
        {
            return _manager._lspWorkspaceRegistrationService.GetAllRegistrations().Contains(workspace);
        }
    }
}
