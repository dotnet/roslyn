// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DocumentChanges;
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
internal partial class LspWorkspaceManager
{
    /// <summary>
    /// Lock to gate access to the <see cref="_workspaceToLspSolution"/>.
    /// Access from the LSP server is serial as the LSP queue is processed serially until
    /// after we give the solution to the request handlers.  However workspace events can interleave
    /// so we must protect against concurrent access here.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// A map from the registered workspace to the running lsp solution with incremental changes.
    /// </summary>
    private readonly Dictionary<Workspace, Solution?> _workspaceToLspSolution = new();

    private readonly string _hostWorkspaceKind;
    private readonly ILspLogger _logger;
    private readonly IDocumentChangeTracker _documentChangeTracker;
    private readonly LspMiscellaneousFilesWorkspace? _lspMiscellaneousFilesWorkspace;
    private readonly RequestTelemetryLogger _requestTelemetryLogger;

    public LspWorkspaceManager(
        LspWorkspaceRegistrationService lspWorkspaceRegistrationService,
        LspMiscellaneousFilesWorkspace? lspMiscellaneousFilesWorkspace,
        IDocumentChangeTracker documentChangeTracker,
        ILspLogger logger,
        RequestTelemetryLogger requestTelemetryLogger)
    {
        _hostWorkspaceKind = lspWorkspaceRegistrationService.GetHostWorkspaceKind();

        _lspMiscellaneousFilesWorkspace = lspMiscellaneousFilesWorkspace;
        _documentChangeTracker = documentChangeTracker;
        _logger = logger;
        _requestTelemetryLogger = requestTelemetryLogger;

        lspWorkspaceRegistrationService.WorkspaceRegistered += OnWorkspaceRegistered;
        // This server may have been started after workspaces were registered, we need to ensure we know about them.
        PopulateAlreadyRegisteredWorkspaces();

        void PopulateAlreadyRegisteredWorkspaces()
        {
            foreach (var workspace in lspWorkspaceRegistrationService.GetAllRegistrations())
            {
                _workspaceToLspSolution[workspace] = null;
                workspace.WorkspaceChanged += OnWorkspaceChanged;
            }
        }
    }

    #region Workspace Updates

    /// <summary>
    /// Handles cases where a new workspace is registered after an LSP server is already started.
    /// For example, in VS the metadata as source workspace is not created until a metadata files is opened.
    /// </summary>
    private void OnWorkspaceRegistered(object? sender, LspWorkspaceRegisteredEventArgs e)
    {
        lock (_gate)
        {
            _workspaceToLspSolution[e.Workspace] = null;
            e.Workspace.WorkspaceChanged += OnWorkspaceChanged;
        }
    }

    /// <summary>
    /// Subscribed to the <see cref="Workspace.WorkspaceChanged"/> event for registered workspaces.
    /// If the workspace change is a change to an open LSP document, we ignore the change event as LSP will update us.
    /// If the workspace change is a change to a non-open document / project change, we trigger a fork from the workspace.
    /// </summary>
    private void OnWorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
    {
        lock (_gate)
        {
            var workspace = e.NewSolution.Workspace;
            if (e.Kind is WorkspaceChangeKind.DocumentChanged or WorkspaceChangeKind.AdditionalDocumentChanged or WorkspaceChangeKind.AnalyzerConfigDocumentChanged)
            {
                if (e.DocumentId == null || IsDocumentTrackedByLsp(e.DocumentId, e.NewSolution, _documentChangeTracker))
                {
                    // We're tracking the document already, no need to fork the workspace to get the changes, LSP will have sent them to us.
                    return;
                }
            }

            // Documents added/removed, closed document changes, any changes to the project, or any changes to the solution mean we need to re-fork from the workspace
            // to ensure that the lsp solution contains these updates.
            _workspaceToLspSolution[workspace] = null;
        }

        static bool IsDocumentTrackedByLsp(DocumentId changedDocumentId, Solution newWorkspaceSolution, IDocumentChangeTracker documentChangeTracker)
        {
            var changedDocument = newWorkspaceSolution.GetRequiredDocument(changedDocumentId);
            var documentUri = changedDocument.TryGetURI();

            if (documentUri != null && documentChangeTracker.IsTracking(documentUri))
            {
                return true;
            }

            return false;
        }
    }

    #endregion

    #region LSP Updates

    /// <summary>
    /// Called by the <see cref="DidOpenHandler"/> when a document is opened in LSP.
    /// </summary>
    public void TrackLspDocument(Uri uri, SourceText documentText)
    {
        lock (_gate)
        {
            var updatedSolutions = ForkAllWorkspaces();

            // If we can't find the document in any of the registered workspaces, add it to our loose files workspace.
            var findDocumentResult = FindDocument(uri, updatedSolutions, _lspMiscellaneousFilesWorkspace?.CurrentSolution, clientName: null, _requestTelemetryLogger, _logger);
            if (findDocumentResult == null)
            {
                _lspMiscellaneousFilesWorkspace?.AddMiscellaneousDocument(uri, documentText);
            }
        }
    }

