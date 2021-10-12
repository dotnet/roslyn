// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.RequestExecutionQueue;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Context for requests handled by <see cref="IRequestHandler"/>
    /// </summary>
    internal readonly struct RequestContext
    {
        /// <summary>
        /// This will be null for non-mutating requests because they're not allowed to change documents
        /// </summary>
        private readonly IDocumentChangeTracker _documentChangeTracker;

        /// <summary>
        /// The solution state that the request should operate on, if the handler requires an LSP solution, or <see langword="null"/> otherwise
        /// </summary>
        public readonly Solution? Solution;

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
        /// The languages supported by the server making the request.
        /// </summary>
        public readonly ImmutableArray<string> SupportedLanguages;

        public readonly IGlobalOptionService GlobalOptions;

        /// <summary>
        /// Tracing object that can be used to log information about the status of requests.
        /// </summary>
        private readonly Action<string> _traceInformation;

        public RequestContext(
            Solution? solution,
            Action<string> traceInformation,
            ClientCapabilities clientCapabilities,
            string? clientName,
            Document? document,
            IDocumentChangeTracker documentChangeTracker,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions)
        {
            Document = document;
            Solution = solution;
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
            SupportedLanguages = supportedLanguages;
            GlobalOptions = globalOptions;
            _documentChangeTracker = documentChangeTracker;
            _traceInformation = traceInformation;
        }

        public static (RequestContext? context, Workspace? workspace) Create(
            bool requiresLSPSolution,
            TextDocumentIdentifier? textDocument,
            string? clientName,
            ILspLogger logger,
            RequestTelemetryLogger telemetryLogger,
            ClientCapabilities clientCapabilities,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspMiscellaneousFilesWorkspace? lspMiscellaneousFilesWorkspace,
            Dictionary<Workspace, (Solution workspaceSolution, Solution lspSolution)>? solutionCache,
            IDocumentChangeTracker? documentChangeTracker,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions)
        {
            // Go through each registered workspace, find the solution that contains the document that
            // this request is for, and then updates it based on the state of the world as we know it, based on the
            // text content in the document change tracker.

            Document? document = null;
            var workspaceSolution = lspWorkspaceRegistrationService.TryGetHostWorkspace()?.CurrentSolution;
            if (textDocument is not null)
            {
                // we were given a request associated with a document.  Find the corresponding roslyn
                // document for this.  If we can't, we cannot proceed.
                document = FindDocument(logger, telemetryLogger, lspWorkspaceRegistrationService, lspMiscellaneousFilesWorkspace, textDocument, clientName);
                if (document != null)
                    workspaceSolution = document.Project.Solution;
            }

            if (workspaceSolution == null)
            {
                logger.TraceError("Could not find appropriate solution for operation");
                return default;
            }

            documentChangeTracker ??= new NoOpDocumentChangeTracker();

            // If the handler doesn't need an LSP solution we do two important things:
            // 1. We don't bother building the LSP solution for perf reasons
            // 2. We explicitly don't give the handler a solution or document, even if we could
            //    so they're not accidentally operating on stale solution state.
            if (!requiresLSPSolution)
            {
                var context = new RequestContext(solution: null, logger.TraceInformation, clientCapabilities, clientName, document: null, documentChangeTracker, supportedLanguages, globalOptions);
                return (context, workspaceSolution.Workspace);
            }
            else
            {
                var lspSolution = BuildLSPSolution(solutionCache, workspaceSolution, documentChangeTracker);

                // If we got a document back, we need pull it out of our updated solution so the handler is operating on the
                // latest document text.
                if (document != null)
                    document = lspSolution.GetRequiredDocument(document.Id);

                var context = new RequestContext(lspSolution, logger.TraceInformation, clientCapabilities, clientName, document, documentChangeTracker, supportedLanguages, globalOptions);
                return (context, lspSolution.Workspace);
            }
        }

        private static Document? FindDocument(
            ILspLogger logger,
            RequestTelemetryLogger telemetryLogger,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            LspMiscellaneousFilesWorkspace? lspMiscFilesWorkspace,
            TextDocumentIdentifier textDocument,
            string? clientName)
        {
            logger.TraceInformation($"Finding document corresponding to {textDocument.Uri}");

            var document = TryGetDocumentFromRegisteredWorkspaces(textDocument, clientName, lspWorkspaceRegistrationService, telemetryLogger, logger);
            if (document != null)
            {
                return document;
            }

            // If the document was not in a registered workspace, try to retrieve from the LSP misc files workspace.
            if (lspMiscFilesWorkspace is not null)
            {
                document = TryGetDocumentFromWorkspace(textDocument, clientName, lspMiscFilesWorkspace);
            }

            return document;

            static Document? TryGetDocumentFromRegisteredWorkspaces(
                TextDocumentIdentifier textDocument,
                string? clientName,
                ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
                RequestTelemetryLogger telemetryLogger,
                ILspLogger logger)
            {
                var registeredWorkspaces = lspWorkspaceRegistrationService.GetAllRegistrations();
                foreach (var workspace in registeredWorkspaces)
                {
                    var document = TryGetDocumentFromWorkspace(textDocument, clientName, workspace);
                    if (document != null)
                    {
                        telemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspace.Kind);
                        logger.TraceInformation($"Using document from workspace {workspace.Kind}: {document.FilePath}");
                        return document;
                    }
                }

                // We didn't find the document in a registered workspace, record a telemetry notification that we did not find it.
                var searchedWorkspaceKinds = string.Join(";", registeredWorkspaces.SelectAsArray(w => w.Kind));
                logger.TraceWarning($"Creating a miscellaneous file for '{textDocument.Uri}' as it was found in a registered workspace (searched {searchedWorkspaceKinds}, with client name '{clientName}'.");
                telemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);

                return null;
            }

            static Document? TryGetDocumentFromWorkspace(TextDocumentIdentifier identifier, string? clientName, Workspace workspace)
            {
                var documents = workspace.CurrentSolution.GetDocuments(identifier.Uri, clientName);

                if (!documents.IsEmpty)
                {
                    var document = documents.FindDocumentInProjectContext(identifier);
                    return document;
                }

                return null;
            }
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
        /// Allows a mutating request to open a document and start it being tracked.
        /// </summary>
        public void StartTracking(Uri documentUri, SourceText initialText)
            => _documentChangeTracker.StartTracking(documentUri, initialText);

        /// <summary>
        /// Allows a mutating request to update the contents of a tracked document.
        /// </summary>
        public void UpdateTrackedDocument(Uri documentUri, SourceText changedText)
            => _documentChangeTracker.UpdateTrackedDocument(documentUri, changedText);

        public SourceText GetTrackedDocumentSourceText(Uri documentUri)
            => _documentChangeTracker.GetTrackedDocumentSourceText(documentUri);

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

            public SourceText GetTrackedDocumentSourceText(Uri documentUri) => null!;

            public bool IsTracking(Uri documentUri) => false;
            public void StartTracking(Uri documentUri, SourceText initialText) { }
            public void StopTracking(Uri documentUri) { }
            public void UpdateTrackedDocument(Uri documentUri, SourceText text) { }
        }
    }
}
