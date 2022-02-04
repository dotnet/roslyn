// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
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
        /// This will be the <see cref="NonMutatingDocumentChangeTracker"/> for non-mutating requests because they're not allowed to change documents
        /// </summary>
        private readonly IDocumentChangeTracker _documentChangeTracker;

        /// <summary>
        /// Contains the LSP text for all opened LSP documents from when this request was processed in the queue.
        /// </summary>
        /// <remarks>
        /// This is a snapshot of the source text that reflects the LSP text based on the order of this request in the queue.
        /// It contains text that is consistent with all prior LSP text sync notifications, but LSP text sync requests
        /// which are ordered after this one in the queue are not reflected here.
        /// </remarks>
        private readonly ImmutableDictionary<Uri, SourceText> _trackedDocuments;

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
        private readonly ILspLogger _logger;

        public RequestContext(
            Solution? solution,
            ILspLogger logger,
            ClientCapabilities clientCapabilities,
            string? clientName,
            Document? document,
            IDocumentChangeTracker documentChangeTracker,
            ImmutableDictionary<Uri, SourceText> trackedDocuments,
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
            _logger = logger;
            _trackedDocuments = trackedDocuments;
        }

        public static RequestContext? Create(
            bool requiresLSPSolution,
            TextDocumentIdentifier? textDocument,
            string? clientName,
            ILspLogger logger,
            ClientCapabilities clientCapabilities,
            LspWorkspaceManager lspWorkspaceManager,
            IDocumentChangeTracker documentChangeTracker,
            ImmutableArray<string> supportedLanguages,
            IGlobalOptionService globalOptions)
        {
            // Retrieve the current LSP tracked text as of this request.
            // This is safe as all creation of request contexts cannot happen concurrently.
            var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();

            // If the handler doesn't need an LSP solution we do two important things:
            // 1. We don't bother building the LSP solution for perf reasons
            // 2. We explicitly don't give the handler a solution or document, even if we could
            //    so they're not accidentally operating on stale solution state.
            if (!requiresLSPSolution)
            {
                return new RequestContext(solution: null, logger, clientCapabilities, clientName, document: null, documentChangeTracker, trackedDocuments, supportedLanguages, globalOptions);
            }

            // Go through each registered workspace, find the solution that contains the document that
            // this request is for, and then updates it based on the state of the world as we know it, based on the
            // text content in the document change tracker.

            Document? document = null;
            var workspaceSolution = lspWorkspaceManager.TryGetHostLspSolution();
            if (textDocument is not null)
            {
                // we were given a request associated with a document.  Find the corresponding roslyn
                // document for this.  If we can't, we cannot proceed.
                document = lspWorkspaceManager.GetLspDocument(textDocument, clientName);
                if (document != null)
                    workspaceSolution = document.Project.Solution;
            }

            if (workspaceSolution == null)
            {
                logger.TraceError("Could not find appropriate solution for operation");
                return null;
            }

            var context = new RequestContext(
                workspaceSolution,
                logger,
                clientCapabilities,
                clientName,
                document,
                documentChangeTracker,
                trackedDocuments,
                supportedLanguages,
                globalOptions);
            return context;
        }

        /// <summary>
        /// Allows a mutating request to open a document and start it being tracked.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void StartTracking(Uri uri, SourceText initialText)
            => _documentChangeTracker.StartTracking(uri, initialText);

        /// <summary>
        /// Allows a mutating request to update the contents of a tracked document.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void UpdateTrackedDocument(Uri uri, SourceText changedText)
            => _documentChangeTracker.UpdateTrackedDocument(uri, changedText);

        public SourceText GetTrackedDocumentSourceText(Uri documentUri)
        {
            Contract.ThrowIfFalse(_trackedDocuments.ContainsKey(documentUri), $"Attempted to get text for {documentUri} which is not open.");
            return _trackedDocuments[documentUri];
        }

        /// <summary>
        /// Allows a mutating request to close a document and stop it being tracked.
        /// Mutating requests are serialized by the execution queue in order to prevent concurrent access.
        /// </summary>
        public void StopTracking(Uri uri)
            => _documentChangeTracker.StopTracking(uri);

        public bool IsTracking(Uri documentUri)
            => _trackedDocuments.ContainsKey(documentUri);

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        public void TraceInformation(string message)
            => _logger.TraceInformation(message);

        public void TraceWarning(string message)
            => _logger.TraceWarning(message);

        public void TraceError(string message)
            => _logger.TraceError(message);

        public void TraceException(Exception exception)
            => _logger.TraceException(exception);
    }
}
