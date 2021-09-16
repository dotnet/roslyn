// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
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

        public static RequestContext Create(
            bool requiresLSPSolution,
            TextDocumentIdentifier? textDocument,
            string? clientName,
            ILspLogger logger,
            RequestTelemetryLogger telemetryLogger,
            ClientCapabilities clientCapabilities,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            Dictionary<Workspace, (Solution workspaceSolution, Solution lspSolution)>? solutionCache,
            IDocumentChangeTracker? documentChangeTracker,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions,
            out Workspace workspace)
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
                logger.TraceInformation("Request contained no text document identifier");
            }
            else
            {
                // There are multiple possible solutions that we could be interested in, so we need to find the document
                // first and then get the solution from there. If we're not given a document, this will return the default
                // solution
                document = FindDocument(logger, telemetryLogger, lspWorkspaceRegistrationService, textDocument, clientName);

                if (document is not null)
                {
                    // Where ever the document came from, thats the "main" solution for this request
                    workspaceSolution = document.Project.Solution;
                }
            }

            documentChangeTracker ??= new NoOpDocumentChangeTracker();

            // If the handler doesn't need an LSP solution we do two important things:
            // 1. We don't bother building the LSP solution for perf reasons
            // 2. We explicitly don't give the handler a solution or document, even if we could
            //    so they're not accidentally operating on stale solution state.
            if (!requiresLSPSolution)
            {
                workspace = workspaceSolution.Workspace;
                return new RequestContext(solution: null, logger.TraceInformation, clientCapabilities, clientName, document: null, documentChangeTracker, supportedLanguages, globalOptions);
            }

            var lspSolution = BuildLSPSolution(solutionCache, workspaceSolution, documentChangeTracker);

            // If we got a document back, we need pull it out of our updated solution so the handler is operating on the
            // latest document text.
            if (document != null)
            {
                document = lspSolution.GetRequiredDocument(document.Id);
            }

            workspace = lspSolution.Workspace;
            return new RequestContext(lspSolution, logger.TraceInformation, clientCapabilities, clientName, document, documentChangeTracker, supportedLanguages, globalOptions);
        }

        private static Document? FindDocument(
            ILspLogger logger,
            RequestTelemetryLogger telemetryLogger,
            ILspWorkspaceRegistrationService lspWorkspaceRegistrationService,
            TextDocumentIdentifier textDocument,
            string? clientName)
        {
            logger.TraceInformation($"Finding document corresponding to {textDocument.Uri}");

            using var workspaceKinds = TemporaryArray<string?>.Empty;
            foreach (var workspace in lspWorkspaceRegistrationService.GetAllRegistrations())
            {
                workspaceKinds.Add(workspace.Kind);
                var documents = workspace.CurrentSolution.GetDocuments(textDocument.Uri, clientName, logger);

                if (!documents.IsEmpty)
                {
                    var document = documents.FindDocumentInProjectContext(textDocument);
                    logger.TraceInformation($"Found document in workspace {workspace.Kind}: {document.FilePath}");
                    telemetryLogger.UpdateFindDocumentTelemetryData(success: true, workspace.Kind);
                    return document;
                }
            }

            var searchedWorkspaceKinds = string.Join(";", workspaceKinds.ToImmutableAndClear());
            logger.TraceWarning($"No document found for '{textDocument.Uri}' after looking in {searchedWorkspaceKinds} workspaces, with client name '{clientName}'.");

            telemetryLogger.UpdateFindDocumentTelemetryData(success: false, workspaceKind: null);

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
