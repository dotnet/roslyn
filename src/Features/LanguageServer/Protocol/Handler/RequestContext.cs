// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Context for requests handled by <see cref="IRequestHandler"/>
    /// </summary>
    internal readonly struct RequestContext
    {
        private readonly Action<Solution>? _solutionUpdater;

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

        public RequestContext(Solution solution, ClientCapabilities clientCapabilities, string? clientName, Document? document, Action<Solution>? solutionUpdater)
        {
            Document = document;
            Solution = solution;
            _solutionUpdater = solutionUpdater;
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
        }

        /// <summary>
        /// Allows a mutating request to provide a new solution snapshot that all subsequent requests should use.
        /// </summary>
        public void UpdateSolution(Solution solution)
        {
            Contract.ThrowIfNull(_solutionUpdater, "Mutating solution not allowed in a non-mutating request handler");
            _solutionUpdater.Invoke(solution);
        }
    }
}
