// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using CommonLanguageServerProtocol.Framework;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.>
    /// Context for requests handled by <see cref="IMethodHandler"/>
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

        private readonly ILspServices _lspServices;

        /// <summary>
        /// The solution state that the request should operate on, if the handler requires an LSP solution, or <see langword="null"/> otherwise
        /// </summary>
        public readonly Solution? Solution;

        /// <summary>
        /// The client capabilities for the request.
        /// </summary>
        public readonly ClientCapabilities? ClientCapabilities;

        /// <summary>
        /// The LSP server handling the request.
        /// </summary>
        public readonly string ServerKind;

        /// <summary>
        /// The document that the request is for, if applicable. This comes from the <see cref="TextDocumentIdentifier"/> returned from the handler itself via a call to <see cref="IRequestHandler{RequestType, ResponseType, TResponseContextType}.GetTextDocumentIdentifier(RequestType)"/>.
        /// </summary>
        public readonly Document? Document;

        /// <summary>
        /// The languages supported by the server making the request.
        /// </summary>
        public readonly ImmutableArray<string> SupportedLanguages;

        public readonly CancellationToken QueueCancellationToken;

        /// <summary>
        /// Tracing object that can be used to log information about the status of requests.
        /// </summary>
        private readonly ILspLogger _logger;

        public RequestContext(
            Solution? solution,
            ILspLogger logger,
            ClientCapabilities? clientCapabilities,
            string serverKind,
            Document? document,
            IDocumentChangeTracker documentChangeTracker,
            ImmutableDictionary<Uri, SourceText> trackedDocuments,
            ImmutableArray<string> supportedLanguages,
            ILspServices lspServices,
            CancellationToken queueCancellationToken)
        {
            Document = document;
            Solution = solution;
            ClientCapabilities = clientCapabilities;
            ServerKind = serverKind;
            SupportedLanguages = supportedLanguages;
            _documentChangeTracker = documentChangeTracker;
            _logger = logger;
            _trackedDocuments = trackedDocuments;
            _lspServices = lspServices;
            QueueCancellationToken = queueCancellationToken;
        }

        public static async Task<RequestContext> CreateAsync(
            bool mutatesSolutionState,
            TextDocumentIdentifier? textDocument,
            string serverKind,
            ClientCapabilities? clientCapabilities,
            ImmutableArray<string> supportedLanguages,
            ILspServices lspServices,
            ILspLogger logger,
            CancellationToken cancellationToken)
        {
            var lspWorkspaceManager = lspServices.GetRequiredService<LspWorkspaceManager>();
            var documentChangeTracker = mutatesSolutionState ? (IDocumentChangeTracker)lspWorkspaceManager : new NonMutatingDocumentChangeTracker();

            // Retrieve the current LSP tracked text as of this request.
            // This is safe as all creation of request contexts cannot happen concurrently.
            var trackedDocuments = lspWorkspaceManager.GetTrackedLspText();

            Solution? workspaceSolution;
            Document? document = null;
            if (textDocument is not null)
            {
                // we were given a request associated with a document.  Find the corresponding roslyn document for this. 
                // There are certain cases where we may be asked for a document that does not exist (for example a document is removed)
                // For example, document pull diagnostics can ask us after removal to clear diagnostics for a document.
                document = await lspWorkspaceManager.GetLspDocumentAsync(textDocument, cancellationToken).ConfigureAwait(false);
            }

            workspaceSolution = document?.Project.Solution ?? await lspWorkspaceManager.TryGetHostLspSolutionAsync(cancellationToken).ConfigureAwait(false);

            var context = new RequestContext(
                workspaceSolution,
                logger,
                clientCapabilities,
                serverKind,
                document,
                documentChangeTracker,
                trackedDocuments,
                supportedLanguages,
                lspServices,
                cancellationToken);
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
        public Task TraceInformationAsync(string message, CancellationToken cancellationToken)
            => _logger.LogInformationAsync(message, cancellationToken);

        public Task TraceWarningAsync(string message, CancellationToken cancellationToken)
            => _logger.LogWarningAsync(message, cancellationToken);

        public Task TraceErrorAsync(string message, CancellationToken cancellationToken)
            => _logger.LogErrorAsync(message, cancellationToken);

        public Task TraceExceptionAsync(Exception exception, CancellationToken cancellationToken)
            => _logger.LogExceptionAsync(exception, cancellationToken: cancellationToken);

        public T GetRequiredLspService<T>() where T : class, ILspService
        {
            return _lspServices.GetRequiredService<T>();
        }

        public T GetRequiredService<T>() where T : class
        {
            return _lspServices.GetRequiredService<T>();
        }
    }
}