    /// <summary>
    /// Called by the <see cref="DidCloseHandler"/> when a document is closed in LSP.
    /// </summary>
    public void StopTrackingLspDocument(Uri uri)
    {
        lock (_gate)
        {
            _ = ForkAllWorkspaces();

            // Remove from the lsp misc files workspace if it was added there.
            _lspMiscellaneousFilesWorkspace?.TryRemoveMiscellaneousDocument(uri);
        }
    }

    /// <summary>
    /// Called by the <see cref="DidChangeHandler"/> when a document's text is updated in LSP.
    /// </summary>
    public void UpdateLspDocument(Uri uri, SourceText newSourceText)
    {
        lock (_gate)
        {
            // Get our current solutions and re-fork from the workspace as needed.
            var updatedSolutions = UpdateAllSolutions();

            var findDocumentResult = FindDocument(uri, updatedSolutions, _lspMiscellaneousFilesWorkspace?.CurrentSolution, clientName: null, _requestTelemetryLogger, _logger);
            if (findDocumentResult == null)
            {
                // We didn't find this document in a registered workspace or in the misc workspace.
                // This can happen when workspace event processing is behind LSP and a new document was added that has not been reflected in the incremental LSP solution yet.
                //
                // This means that the LSP solution will be stale until the event is processed and will not be able to answer requests on this URI.
                // When the workspace event is processed, we will fork from the workspace and apply all missed incremental LSP text (stored in IDocumentChangeTracker)
                // to bring the incremental LSP solution up to date.
                return;
            }

            if (findDocumentResult.Value.LspSolution.Workspace == _lspMiscellaneousFilesWorkspace)
            {
                // We found the document in the lsp misc files workspace.  Update it with the latest text.
                _lspMiscellaneousFilesWorkspace.TryUpdateMiscellaneousDocument(uri, newSourceText);
                return;
            }

            // Update all the documents that have a matching uri with the new source text and store as our new incremental solution.
            var solution = GetSolutionWithReplacedDocuments(findDocumentResult.Value.LspSolution, (uri, newSourceText));
            _workspaceToLspSolution[solution.Workspace] = solution;

            // We found the document in a registered solution.  Remove it from the loose files workspace if it exists.
            _lspMiscellaneousFilesWorkspace?.TryRemoveMiscellaneousDocument(uri);
            return;
        }
    }

    #endregion

    #region LSP Solution Retrieval

    /// <summary>
    /// Returns the LSP solution associated with the workspace with the specified <see cref="_hostWorkspaceKind"/>.
    /// This is the solution used for LSP requests that pertain to the entire workspace, for example code search or workspace diagnostics.
    /// </summary>
    public Solution? TryGetHostLspSolution()
    {
        lock (_gate)
        {
            // Ensure we have the latest lsp solutions
            var updatedSolutions = UpdateAllSolutions();

            var hostWorkspaceSolution = updatedSolutions.FirstOrDefault(s => s.Workspace.Kind == _hostWorkspaceKind);
            return hostWorkspaceSolution;
        }
    }

    /// <summary>
    /// Returns a document with the LSP tracked text forked from the appropriate workspace solution. 
    /// </summary>
    public Document? GetLspDocument(TextDocumentIdentifier textDocumentIdentifier, string? clientName)
    {
        lock (_gate)
        {
            // Ensure we have the latest lsp solutions
            var currentLspSolutions = UpdateAllSolutions();

            // Search through the latest lsp solutions to find the document with matching uri and client name.
            var findDocumentResult = FindDocument(textDocumentIdentifier.Uri, currentLspSolutions, _lspMiscellaneousFilesWorkspace?.CurrentSolution, clientName, _requestTelemetryLogger, _logger);
            if (findDocumentResult != null)
            {
                // Filter the matching documents by project context.
                var documentInProjectContext = findDocumentResult.Value.MatchingDocuments.FindDocumentInProjectContext(textDocumentIdentifier);
                return documentInProjectContext;
            }

            return null;
        }
    }

    #endregion

    /// <summary>
    /// Helper to clear out the LSP incremental solution for all registered workspaces and re-fork from the workspace.
    /// Should be called under <see cref="_gate"/>
    /// </summary>
    private ImmutableArray<Solution> ForkAllWorkspaces()
    {
        var workspaces = _workspaceToLspSolution.Keys.ToImmutableArray();
        foreach (var workspace in workspaces)
        {
            _workspaceToLspSolution[workspace] = null;
        }

        return UpdateAllSolutions();
    }

