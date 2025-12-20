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
    private readonly ImmutableArray<ILspMiscellaneousFilesWorkspaceProvider> _lspMiscellaneousFilesWorkspaceProviders;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly ILanguageInfoProvider _languageInfoProvider;
    private readonly RequestTelemetryLogger _requestTelemetryLogger;

    public LspWorkspaceManager(
        ILspLogger logger,
        ImmutableArray<ILspMiscellaneousFilesWorkspaceProvider> lspMiscellaneousFilesWorkspaceProviders,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        ILanguageInfoProvider languageInfoProvider,
        RequestTelemetryLogger requestTelemetryLogger)
    {
        _lspMiscellaneousFilesWorkspaceProviders = lspMiscellaneousFilesWorkspaceProviders;
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

        if (uri.ParsedUri is null)
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
        if (!_lspMiscellaneousFilesWorkspaceProviders.IsDefaultOrEmpty)
        {
            try
            {
                // Loop through providers until one successfully removes the document
                foreach (var provider in _lspMiscellaneousFilesWorkspaceProviders)
                {
                    if (await provider.TryRemoveMiscellaneousDocumentAsync(uri).ConfigureAwait(false))
                    {
                        break;
                    }
                }
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

    /// <summary>
    /// Returns the LSP solution associated with the workspace with workspace kind <see cref="WorkspaceKind.Host"/>.
    /// This is the solution used for LSP requests that pertain to the entire workspace, for example code search or
    /// workspace diagnostics.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue{RequestContextType}"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<(Workspace?, Solution?)> GetLspSolutionInfoAsync(CancellationToken cancellationToken)
    {
        // Ensure we have the latest lsp solutions
        var updatedSolutions = await GetLspSolutionsAsync(cancellationToken).ConfigureAwait(false);

        var (hostWorkspace, hostWorkspaceSolution, isForked) = updatedSolutions.FirstOrDefault(lspSolution => lspSolution.Solution.WorkspaceKind is WorkspaceKind.Host);
        _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);

        return (hostWorkspace, hostWorkspaceSolution);
    }

    /// <summary>
    /// Returns the LSP solution associated with the workspace with kind <see cref="WorkspaceKind.Host"/>. This is the
    /// solution used for LSP requests that pertain to the entire workspace, for example code search or workspace
    /// diagnostics.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue{RequestContextType}"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<(Workspace?, Solution?, TextDocument?)> GetLspDocumentInfoAsync(TextDocumentIdentifier textDocumentIdentifier, CancellationToken cancellationToken)
    {
        // Get the LSP view of all the workspace solutions.
        var uri = textDocumentIdentifier.DocumentUri;
        var lspSolutions = await GetLspSolutionsAsync(cancellationToken).ConfigureAwait(false);

        // Find the matching document from the LSP solutions.
        foreach (var (workspace, lspSolution, isForked) in lspSolutions)
        {
            var documents = await lspSolution.GetTextDocumentsAsync(textDocumentIdentifier.DocumentUri, cancellationToken).ConfigureAwait(false);

            if (documents.Length > 0)
            {
                // We have at least one document, so find the one in the right project context.
                var document = documents.FindDocumentInProjectContext(textDocumentIdentifier, (sln, id) => sln.GetRequiredTextDocument(id));

                if (!_lspMiscellaneousFilesWorkspaceProviders.IsDefaultOrEmpty)
                {
                    // It is possible that a document that was previously a misc file is now part of a real workspace (e.g. project system told us about a file we already had open).
                    // We need to check if:
                    // 1. We found a non-misc document, OR
                    // 2. A different provider can take ownership of this document
                    
                    var shouldRemove = false;
                    
                    // Check if any document is not a misc document
                    var foundNonMiscDocument = false;
                    foreach (var doc in documents)
                    {
                        var isMiscInAnyProvider = false;
                        foreach (var provider in _lspMiscellaneousFilesWorkspaceProviders)
                        {
                            if (await provider.IsMiscellaneousFilesDocumentAsync(doc, cancellationToken).ConfigureAwait(false))
                            {
                                isMiscInAnyProvider = true;
                                break;
                            }
                        }
                        
                        if (!isMiscInAnyProvider)
                        {
                            foundNonMiscDocument = true;
                            break;
                        }
                    }
                    
                    if (foundNonMiscDocument)
                    {
                        shouldRemove = true;
                    }
                    else if (_trackedDocuments.TryGetValue(uri, out var trackedDocument))
                    {
                        // Check if a prior provider can take ownership
                        var documentFilePath = uri.ParsedUri is { } parsedUri 
                            ? ProtocolConversions.GetDocumentFilePathFromUri(parsedUri) 
                            : uri.UriString;
                        
                        for (var i = 0; i < _lspMiscellaneousFilesWorkspaceProviders.Length; i++)
                        {
                            var priorProvider = _lspMiscellaneousFilesWorkspaceProviders[i];
                            
                            // Check if this provider can take ownership
                            if (await priorProvider.CanTakeOwnership(trackedDocument.SourceText, documentFilePath, trackedDocument.LanguageId).ConfigureAwait(false))
                            {
                                // Check if the document is in a different (later) provider
                                for (var j = i + 1; j < _lspMiscellaneousFilesWorkspaceProviders.Length; j++)
                                {
                                    var currentProvider = _lspMiscellaneousFilesWorkspaceProviders[j];
                                    if (await documents.AnyAsync(async doc => await currentProvider.IsMiscellaneousFilesDocumentAsync(doc, cancellationToken).ConfigureAwait(false)).ConfigureAwait(false))
                                    {
                                        // Document is in a later provider but an earlier provider can take ownership
                                        shouldRemove = true;
                                        break;
                                    }
                                }
                                
                                if (shouldRemove)
                                    break;
                            }
                        }
                    }
                    
                    if (shouldRemove)
                    {
                        try
                        {
                            // Loop through providers until one successfully removes the document
                            var didRemove = false;
                            foreach (var provider in _lspMiscellaneousFilesWorkspaceProviders)
                            {
                                if (await provider.TryRemoveMiscellaneousDocumentAsync(uri).ConfigureAwait(false))
                                {
                                    didRemove = true;
                                    break;
                                }
                            }
                            
                            if (didRemove)
                            {
                                // If we actually removed something, lookup the document again to ensure we return updated solutions without the misc document.
                                return await GetLspDocumentInfoAsync(textDocumentIdentifier, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        catch (Exception ex) when (FatalError.ReportAndCatch(ex))
                        {
                            _logger.LogException(ex);
                        }
                    }
                }

                // Record metadata on how we got this document.
                var workspaceKind = document.Project.Solution.WorkspaceKind;
                _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspaceKind);
                _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);
                _logger.LogDebug($"{document.FilePath} found in workspace {workspaceKind}; project {document.Project.Name}");

                return (workspace, document.Project.Solution, document);
            }
        }

        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        // Depending on the host, this can be entirely normal (e.g. opening a loose file)
        var searchedWorkspaceKinds = string.Join(";", lspSolutions.SelectAsArray(lspSolution => lspSolution.Solution.Workspace.Kind));
        _logger.LogDebug($"Could not find '{textDocumentIdentifier.DocumentUri}'.  Searched {searchedWorkspaceKinds}");
        _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);

        // Add the document to our loose files workspace (if we have one) if it is open.
        if (_trackedDocuments.TryGetValue(uri, out var trackedDocInfo) && !_lspMiscellaneousFilesWorkspaceProviders.IsDefaultOrEmpty)
        {
            try
            {
                // Loop through providers until one successfully adds the document
                foreach (var provider in _lspMiscellaneousFilesWorkspaceProviders)
                {
                    var miscDocument = await provider.TryAddMiscellaneousDocumentAsync(uri, trackedDocInfo.SourceText, trackedDocInfo.LanguageId, _logger).ConfigureAwait(false);
                    if (miscDocument is not null)
                        return (miscDocument.Project.Solution.Workspace, miscDocument.Project.Solution, miscDocument);
                }
                
                // If no provider could handle the document, throw an exception
                throw new InvalidOperationException($"No miscellaneous files provider could handle document {uri}");
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex))
            {
                _logger.LogException(ex);
            }
        }

        return default;
    }

    /// <summary>
    /// Gets the LSP view of all the registered workspaces' current solutions.
    /// </summary>
    private async Task<ImmutableArray<(Workspace workspace, Solution Solution, bool IsForked)>> GetLspSolutionsAsync(CancellationToken cancellationToken)
    {
        // Ensure that the loose files workspace is searched last.
        var registeredWorkspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
        registeredWorkspaces =
        [
            .. registeredWorkspaces
                        .Where(workspace => workspace.Kind != WorkspaceKind.MiscellaneousFiles)
,
            .. registeredWorkspaces.Where(workspace => workspace.Kind == WorkspaceKind.MiscellaneousFiles),
        ];

        var solutions = new FixedSizeArrayBuilder<(Workspace, Solution, bool)>(registeredWorkspaces.Length);
        foreach (var workspace in registeredWorkspaces)
        {
            // Retrieve the workspace's current view of the world at the time the request comes in. If this is changing
            // underneath, it is either the job of the LSP client to poll us (diagnostics) or we send refresh
            // notifications (semantic tokens) to the client letting them know that our workspace has changed and they
            // need to re-query us.
            var (lspSolution, isForked) = await GetLspSolutionForWorkspaceAsync(workspace, cancellationToken).ConfigureAwait(false);
            solutions.Add((workspace, lspSolution, isForked));
        }

        return solutions.MoveToImmutable();

        async Task<(Solution Solution, bool IsForked)> GetLspSolutionForWorkspaceAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            var workspaceCurrentSolution = workspace.CurrentSolution;

            // At a high level these are the steps we take to compute what the desired LSP solution should be.
            //
            //  1. First we want to check if our workspace current solution is the same as the last workspace current
            //     solution that we verified matches the LSP text. If so, we can skip comparing the LSP text against the
            //     workspace text and just return the cached one since absolutely nothing has changed. Importantly, we
            //     do not return a cached forked solution - we do not want to re-use a forked solution if the LSP text
            //     has changed and now matches the workspace.
            //
            //  2. Next, ensure that any changes we've collected are pushed through to the underlying workspace *if* 
            //     it's a mutating workspace.  This will bring that workspace into sync with all that we've heard from lsp.
            //
            //  3. If the cached solution isn't a match, we compare the LSP text to the workspace's text and return the
            //     workspace text if all LSP text matches. While this does compute checksums, generally speaking that's
            //     a reasonable price to pay.  For example, we always do this in VS anyways to make OOP calls, and it is
            //     not a burden there.
            //
            //  4. Third, we check to see if we have cached a forked LSP solution for the current set of LSP texts
            //     against the current workspace version. If so, we can just reuse that instead of re-forking and
            //     blowing away the trees / source generated docs / etc. that we created for the fork.
            //
            //  5. We have nothing cached for this combination of LSP texts and workspace version.  We have exhausted
            //     our options and must create an LSP fork from the current workspace solution with the current LSP
            //     text.
            //
            // We propagate the IsForked value back up so that we only report telemetry on forking if the forked
            // solution is actually requested.

            // Step 1: Check if nothing has changed and we already verified that the workspace text matches our LSP text.
            if (_cachedLspSolutions.TryGetValue(workspace, out var cachedSolution) && cachedSolution.solution == workspaceCurrentSolution)
                return (workspaceCurrentSolution, IsForked: false);

            // Step 2: Push through any changes to the underlying workspace if it's a mutating workspace.
            await TryOpenAndEditDocumentsInMutatingWorkspaceAsync(workspace).ConfigureAwait(false);

            // Because the workspace may have been mutated, go back and retrieve its current snapshot so we're operating
            // against that view.
            workspaceCurrentSolution = workspace.CurrentSolution;

            // Step 3: Check to see if the LSP text matches the workspace text.

            var documentsInWorkspace = GetDocumentsForUris([.. _trackedDocuments.Keys], workspaceCurrentSolution);
            var sourceGeneratedDocuments =
                _trackedDocuments.Keys.Where(static trackedDocument => trackedDocument.ParsedUri?.Scheme == SourceGeneratedDocumentUri.Scheme)
                    // We know we have a non null URI with a source generated scheme.
                    .Select(uri => (identity: SourceGeneratedDocumentUri.DeserializeIdentity(workspaceCurrentSolution, uri.ParsedUri!), _trackedDocuments[uri].SourceText))
                    .SelectAsArray(
                        predicate: tuple => tuple.identity.HasValue,
                        selector: tuple => (tuple.identity!.Value, DateTime.Now, tuple.SourceText));

            // First we check if normal document text matches the workspace solution.
            // This does not look at source generated documents.
            var doesAllTextMatch = await DoesAllTextMatchWorkspaceSolutionAsync(documentsInWorkspace, cancellationToken).ConfigureAwait(false);

            // Then we check if source generated document text matches the workspace solution.
            // This is intentionally done differently from normal documents because the normal method will cause
            // source generators to run which we do not want to do in queue dispatch.
            var doesAllSourceGeneratedTextMatch = DoesAllSourceGeneratedTextMatchWorkspaceSolution(sourceGeneratedDocuments, workspaceCurrentSolution);
            if (doesAllTextMatch && doesAllSourceGeneratedTextMatch)
            {
                // Remember that the current LSP text matches the text in this workspace solution.
                _cachedLspSolutions[workspace] = (forkedFromVersion: null, sourceGeneratorChecksum: null, workspaceCurrentSolution);
                return (workspaceCurrentSolution, IsForked: false);
            }

            var forkedFromVersion = workspaceCurrentSolution.SolutionStateContentVersion;
            var sourceGeneratorChecksum = workspaceCurrentSolution.CompilationState.SourceGeneratorExecutionVersionMap.GetChecksum();

            // Step 4: See if we can reuse a previously forked solution.
            if (cachedSolution != default &&
                cachedSolution.forkedFromVersion == forkedFromVersion &&
                cachedSolution.sourceGeneratorChecksum == sourceGeneratorChecksum)
            {
                return (cachedSolution.solution, IsForked: true);
            }

            // Step 5: Fork a new solution from the workspace with the LSP text applied.
            var lspSolution = workspaceCurrentSolution;
            // If the workspace text matched we can leave the normal documents as-is
            if (!doesAllTextMatch)
            {
                foreach (var (uri, workspaceDocuments) in documentsInWorkspace)
                    lspSolution = lspSolution.WithDocumentText(workspaceDocuments.Select(d => d.Id), _trackedDocuments[uri].SourceText);
            }

            // If the source generated documents matched we can leave the source generated documents as-is
            if (!doesAllSourceGeneratedTextMatch)
            {
                lspSolution = lspSolution.WithFrozenSourceGeneratedDocuments(sourceGeneratedDocuments);
            }

            // Remember this forked solution and the workspace version it was forked from.
            _cachedLspSolutions[workspace] = (forkedFromVersion, sourceGeneratorChecksum, lspSolution);
            return (lspSolution, IsForked: true);
        }

        async ValueTask TryOpenAndEditDocumentsInMutatingWorkspaceAsync(Workspace workspace)
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
    private async Task<bool> DoesAllTextMatchWorkspaceSolutionAsync(ImmutableDictionary<DocumentUri, ImmutableArray<TextDocument>> documentsInWorkspace, CancellationToken cancellationToken)
    {
        foreach (var (uriInWorkspace, documentsForUri) in documentsInWorkspace)
        {
            // We're comparing text, so we can take any of the linked documents.
            var firstDocument = documentsForUri.First();
            var isTextEquivalent = await AreChecksumsEqualAsync(firstDocument, _trackedDocuments[uriInWorkspace].SourceText, cancellationToken).ConfigureAwait(false);

            if (!isTextEquivalent)
            {
                _logger.LogWarning($"Text for {uriInWorkspace} did not match document text {firstDocument.Id} in workspace's {firstDocument.Project.Solution.WorkspaceKind} current solution");
                return false;
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

    internal TestAccessor GetTestAccessor()
            => new(this);

    internal readonly struct TestAccessor
    {
        private readonly LspWorkspaceManager _manager;

        public TestAccessor(LspWorkspaceManager manager)
            => _manager = manager;

        public async ValueTask<bool> IsMiscellaneousFilesDocumentAsync(TextDocument document)
        {
            // Check if the document is a misc document in any provider
            foreach (var provider in _manager._lspMiscellaneousFilesWorkspaceProviders)
            {
                if (await provider.IsMiscellaneousFilesDocumentAsync(document, CancellationToken.None).ConfigureAwait(false))
                    return true;
            }
            return false;
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
