// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.RequestExecutionQueue;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Manages the registered workspaces and corresponding LSP solutions for an LSP server.
/// This type is tied to a particular server.
/// </summary>
/// <remarks>
/// This type provides an LSP view of the registered workspace solutions so that all LSP requests operate
/// on the state of the world that matches the LSP requests we've recieved.  
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
internal class LspWorkspaceManager : IDocumentChangeTracker, ILspService
{
    /// <summary>
    /// A cache from workspace to the last solution we returned for LSP.
    /// 
    /// The forkedFromVersion is not null when the solution was created from a fork of the workspace with LSP text applied on top.
    /// It is null when LSP reuses the workspace solution (the LSP text matches the contents of the workspace).
    /// 
    /// Access to this is gauranteed to be serial by the <see cref="RequestExecutionQueue"/>
    /// </summary>
    private readonly Dictionary<Workspace, (int? forkedFromVersion, Solution solution)> _cachedLspSolutions = new();

    /// <summary>
    /// Stores the current source text for each URI that is being tracked by LSP.
    /// Each time an LSP text sync notification comes in, this source text is updated to match.
    /// Used as the backing implementation for the <see cref="IDocumentChangeTracker"/>.
    /// 
    /// Note that the text here is tracked regardless of whether or not we found a matching roslyn document
    /// for the URI.
    /// 
    /// Access to this is gauranteed to be serial by the <see cref="RequestExecutionQueue"/>
    /// </summary>
    private ImmutableDictionary<Uri, SourceText> _trackedDocuments = ImmutableDictionary<Uri, SourceText>.Empty;

    private readonly string _hostWorkspaceKind;
    private readonly ILspLogger _logger;
    private readonly LspMiscellaneousFilesWorkspace? _lspMiscellaneousFilesWorkspace;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;
    private readonly RequestTelemetryLogger _requestTelemetryLogger;

    public LspWorkspaceManager(
        ILspLogger logger,
        LspMiscellaneousFilesWorkspace? lspMiscellaneousFilesWorkspace,
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        RequestTelemetryLogger requestTelemetryLogger)
    {
        _hostWorkspaceKind = lspWorkspaceRegistrationService.GetHostWorkspaceKind();

        _lspMiscellaneousFilesWorkspace = lspMiscellaneousFilesWorkspace;
        _logger = logger;
        _requestTelemetryLogger = requestTelemetryLogger;

        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;

    }

    #region Implementation of IDocumentChangeTracker

    /// <summary>
    /// Called by the <see cref="DidOpenHandler"/> when a document is opened in LSP.
    /// 
    /// <see cref="DidOpenHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue"/>
    /// </summary>
    public void StartTracking(Uri uri, SourceText documentText)
    {
        // First, store the LSP view of the text as the uri is now owned by the LSP client.
        Contract.ThrowIfTrue(_trackedDocuments.ContainsKey(uri), $"didOpen received for {uri} which is already open.");
        _trackedDocuments = _trackedDocuments.Add(uri, documentText);

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();
    }

    /// <summary>
    /// Called by the <see cref="DidCloseHandler"/> when a document is closed in LSP.
    /// 
    /// <see cref="DidCloseHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue"/>
    /// </summary>
    public void StopTracking(Uri uri)
    {
        // First, stop tracking this URI and source text as it is no longer owned by LSP.
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(uri), $"didClose received for {uri} which is not open.");
        _trackedDocuments = _trackedDocuments.Remove(uri);

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();

