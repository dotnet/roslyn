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
    /// <para/> The forkedFromVersion is not null when the solution was created from a fork of the workspace with LSP
    /// text applied on top. It is null when LSP reuses the workspace solution (the LSP text matches the contents of the
    /// workspace).
    /// <para/> Access to this is guaranteed to be serial by the <see cref="RequestExecutionQueue{RequestContextType}"/>
    /// </summary>
    private readonly Dictionary<Workspace, (int? forkedFromVersion, Checksum? sourceGeneratorChecksum, Solution solution)> _cachedLspSolutions = [];

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

    public LspWorkspaceManager(
        ILspLogger logger,
        ILspMiscellaneousFilesWorkspaceProvider? lspMiscellaneousFilesWorkspace,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        ILanguageInfoProvider languageInfoProvider,
        RequestTelemetryLogger requestTelemetryLogger)
    {
        _lspMiscellaneousFilesWorkspaceProvider = lspMiscellaneousFilesWorkspace;
        _logger = logger;
        _requestTelemetryLogger = requestTelemetryLogger;

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
        // GetLspSolutionForWorkspaceAsync
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

    public ImmutableDictionary<DocumentUri, TrackedDocumentInfo> GetTrackedLspText() => _trackedDocuments;

    #endregion

    #region LSP Solution Retrieval

    internal async Task<LspContext> CreateResolvedLspContextAsync(
        LspContext initialValue,
        Solution? initialWorkspaceSolution,
        TextDocumentIdentifier? textDocumentIdentifier,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        Task projectLoadTask,
        CancellationToken cancellationToken)
    {
        // Ensure that post-load resolution does not run in the serialized request queue when loading has already completed.
        await Task.Yield();

        try
        {
            await projectLoadTask.WithCancellation(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return initialValue;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (initialValue.Workspace.Kind == WorkspaceKind.Host &&
            ReferenceEquals(initialValue.Workspace.CurrentSolution, initialWorkspaceSolution))
        {
            return initialValue;
        }

        var solutions = ImmutableArray.CreateBuilder<(Workspace workspace, Solution Solution, bool IsForked)>();
        foreach (var workspace in GetRegisteredWorkspacesInSearchOrder())
        {
            var (solution, isForked) = await ApplyLspTextAsync(
                workspace.CurrentSolution, trackedDocuments, cachedFork: null, cancellationToken).ConfigureAwait(false);
            solutions.Add((workspace, solution, isForked));
        }

        if (textDocumentIdentifier is not null)
        {
            var documentContext = RecordDocumentLookupResult(
                textDocumentIdentifier.DocumentUri,
                await FindDocumentInSolutionsAsync(textDocumentIdentifier, solutions.ToImmutable(), cancellationToken).ConfigureAwait(false));
            if (documentContext is { Workspace: not null, Solution: not null })
                return new(documentContext.Workspace, documentContext.Solution, documentContext.Document);

            if (initialValue.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
                return initialValue;
        }

        var hostContext = solutions.FirstOrDefault(static context => context.workspace.Kind == WorkspaceKind.Host);
        if (hostContext.workspace is null)
            return initialValue;

        _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(hostContext.IsForked);
        return new(hostContext.workspace, hostContext.Solution, Document: null);
    }

    internal Solution? GetHostWorkspaceCurrentSolution()
        => _lspWorkspaceRegistrationService.GetAllRegistrations()
            .FirstOrDefault(static workspace => workspace.Kind == WorkspaceKind.Host)?.CurrentSolution;

    /// <summary>
    /// Returns the LSP solution associated with the workspace with workspace kind <see cref="WorkspaceKind.Host"/>.
    /// This is the solution used for LSP requests that pertain to the entire workspace, for example code search or
    /// workspace diagnostics.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue{RequestContextType}"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<(Workspace? Workspace, Solution? Solution)> GetLspSolutionInfoAsync(CancellationToken cancellationToken)
    {
        var hostWorkspace = GetHostWorkspaceCurrentSolution()?.Workspace;
        if (hostWorkspace is null)
            return default;

        var (solution, isForked) = await GetLspSolutionForWorkspaceAsync(
            hostWorkspace, cancellationToken).ConfigureAwait(false);
        _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);

        return (hostWorkspace, solution);
    }

    /// <summary>
    /// Returns the LSP solution associated with the workspace with kind <see cref="WorkspaceKind.Host"/>. This is the
    /// solution used for LSP requests that pertain to the entire workspace, for example code search or workspace
    /// diagnostics.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue{RequestContextType}"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<(Workspace? Workspace, Solution? Solution, TextDocument? Document)> GetLspDocumentInfoAsync(
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken)
    {
        var uri = textDocumentIdentifier.DocumentUri;
        var documentContext = await FindDocumentInSolutionsAsync(
            textDocumentIdentifier, await GetLspSolutionsAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        if (documentContext is null && _lspMiscellaneousFilesWorkspaceProvider is not null)
        {
            // Ask the loose files provider for the document. The provider may add tracked documents to a workspace
            // or return an untracked file URI in a transient solution.
            TrackedDocumentInfo? trackedDocument = _trackedDocuments.TryGetValue(uri, out var documentInfo)
                ? documentInfo
                : null;

            try
            {
                var document = await _lspMiscellaneousFilesWorkspaceProvider.AddDocumentAsync(uri, trackedDocument).ConfigureAwait(false);

                if (document is not null)
                    documentContext = (document.Project.Solution.Workspace, document.Project.Solution, document, IsForked: false);
            }
            catch (Exception exception) when (FatalError.ReportAndCatchUnlessCanceled(exception))
            {
                _logger.LogException(exception);
            }
        }

        if (documentContext is { } result)
        {
            if (result.Workspace.Kind != WorkspaceKind.MiscellaneousFiles &&
                _lspMiscellaneousFilesWorkspaceProvider is not null)
            {
                // Found the document in a non-miscellaneous files workspace. Unload it from the miscellaneous files workspace.
                try
                {
                    await _lspMiscellaneousFilesWorkspaceProvider.TryRemoveMiscellaneousDocumentAsync(uri).ConfigureAwait(false);
                }
                catch (Exception exception) when (FatalError.ReportAndCatchUnlessCanceled(exception))
                {
                    _logger.LogException(exception);
                }
            }
        }

        return RecordDocumentLookupResult(uri, documentContext);
    }

    private static async Task<(Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked)?> FindDocumentInSolutionsAsync(
        TextDocumentIdentifier textDocumentIdentifier,
        ImmutableArray<(Workspace workspace, Solution Solution, bool IsForked)> lspSolutions,
        CancellationToken cancellationToken)
    {
        var uri = textDocumentIdentifier.DocumentUri;
        foreach (var (workspace, lspSolution, isForked) in lspSolutions)
        {
            var documents = await lspSolution.GetTextDocumentsAsync(uri, cancellationToken).ConfigureAwait(false);
            if (documents.IsEmpty)
                continue;

            // We have at least one document, so find the one in the right project context.
            var document = documents.FindDocumentInProjectContext(
                textDocumentIdentifier, (solution, documentId) => solution.GetRequiredTextDocument(documentId));
            return (workspace, document.Project.Solution, document, isForked);
        }

        return null;
    }

    private (Workspace? Workspace, Solution? Solution, TextDocument? Document) RecordDocumentLookupResult(
        DocumentUri uri,
        (Workspace Workspace, Solution Solution, TextDocument Document, bool IsForked)? documentContext)
    {
        if (documentContext is { } result)
        {
            // Record metadata on how we got this document.
            var workspaceKind = result.Solution.WorkspaceKind;
            _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspaceKind);
            _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(result.IsForked);
            _logger.LogDebug($"{result.Document.FilePath} found in workspace {workspaceKind}; project {result.Document.Project.Name}");

            return (result.Workspace, result.Solution, result.Document);
        }

        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        // Depending on the host, this can be entirely normal (for example, opening a loose file).
        var searchedWorkspaceKinds = string.Join(
            ";", _lspWorkspaceRegistrationService.GetAllRegistrations().SelectAsArray(static workspace => workspace.Kind));
        _logger.LogDebug($"Could not find '{uri}'.  Searched {searchedWorkspaceKinds}");
        _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);

        return default;
    }

    /// <summary>
    /// Gets the LSP view of all the registered workspaces' current solutions.
    /// </summary>
    private async Task<ImmutableArray<(Workspace workspace, Solution Solution, bool IsForked)>> GetLspSolutionsAsync(
        CancellationToken cancellationToken)
    {
        var registeredWorkspaces = GetRegisteredWorkspacesInSearchOrder();
        var solutions = ImmutableArray.CreateBuilder<(Workspace, Solution, bool)>(registeredWorkspaces.Length);
        foreach (var workspace in registeredWorkspaces)
        {
            // Retrieve the workspace's current view of the world at the time the request comes in. If this is changing
            // underneath, it is either the job of the LSP client to poll us (diagnostics) or we send refresh
            // notifications (semantic tokens) to the client letting them know that our workspace has changed and they
            // need to re-query us.
            var (lspSolution, isForked) = await GetLspSolutionForWorkspaceAsync(
                workspace, cancellationToken).ConfigureAwait(false);
            solutions.Add((workspace, lspSolution, isForked));
        }

        return solutions.MoveToImmutable();
    }

    private ImmutableArray<Workspace> GetRegisteredWorkspacesInSearchOrder()
    {
        var workspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
        return [
            .. workspaces.Where(workspace => workspace.Kind != WorkspaceKind.MiscellaneousFiles),
            .. workspaces.Where(workspace => workspace.Kind == WorkspaceKind.MiscellaneousFiles),
        ];
    }

    private async Task<(Solution Solution, bool IsForked)> GetLspSolutionForWorkspaceAsync(
        Workspace workspace, CancellationToken cancellationToken)
    {
        var workspaceCurrentSolution = workspace.CurrentSolution;
        if (_cachedLspSolutions.TryGetValue(workspace, out var cachedSolution) &&
            cachedSolution.solution == workspaceCurrentSolution)
        {
            return (workspaceCurrentSolution, IsForked: false);
        }

        await TryOpenAndEditDocumentsInMutatingWorkspaceAsync(workspace, cancellationToken).ConfigureAwait(false);

        // Because the workspace may have been mutated, go back and retrieve its current snapshot so we're operating
        // against that view.
        workspaceCurrentSolution = workspace.CurrentSolution;
        var forkedFromVersion = workspaceCurrentSolution.SolutionStateContentVersion;
        var sourceGeneratorChecksum = workspaceCurrentSolution.CompilationState.SourceGeneratorExecutionVersionMap.GetChecksum();
        var cachedFork = cachedSolution.forkedFromVersion == forkedFromVersion &&
            cachedSolution.sourceGeneratorChecksum == sourceGeneratorChecksum ? cachedSolution.solution : null;

        var result = await ApplyLspTextAsync(
            workspaceCurrentSolution, _trackedDocuments, cachedFork, cancellationToken).ConfigureAwait(false);
        _cachedLspSolutions[workspace] = result.IsForked
            ? (forkedFromVersion, sourceGeneratorChecksum, result.Solution)
            : (null, null, result.Solution);
        return result;
    }

    private async Task<(Solution Solution, bool IsForked)> ApplyLspTextAsync(
        Solution workspaceCurrentSolution,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        Solution? cachedFork,
        CancellationToken cancellationToken)
    {
        var documentsInWorkspace = GetDocumentsForUris([.. trackedDocuments.Keys], workspaceCurrentSolution);
        var sourceGeneratedDocuments =
            trackedDocuments.Keys.Where(static trackedDocument => trackedDocument.IsSourceGeneratedUri())
                // We know we have a non null URI with a source generated scheme.
                .Select(uri => (identity: SourceGeneratedDocumentUri.DeserializeIdentity(workspaceCurrentSolution, uri.GetRequiredParsedUri()), trackedDocuments[uri].SourceText))
                .SelectAsArray(
                    predicate: tuple => tuple.identity.HasValue,
                    selector: tuple => (tuple.identity!.Value, DateTime.Now, tuple.SourceText));

        // First we check if normal document text matches the workspace solution.
        // This does not look at source generated documents.
        var doesAllTextMatch = await DoesAllTextMatchWorkspaceSolutionAsync(
            documentsInWorkspace, trackedDocuments, cancellationToken).ConfigureAwait(false);

        // Then we check if source generated document text matches the workspace solution.
        // This is intentionally done differently from normal documents because the normal method will cause
        // source generators to run which we do not want to do in queue dispatch.
        var doesAllSourceGeneratedTextMatch = DoesAllSourceGeneratedTextMatchWorkspaceSolution(sourceGeneratedDocuments, workspaceCurrentSolution);
        if (doesAllTextMatch && doesAllSourceGeneratedTextMatch)
        {
            return (workspaceCurrentSolution, IsForked: false);
        }

        // Prefer the actual workspace snapshot if text has caught up since this fork was cached.
        if (cachedFork is not null)
        {
            return (cachedFork, IsForked: true);
        }

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
    }

    private async ValueTask TryOpenAndEditDocumentsInMutatingWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
    {
        foreach (var (uri, (sourceText, _, _)) in _trackedDocuments)
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

    /// <summary>
    /// Checks if the open source generator document contents matches the contents of the workspace solution.
    /// This looks at the source generator state explicitly to avoid actually running source generators
    /// </summary>
    private static bool DoesAllSourceGeneratedTextMatchWorkspaceSolution(
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

    /// <summary>
    /// Given a set of documents from the workspace current solution, verify that the LSP text is the same as the document contents.
    /// </summary>
    private async Task<bool> DoesAllTextMatchWorkspaceSolutionAsync(
        ImmutableDictionary<DocumentUri, ImmutableArray<TextDocument>> documentsInWorkspace,
        ImmutableDictionary<DocumentUri, TrackedDocumentInfo> trackedDocuments,
        CancellationToken cancellationToken)
    {
        foreach (var (uriInWorkspace, documentsForUri) in documentsInWorkspace)
        {
            var lspText = trackedDocuments[uriInWorkspace].SourceText;
            foreach (var document in documentsForUri)
            {
                // Linked documents can temporarily have different text when only part of the linked set has been updated.
                var isTextEquivalent = await AreChecksumsEqualAsync(document, lspText, cancellationToken).ConfigureAwait(false);
                if (!isTextEquivalent)
                {
                    _logger.LogWarning($"Text for {uriInWorkspace} did not match document text {document.Id} in workspace's {document.Project.Solution.WorkspaceKind} current solution");
                    return false;
                }
            }
        }

        return true;
    }

    private static async ValueTask<bool> AreChecksumsEqualAsync(TextDocument document, SourceText lspText, CancellationToken cancellationToken)
    {
        var documentText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        if (documentText == lspText)
            return true;

        return lspText.GetContentHash().AsSpan().SequenceEqual(documentText.GetContentHash().AsSpan());
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

    /// <summary>
    /// Using the workspace's current solutions, find the matching documents in for each URI.
    /// </summary>
    private static ImmutableDictionary<DocumentUri, ImmutableArray<TextDocument>> GetDocumentsForUris(ImmutableArray<DocumentUri> trackedDocuments, Solution workspaceCurrentSolution)
    {
        using var _ = PooledDictionary<DocumentUri, ImmutableArray<TextDocument>>.GetInstance(out var documentsInSolution);
        foreach (var trackedDoc in trackedDocuments)
        {
            var documents = workspaceCurrentSolution.GetTextDocuments(trackedDoc);
            if (documents.Any())
            {
                documentsInSolution[trackedDoc] = documents;
            }
        }

        return documentsInSolution.ToImmutableDictionary();
    }

    internal readonly record struct LspContext(
        Workspace Workspace,
        Solution Solution,
        TextDocument? Document);

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
