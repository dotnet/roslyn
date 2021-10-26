// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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
/// <remarks>
/// This is built to store incremental solutions that we update based on LSP text changes. This solution is <b>eventually consistent</b> with the workspace:
/// <list type="bullet">
///   <item> When LSP text changes come in, we only fork the relevant document. </item>
///   <item> We listen to workspace events. When we receive an event that is not for an LSP open document,
///          we delete our incremental LSP solution so that we can fork the workspace with all open LSP documents. </item>
/// </list>
///
/// Doing incremental forking like this is more complex, but has a few nice properties:
/// <list type="bullet">
///   <item>LSP didChange events only cause us to update the document that changed, not all open documents.</item>
///   <item>Since we incrementally update our LSP documents, we only have to re-parse the document that changed.</item>
///   <item>Since we incrementally update our LSP documents, project versions for other open documents remain unchanged.</item>
///   <item>We are not reliant on the workspace being updated frequently (which it is not in VSCode) to do checksum diffing between LSP and the workspace.</item>
/// </list>
/// </remarks>
internal partial class LspWorkspaceManager : IDisposable
{
    /// <summary>
    /// Lock to gate access to the <see cref="_workspaceToLspSolution"/>.
    /// Access from the LSP server is serial as the LSP queue is processed serially until
    /// after we give the solution to the request handlers.  However workspace events can interleave
    /// so we must protect against concurrent access here.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// A map from the registered workspace to the running lsp solution with incremental changes from LSP
    /// text sync applied to it.
    /// All workspaces registered by the <see cref="LspWorkspaceRegistrationService"/> are added as keys to this dictionary
    /// with a null value (solution) initially.  As LSP text changes come in, we create the incremental solution from the workspace and update it with changes.
    /// When we detect a change that requires us to re-fork the LSP solution from the workspace we null out the solution for the key.
    /// </summary>
    private readonly Dictionary<Workspace, Solution?> _workspaceToLspSolution = new();

    private readonly string _hostWorkspaceKind;
    private readonly ILspLogger _logger;
    private readonly IDocumentChangeTracker _documentChangeTracker;
    private readonly LspMiscellaneousFilesWorkspace? _lspMiscellaneousFilesWorkspace;
    private readonly RequestTelemetryLogger _requestTelemetryLogger;
    private readonly LspWorkspaceRegistrationService _lspWorkspaceRegistrationService;

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