        // Also remove it from our loose files workspace if it is still there.
        _lspMiscellaneousFilesWorkspace?.TryRemoveMiscellaneousDocument(uri);
    }

    /// <summary>
    /// Called by the <see cref="DidChangeHandler"/> when a document's text is updated in LSP.
    /// 
    /// <see cref="DidChangeHandler.MutatesSolutionState"/> is true which means this runs serially in the <see cref="RequestExecutionQueue"/>
    /// </summary>
    public void UpdateTrackedDocument(Uri uri, SourceText newSourceText)
    {
        // Store the updated LSP view of the source text.
        Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(uri), $"didChange received for {uri} which is not open.");
        _trackedDocuments = _trackedDocuments.SetItem(uri, newSourceText);

        // If LSP changed, we need to compare against the workspace again to get the updated solution.
        _cachedLspSolutions.Clear();
    }

    public ImmutableDictionary<Uri, SourceText> GetTrackedLspText() => _trackedDocuments;

    #endregion

    #region LSP Solution Retrieval

    /// <summary>
    /// Returns the LSP solution associated with the workspace with the specified <see cref="_hostWorkspaceKind"/>.
    /// This is the solution used for LSP requests that pertain to the entire workspace, for example code search or workspace diagnostics.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<Solution?> TryGetHostLspSolutionAsync(CancellationToken cancellationToken)
    {
        // Ensure we have the latest lsp solutions
        var updatedSolutions = await GetLspSolutionsAsync(cancellationToken).ConfigureAwait(false);

        var (hostWorkspaceSolution, isForked) = updatedSolutions.FirstOrDefault(lspSolution => lspSolution.Solution.Workspace.Kind == _hostWorkspaceKind);
        _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);

        return hostWorkspaceSolution;
    }

    /// <summary>
    /// Returns a document with the LSP tracked text forked from the appropriate workspace solution.
    /// 
    /// This is always called serially in the <see cref="RequestExecutionQueue"/> when creating the <see cref="RequestContext"/>.
    /// </summary>
    public async Task<Document?> GetLspDocumentAsync(TextDocumentIdentifier textDocumentIdentifier, CancellationToken cancellationToken)
    {
        var uri = textDocumentIdentifier.Uri;

        // Get the LSP view of all the workspace solutions.
        var lspSolutions = await GetLspSolutionsAsync(cancellationToken).ConfigureAwait(false);

        // Find the matching document from the LSP solutions.
        foreach (var (lspSolution, isForked) in lspSolutions)
        {
            var documents = lspSolution.GetDocuments(uri);
            if (documents.Any())
            {
                var document = documents.FindDocumentInProjectContext(textDocumentIdentifier);

                // Record metadata on how we got this document.
                var workspaceKind = document.Project.Solution.Workspace.Kind;
                _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspaceKind);
                _requestTelemetryLogger.UpdateUsedForkedSolutionCounter(isForked);
                _logger.TraceInformation($"{document.FilePath} found in workspace {workspaceKind}");

                return document;
            }
        }

        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        var searchedWorkspaceKinds = string.Join(";", lspSolutions.SelectAsArray(lspSolution => lspSolution.Solution.Workspace.Kind));
        _logger.TraceError($"Could not find '{uri}'.  Searched {searchedWorkspaceKinds}");
        _requestTelemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);

        // Add the document to our loose files workspace if its open.
        var miscDocument = _trackedDocuments.ContainsKey(uri) ? _lspMiscellaneousFilesWorkspace?.AddMiscellaneousDocument(uri, _trackedDocuments[uri], _logger) : null;
        return miscDocument;
    }

    /// <summary>
    /// Gets the LSP view of all the registered workspaces' current solutions.
    /// </summary>
    private async Task<ImmutableArray<(Solution Solution, bool IsForked)>> GetLspSolutionsAsync(CancellationToken cancellationToken)
    {
        // Ensure that the loose files workspace is searched last.
        var registeredWorkspaces = _lspWorkspaceRegistrationService.GetAllRegistrations();
        registeredWorkspaces = registeredWorkspaces
            .Where(workspace => workspace is not LspMiscellaneousFilesWorkspace)
            .Concat(registeredWorkspaces.Where(workspace => workspace is LspMiscellaneousFilesWorkspace)).ToImmutableArray();

        using var _ = ArrayBuilder<(Solution, bool)>.GetInstance(out var solutions);
        foreach (var workspace in registeredWorkspaces)
        {
            // Retrieve the workspace's current view of the world at the time the request comes in.
            // If this is changing underneath, it is either the job of the LSP client to poll us (diagnostics) or we send refresh notifications (semantic tokens) to the client
            // letting them know that our workspace has changed and they need to re-query us.
            var workspaceCurrentSolution = workspace.CurrentSolution;
            var lspSolution = await GetLspSolutionForWorkspaceAsync(workspaceCurrentSolution, cancellationToken).ConfigureAwait(false);
            solutions.Add(lspSolution);
        }

        return solutions.ToImmutable();

        async Task<(Solution Solution, bool IsForked)> GetLspSolutionForWorkspaceAsync(Solution workspaceCurrentSolution, CancellationToken cancellationToken)
        {
            // At a high level these are the steps we take to compute what the desired LSP solution should be.
            //
            //   1.  First we want to check if our workspace current solution is the same as the last workspace current solution that we verified matches the LSP text.
            //       If so, we can skip comparing the LSP text against the workspace text and just return the cached one since absolutely nothing has changed.
            //       Importantly, we do not return a cached forked solution - we do not want to re-use a forked solution if the LSP text has changed and now matches the workspace.
            //
            //   2.  If the cached solution isn't a match, we compare the LSP text to the workspace's text and return the workspace text if all LSP text matches.
            //       This check is performant as checksums will be computed for these documents in order to make requests OOP.  So these are already computed or will be later in this request.
            //
            //   3.  Third, we check to see if we have cached a forked LSP solution for the current set of LSP texts against the current workspace version.
            //       If so, we can just reuse that instead of re-forking and blowing away the trees / source generated docs / etc. that we created for the fork.
            //       
            //   4.  We have nothing cached for this combination of LSP texts and workspace version.  We have exhausted our options and must create an LSP fork
            //       from the current workspace solution with the current LSP text.
            //
            //   We propogate the IsForked value back up so that we only report telemetry on forking if the forked solution is actually requested.

            // Step 1: Check if nothing has changed and we already verified that the workspace text matches our LSP text.
            if (_cachedLspSolutions.TryGetValue(workspaceCurrentSolution.Workspace, out var cachedSolution) && cachedSolution.solution == workspaceCurrentSolution)
            {
                return (workspaceCurrentSolution, IsForked: false);
            }

            // Step 2: Check to see if the LSP text matches the workspace text.
            var documentsInWorkspace = GetDocumentsForUris(_trackedDocuments.Keys.ToImmutableArray(), workspaceCurrentSolution);
            if (await DoesAllTextMatchWorkspaceSolutionAsync(documentsInWorkspace, cancellationToken).ConfigureAwait(false))
            {
                // Remember that the current LSP text matches the text in this workspace solution.
                _cachedLspSolutions[workspaceCurrentSolution.Workspace] = (forkedFromVersion: null, workspaceCurrentSolution);
                return (workspaceCurrentSolution, IsForked: false);
            }

            // Step 3: See if we can reuse a previously forked solution.
            if (cachedSolution != default && cachedSolution.forkedFromVersion == workspaceCurrentSolution.WorkspaceVersion)
            {
                return (cachedSolution.solution, IsForked: true);
            }

            // Step 4: Fork a new solution from the workspace with the LSP text applied.
            var lspSolution = workspaceCurrentSolution;
            foreach (var (uri, workspaceDocuments) in documentsInWorkspace)
            {
                lspSolution = lspSolution.WithDocumentText(workspaceDocuments.Select(d => d.Id), _trackedDocuments[uri]);
            }

            // Remember this forked solution and the workspace version it was forked from.
            _cachedLspSolutions[workspaceCurrentSolution.Workspace] = (workspaceCurrentSolution.WorkspaceVersion, lspSolution);
            return (lspSolution, IsForked: true);
        }
    }

    /// <summary>
    /// Given a set of documents from the workspace current solution, verify that the LSP text is the same as the document contents.
    /// </summary>
    private async Task<bool> DoesAllTextMatchWorkspaceSolutionAsync(ImmutableDictionary<Uri, ImmutableArray<Document>> documentsInWorkspace, CancellationToken cancellationToken)
    {
        foreach (var (uriInWorkspace, documentsForUri) in documentsInWorkspace)
        {
            // We're comparing text, so we can take any of the linked documents.
            var firstDocument = documentsForUri.First();
            var isTextEquivalent = await AreChecksumsEqualAsync(firstDocument, _trackedDocuments[uriInWorkspace], cancellationToken).ConfigureAwait(false);

            if (!isTextEquivalent)
            {
                _logger.TraceWarning($"Text for {uriInWorkspace} did not match document text {firstDocument.Id} in workspace's {firstDocument.Project.Solution.Workspace.Kind} current solution");
                return false;
            }
        }

        return true;
    }

    private static async Task<bool> AreChecksumsEqualAsync(Document document, SourceText lspText, CancellationToken cancellationToken)
    {
        var documentText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var documentTextChecksum = documentText.GetChecksum();
        var lspTextChecksum = lspText.GetChecksum();
        return lspTextChecksum.SequenceEqual(documentTextChecksum);
    }

    #endregion

    /// <summary>
    /// Using the workspace's current solutions, find the matching documents in for each URI.
    /// </summary>
    private static ImmutableDictionary<Uri, ImmutableArray<Document>> GetDocumentsForUris(ImmutableArray<Uri> trackedDocuments, Solution workspaceCurrentSolution)
    {
        using var _ = PooledDictionary<Uri, ImmutableArray<Document>>.GetInstance(out var documentsInSolution);
        foreach (var trackedDoc in trackedDocuments)
        {
            var documents = workspaceCurrentSolution.GetDocuments(trackedDoc);
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

        public LspMiscellaneousFilesWorkspace? GetLspMiscellaneousFilesWorkspace()
            => _manager._lspMiscellaneousFilesWorkspace;

        public bool IsWorkspaceRegistered(Workspace workspace)
        {
            return _manager._lspWorkspaceRegistrationService.GetAllRegistrations().Contains(workspace);
        }
    }
}