    /// <summary>
    /// Helper to get LSP solutions for all the registered workspaces.
    /// If the incremental lsp solution is missing, this will re-fork from the workspace.
    /// Should be called under <see cref="_gate"/>
    /// </summary>
    private ImmutableArray<Solution> UpdateAllSolutions()
    {
        var workspacePairs = _workspaceToLspSolution.ToImmutableArray();
        using var updatedSolutions = TemporaryArray<Solution>.Empty;
        foreach (var pair in workspacePairs)
        {
            var incrementalSolution = pair.Value;
            var workspace = pair.Key;
            if (incrementalSolution == null)
            {
                // We have no incremental lsp solution, create a new one forked from the workspace with LSP tracked documents.
                incrementalSolution = GetSolutionWithReplacedDocuments(workspace.CurrentSolution, _documentChangeTracker.GetTrackedDocuments().ToArray());
                _workspaceToLspSolution[workspace] = incrementalSolution;
                updatedSolutions.Add(incrementalSolution);
            }
            else
            {
                updatedSolutions.Add(incrementalSolution);
            }
        }

        return updatedSolutions.ToImmutableAndClear();
    }

    private static (Solution LspSolution, ImmutableArray<Document> MatchingDocuments)? FindDocument(
        Uri uri,
        ImmutableArray<Solution> registeredSolutions,
        Solution? lspMiscFilesSolution,
        string? clientName,
        RequestTelemetryLogger telemetryLogger,
        ILspLogger logger)
    {
        logger.TraceInformation($"Finding document corresponding to {uri}");

        // First search the registered workspaces for documents with a matching URI.
        if (TryGetDocumentsForUri(uri, registeredSolutions, clientName, out var documents, out var solution))
        {
            telemetryLogger.UpdateFindDocumentTelemetryData(success: true, solution.Workspace.Kind);
            logger.TraceInformation($"{documents.Value.First().FilePath} found in workspace {solution.Workspace.Kind}");

            return (solution, documents.Value);
        }

        // If the document was not in a registered workspace, try to retrieve from the LSP misc files workspace.
        var miscDocuments = lspMiscFilesSolution?.GetDocuments(uri);
        if (lspMiscFilesSolution != null && miscDocuments?.Any() == true)
        {
            telemetryLogger.UpdateFindDocumentTelemetryData(success: true, lspMiscFilesSolution.Workspace.Kind);
            logger.TraceInformation($"{miscDocuments.Value.First().FilePath} found in LSP miscellaneous workspace");
            return (lspMiscFilesSolution, miscDocuments.Value);
        }

        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        var searchedWorkspaceKinds = string.Join(";", registeredSolutions.SelectAsArray(s => s.Workspace.Kind));
        logger.TraceError($"Could not find '{uri}' with client name '{clientName}'.  Searched {searchedWorkspaceKinds}");
        telemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);
        return null;

        static bool TryGetDocumentsForUri(
            Uri uri,
            ImmutableArray<Solution> registeredSolutions,
            string? clientName,
            [NotNullWhen(true)] out ImmutableArray<Document>? documents,
            [NotNullWhen(true)] out Solution? solution)
        {
            foreach (var registeredSolution in registeredSolutions)
            {
                var matchingDocuments = registeredSolution.GetDocuments(uri, clientName);
                if (matchingDocuments.Any())
                {
                    documents = matchingDocuments;
                    solution = registeredSolution;
                    return true;
                }
            }

            documents = null;
            solution = null;
            return false;
        }
    }

    /// <summary>
    /// Gets a solution that represents the workspace view of the world (as passed in via the solution parameter)
    /// but with document text for any open documents updated to match the LSP view of the world. This makes
    /// the LSP server the source of truth for all document text, but all other changes come from the workspace
    /// </summary>
    private static Solution GetSolutionWithReplacedDocuments(Solution solution, params (Uri DocumentUri, SourceText Text)[] documentsToReplace)
    {
        foreach (var (uri, text) in documentsToReplace)
        {
            var documentIds = solution.GetDocumentIds(uri);

            // The tracked document might not be a part of this solution.
            if (documentIds.Any())
            {
                solution = solution.WithDocumentText(documentIds, text);
            }
        }

        return solution;
    }

    internal TestAccessor GetTestAccessor()
            => new(this);

    internal readonly struct TestAccessor
    {
        private readonly LspWorkspaceManager _manager;

        public TestAccessor(LspWorkspaceManager manager)
            => _manager = manager;
        public LspMiscellaneousFilesWorkspace? GetLspMiscellaneousFilesWorkspace() => _manager._lspMiscellaneousFilesWorkspace;
    }
}