        _lspWorkspaceRegistrationService = lspWorkspaceRegistrationService;
        lspWorkspaceRegistrationService.WorkspaceRegistered += OnWorkspaceRegistered;
        // This server may have been started after workspaces were registered, we need to ensure we know about them.
        foreach (var workspace in lspWorkspaceRegistrationService.GetAllRegistrations())
        {
            OnWorkspaceRegistered(this, new LspWorkspaceRegisteredEventArgs(workspace));
        }
    }

    public void Dispose()
    {
        // Workspace events can come in while we're disposing of an LSP server (e.g. restart).
        lock (_gate)
        {
            _lspWorkspaceRegistrationService.WorkspaceRegistered -= OnWorkspaceRegistered;
            foreach (var registeredWorkspace in _workspaceToLspSolution.Keys)
            {
                registeredWorkspace.WorkspaceChanged -= OnWorkspaceChanged;
            }

            _workspaceToLspSolution.Clear();
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
            // Set the LSP solution for the workspace to null so that when asked we fork from the workspace.
            _workspaceToLspSolution.Add(e.Workspace, null);
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
        var workspace = e.NewSolution.Workspace;
        if (e.Kind is WorkspaceChangeKind.DocumentChanged or WorkspaceChangeKind.AdditionalDocumentChanged or WorkspaceChangeKind.AnalyzerConfigDocumentChanged)
        {
            Contract.ThrowIfNull(e.DocumentId, $"DocumentId missing for document change event {e.Kind}");
            if (IsDocumentTrackedByLsp(e.DocumentId, e.NewSolution, _documentChangeTracker))
            {
                // We're tracking the document already, no need to fork the workspace to get the changes, LSP will have sent them to us.
                return;
            }
        }

        lock (_gate)
        {
            // Documents added/removed, closed document changes, any changes to the project, or any changes to the solution mean we need to re-fork from the workspace
            // to ensure that the lsp solution contains these updates.
            _workspaceToLspSolution[workspace] = null;
        }

        static bool IsDocumentTrackedByLsp(DocumentId changedDocumentId, Solution newWorkspaceSolution, IDocumentChangeTracker documentChangeTracker)
        {
            var changedDocument = newWorkspaceSolution.GetRequiredDocument(changedDocumentId);
            var documentUri = changedDocument.TryGetURI();

            return documentUri != null && documentChangeTracker.IsTracking(documentUri);
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
            var updatedSolutions = ResetIncrementalLspSolutions_CalledUnderLock();

            if (updatedSolutions.Any(solution => solution.GetDocuments(uri).Any()))
            {
                return;
            }

            // If we can't find the document in any of the registered workspaces, add it to our loose files workspace.
            var miscDocument = _lspMiscellaneousFilesWorkspace?.AddMiscellaneousDocument(uri, documentText);
            if (miscDocument != null)
            {
                _workspaceToLspSolution[miscDocument.Project.Solution.Workspace] = miscDocument.Project.Solution;
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
            // Trigger a fork of all workspaces, the LSP document text may not be correct anymore and this document
            // may have been removed / moved to a different workspace.
            _ = ResetIncrementalLspSolutions_CalledUnderLock();

            // If the file was added to the LSP misc files workspace, it needs to be removed as we no longer care about it once closed.
            // If the misc document ended up being added to an actual workspace, we may have already removed it from LSP misc in UpdateLspDocument.
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
            var updatedSolutions = ComputeIncrementalLspSolutions_CalledUnderLock();

            var findDocumentResult = FindDocuments(uri, updatedSolutions, clientName: null, _requestTelemetryLogger, _logger);
            if (findDocumentResult.IsEmpty)
            {
                // We didn't find this document in a registered workspace or in the misc workspace.
                // This can happen when workspace event processing is behind LSP and a new document was added that has not been reflected in the incremental LSP solution yet.
                //
                // This means that the LSP solution will be stale until the event is processed and will not be able to answer requests on this URI.
                // When the workspace event is processed, we will fork from the workspace and apply all missed incremental LSP text (stored in IDocumentChangeTracker)
                // to bring the incremental LSP solution up to date.
                //
                // TODO - Once we always create the misc files workspace we should never hit this path (when workspace events are behind the doc will go into misc).
                // Remove as part of https://github.com/dotnet/roslyn/issues/57243
                return;
            }

            // Update all the documents that have a matching uri with the new source text and store as our new incremental solution.
            // We can have multiple documents with the same URI (e.g. linked documents).
            var solution = GetSolutionWithReplacedDocuments(findDocumentResult.First().Project.Solution, ImmutableArray.Create((uri, newSourceText)));
            _workspaceToLspSolution[solution.Workspace] = solution;

            if (solution.Workspace is not LspMiscellaneousFilesWorkspace && _lspMiscellaneousFilesWorkspace != null)
            {
                // If we found the uri in a workspace that isn't LSP misc files, we can remove it from the lsp misc files if it is there.
                //
                // Example: In VSCode, the server is started and file opened before the user has chosen which project/sln in the folder they want to open,
                // and therefore on didOpen the file gets put into LSP misc files.
                //
                // Once the workspace is updated with the document however, we proactively cleanup the lsp misc files workspace here
                // so that we don't keep lingering references around.
                _lspMiscellaneousFilesWorkspace.TryRemoveMiscellaneousDocument(uri);
            }
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
            var updatedSolutions = ComputeIncrementalLspSolutions_CalledUnderLock();

            var hostWorkspaceSolution = updatedSolutions.FirstOrDefault(s => s.Workspace.Kind == _hostWorkspaceKind);
            return hostWorkspaceSolution;
        }
    }

    /// <summary>
    /// Returns a document with the LSP tracked text forked from the appropriate workspace solution.
    /// </summary>
    /// <param name="clientName">
    /// Returns documents that have a matching client name.  In razor scenarios this is to ensure that the Razor C# server
    /// only provides data for generated razor documents (which have a client name).
    /// </param>
    public Document? GetLspDocument(TextDocumentIdentifier textDocumentIdentifier, string? clientName)
    {
        lock (_gate)
        {
            // Ensure we have the latest lsp solutions
            var currentLspSolutions = ComputeIncrementalLspSolutions_CalledUnderLock();

            // Search through the latest lsp solutions to find the document with matching uri and client name.
            var findDocumentResult = FindDocuments(textDocumentIdentifier.Uri, currentLspSolutions, clientName, _requestTelemetryLogger, _logger);
            if (findDocumentResult.IsEmpty)
            {
                return null;
            }

            // Filter the matching documents by project context.
            var documentInProjectContext = findDocumentResult.FindDocumentInProjectContext(textDocumentIdentifier);
            return documentInProjectContext;
        }
    }

    #endregion

    /// <summary>
    /// Helper to clear out the LSP incremental solution for all registered workspaces and re-fork from the workspace.
    /// Should be called under <see cref="_gate"/>
    /// </summary>
    private ImmutableArray<Solution> ResetIncrementalLspSolutions_CalledUnderLock()
    {
        Contract.ThrowIfFalse(Monitor.IsEntered(_gate));

        var workspaces = _workspaceToLspSolution.Keys.ToImmutableArray();
        foreach (var workspace in workspaces)
        {
            _workspaceToLspSolution[workspace] = null;
        }

        return ComputeIncrementalLspSolutions_CalledUnderLock();
    }

    /// <summary>
    /// Helper to get LSP solutions for all the registered workspaces.
    /// If the incremental lsp solution is missing, this will re-fork from the workspace.
    /// Should be called under <see cref="_gate"/>
    /// </summary>
    private ImmutableArray<Solution> ComputeIncrementalLspSolutions_CalledUnderLock()
    {
        Contract.ThrowIfFalse(Monitor.IsEntered(_gate));

        var workspacePairs = _workspaceToLspSolution.ToImmutableArray();
        using var updatedSolutions = TemporaryArray<Solution>.Empty;
        foreach (var (workspace, incrementalSolution) in workspacePairs)
        {
            if (incrementalSolution == null)
            {
                // We have no incremental lsp solution, create a new one forked from the workspace with LSP tracked documents.
                var newIncrementalSolution = GetSolutionWithReplacedDocuments(workspace.CurrentSolution, _documentChangeTracker.GetTrackedDocuments());
                _workspaceToLspSolution[workspace] = newIncrementalSolution;
                updatedSolutions.Add(newIncrementalSolution);
            }
            else
            {
                updatedSolutions.Add(incrementalSolution);
            }
        }

        return updatedSolutions.ToImmutableAndClear();
    }

    /// <summary>
    /// Looks for document(s - e.g. linked docs) from a single solution matching the input URI in the set of passed in solutions.
    /// 
    /// Client name is used in razor cases to filter out non-razor documents.  However once we switch fully over to pull
    /// diagnostics, the client should only ever ask the razor server about razor documents, so we may be able to remove it here.
    /// </summary>
    private static ImmutableArray<Document> FindDocuments(
        Uri uri,
        ImmutableArray<Solution> registeredSolutions,
        string? clientName,
        RequestTelemetryLogger telemetryLogger,
        ILspLogger logger)
    {
        logger.TraceInformation($"Finding document corresponding to {uri}");

        // Ensure we search the lsp misc files solution last if it is present.
        registeredSolutions = registeredSolutions
            .Where(solution => solution.Workspace is not LspMiscellaneousFilesWorkspace)
            .Concat(registeredSolutions.Where(solution => solution.Workspace is LspMiscellaneousFilesWorkspace)).ToImmutableArray();

        // First search the registered workspaces for documents with a matching URI.
        if (TryGetDocumentsForUri(uri, registeredSolutions, clientName, out var documents, out var solution))
        {
            telemetryLogger.UpdateFindDocumentTelemetryData(success: true, solution.Workspace.Kind);
            logger.TraceInformation($"{documents.Value.First().FilePath} found in workspace {solution.Workspace.Kind}");

            return documents.Value;
        }

        // We didn't find the document in any workspace, record a telemetry notification that we did not find it.
        var searchedWorkspaceKinds = string.Join(";", registeredSolutions.SelectAsArray(s => s.Workspace.Kind));
        logger.TraceError($"Could not find '{uri}' with client name '{clientName}'.  Searched {searchedWorkspaceKinds}");
        telemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);
        return ImmutableArray<Document>.Empty;

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
    private static Solution GetSolutionWithReplacedDocuments(Solution solution, ImmutableArray<(Uri DocumentUri, SourceText Text)> documentsToReplace)
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

        public LspMiscellaneousFilesWorkspace? GetLspMiscellaneousFilesWorkspace()
            => _manager._lspMiscellaneousFilesWorkspace;

        public Dictionary<Workspace, Solution?> GetWorkspaceState()
            => _manager._workspaceToLspSolution;
    }
}
