// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly DocumentChangeTracker? _documentChangeTracker;

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

        public RequestContext(Solution solution, ClientCapabilities clientCapabilities, string? clientName, Document? document, DocumentChangeTracker? documentChangeTracker)
        {
            Document = document;
            Solution = solution;
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
            _documentChangeTracker = documentChangeTracker;
        }

        /// <summary>
        /// Allows a mutating request to open a document and start it being tracked.
        /// </summary>
        public void StartTracking(Uri documentUri, SourceText initialText)
        {
            Contract.ThrowIfNull(_documentChangeTracker, "Mutating documents not allowed in a non-mutating request handler");

            _documentChangeTracker.StartTracking(documentUri, initialText);
        }

        /// <summary>
        /// Allows a mutating request to update the contents of a tracked document.
        /// </summary>
        public void UpdateTrackedDocument(Uri documentUri, SourceText changedText)
        {
            Contract.ThrowIfNull(_documentChangeTracker, "Mutating documents not allowed in a non-mutating request handler");

            _documentChangeTracker.UpdateTrackedDocument(documentUri, changedText);
        }

        /// <summary>
        /// Allows a mutating request to close a document and stop it being tracked.
        /// </summary>
        public void StopTracking(Uri documentUri)
        {
            Contract.ThrowIfNull(_documentChangeTracker, "Mutating documents not allowed in a non-mutating request handler");

            _documentChangeTracker.StopTracking(documentUri);
        }
    }
}
