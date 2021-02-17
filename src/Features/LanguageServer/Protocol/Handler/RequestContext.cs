// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.RequestExecutionQueue;
using Logger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Context for requests handled by <see cref="IRequestHandler"/>
    /// </summary>
    internal readonly struct RequestContext
    {
        public static RequestContext Create(
            TextDocumentIdentifier? textDocument,
            string? clientName,
            ILspLogger _logger,
            ClientCapabilities clientCapabilities,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            Dictionary<Workspace, (Solution workspaceSolution, Solution lspSolution)>? solutionCache,
            IDocumentChangeTracker? documentChangeTracker)
        {
            // Go through each registered workspace, find the solution that contains the document that
            // this request is for, and then updates it based on the state of the world as we know it, based on the
            // text content in the document change tracker.

            // Assume the first workspace registered is the main one
            var workspaceSolution = lspWorkspaceRegistrationService.GetAllRegistrations().First().CurrentSolution;
            Document? document = null;

            // If we were given a document, find it in whichever workspace it exists in
            if (textDocument is null)
            {
                _logger.TraceInformation("Request contained no document id");
            }
            else
            {
                // There are multiple possible solutions that we could be interested in, so we need to find the document
                // first and then get the solution from there. If we're not given a document, this will return the default
                // solution
                document = FindDocument(_logger, lspWorkspaceRegistrationService, textDocument, clientName);

                if (document is not null)
                {
                    // Where ever the document came from, thats the "main" solution for this request
                    workspaceSolution = document.Project.Solution;
                }
            }

            documentChangeTracker ??= new NoOpDocumentChangeTracker();

            var lspSolution = BuildLSPSolution(solutionCache, workspaceSolution, documentChangeTracker);

            // If we got a document back, we need pull it out of our updated solution so the handler is operating on the
            // latest document text.
            if (document != null)
            {
                document = lspSolution.GetRequiredDocument(document.Id);
            }

            return new RequestContext(lspSolution, _logger.TraceInformation, clientCapabilities, clientName, document, documentChangeTracker);
        }

        private static Document? FindDocument(ILspLogger logger, ILspWorkspaceRegistrationService lspWorkspaceRegistrationService, TextDocumentIdentifier textDocument, string? clientName)
        {
            logger.TraceInformation($"Finding document corresponding to {textDocument.Uri}");

            using var workspaceKinds = TemporaryArray<string?>.Empty;
            foreach (var workspace in lspWorkspaceRegistrationService.GetAllRegistrations())
            {
                workspaceKinds.Add(workspace.Kind);
                var documents = workspace.CurrentSolution.GetDocuments(textDocument.Uri, clientName);

                if (!documents.IsEmpty)
                {
                    var document = documents.FindDocumentInProjectContext(textDocument);
                    logger.TraceInformation($"Found document in workspace {workspace.Kind}: {document.FilePath}");

                    Logger.Log(FunctionId.FindDocumentInWorkspace, KeyValueLogMessage.Create(LogType.Trace, m =>
                    {
                        m["WorkspaceKind"] = workspace.Kind;
                        m["FoundInWorkspace"] = true;
                        m["DocumentUriHashCode"] = textDocument.Uri.GetHashCode();
                    }));

                    return document;
                }
            }

            var searchedWorkspaceKinds = string.Join(";", workspaceKinds.ToImmutableAndClear());
            logger.TraceWarning($"No document found after looking in {searchedWorkspaceKinds} workspaces, but request did contain a document uri");

            Logger.Log(FunctionId.FindDocumentInWorkspace, KeyValueLogMessage.Create(LogType.Trace, m =>
            {
                m["AvailableWorkspaceKinds"] = searchedWorkspaceKinds;
                m["FoundInWorkspace"] = false;
                m["DocumentUriHashCode"] = textDocument.Uri.GetHashCode();
            }));

            return null;
        }

        /// <summary>
        /// Gets the "LSP view of the world", either by forking the workspace solution and updating the documents we track
        /// or by simply returning our cached solution if it is still valid.
        /// </summary>
        private static Solution BuildLSPSolution(Dictionary<Workspace, (Solution workspaceSolution, Solution lspSolution)>? solutionCache, Solution workspaceSolution, IDocumentChangeTracker documentChangeTracker)
        {
            var workspace = workspaceSolution.Workspace;

            // If we have a cached solution we can use it, unless the workspace solution it was based on
            // is not the current one. 
            if (solutionCache is null ||
                !solutionCache.TryGetValue(workspace, out var cacheInfo) ||
                workspaceSolution != cacheInfo.workspaceSolution)
            {
                var lspSolution = GetSolutionWithReplacedDocuments(workspaceSolution, documentChangeTracker);

                if (solutionCache is not null)
                {
                    solutionCache[workspace] = (workspaceSolution, lspSolution);
                }

                return lspSolution;
            }

            return cacheInfo.lspSolution;
        }

        /// <summary>
        /// Gets a solution that represents the workspace view of the world (as passed in via the solution parameter)
        /// but with document text for any open documents updated to match the LSP view of the world. This makes
        /// the LSP server the source of truth for all document text, but all other changes come from the workspace
        /// </summary>
        private static Solution GetSolutionWithReplacedDocuments(Solution solution, IDocumentChangeTracker documentChangeTracker)
        {
            foreach (var (uri, text) in documentChangeTracker.GetTrackedDocuments())
            {
                var documentIds = solution.GetDocumentIds(uri);

                // We are tracking documents from multiple solutions, so this might not be one we care about
                if (!documentIds.IsEmpty)
                {
                    solution = solution.WithDocumentText(documentIds, text);
                }
            }

            return solution;
        }

        /// <summary>
        /// This will be null for non-mutating requests because they're not allowed to change documents
        /// </summary>
        private readonly IDocumentChangeTracker _documentChangeTracker;

        /// <summary>
        /// The solution state that the request should operate on.
        /// </summary>
        public readonly Solution Solution;

        /// <summary>
        /// The client capabilities for the request.
        /// </summary>
        public readonly ClientCapabilities ClientCapabilities;

        /// <summary>
        /// The LSP client making the request
        /// </summary>
        public readonly string? ClientName;

        /// <summary>
        /// The document that the request is for, if applicable. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>.
        /// </summary>
        public readonly Document? Document;

        /// <summary>
        /// Tracing object that can be used to log information about the status of requests.
        /// </summary>
        private readonly Action<string> _traceInformation;

        public RequestContext(
            Solution solution,
            Action<string> traceInformation,
            ClientCapabilities clientCapabilities,
            string? clientName,
            Document? document,
            IDocumentChangeTracker documentChangeTracker)
        {
            Document = document;
            Solution = solution;
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
            _documentChangeTracker = documentChangeTracker;
            _traceInformation = traceInformation;
        }

        /// <summary>
        /// Allows a mutating request to open a document and start it being tracked.
        /// </summary>
        public void StartTracking(Uri documentUri, SourceText initialText)
            => _documentChangeTracker.StartTracking(documentUri, initialText);

        /// <summary>
        /// Allows a mutating request to update the contents of a tracked document.
        /// </summary>
        public void UpdateTrackedDocument(Uri documentUri, SourceText changedText)
            => _documentChangeTracker.UpdateTrackedDocument(documentUri, changedText);

        /// <summary>
        /// Allows a mutating request to close a document and stop it being tracked.
        /// </summary>
        public void StopTracking(Uri documentUri)
            => _documentChangeTracker.StopTracking(documentUri);

        public bool IsTracking(Uri documentUri)
            => _documentChangeTracker.IsTracking(documentUri);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public void TraceInformation(string message)
            => _traceInformation(message);

        private class NoOpDocumentChangeTracker : IDocumentChangeTracker
        {
            public IEnumerable<(Uri DocumentUri, SourceText Text)> GetTrackedDocuments()
                => Enumerable.Empty<(Uri DocumentUri, SourceText Text)>();

            public bool IsTracking(Uri documentUri) => false;
            public void StartTracking(Uri documentUri, SourceText initialText) { }
            public void StopTracking(Uri documentUri) { }
            public void UpdateTrackedDocument(Uri documentUri, SourceText text) { }
        }
    }
}
