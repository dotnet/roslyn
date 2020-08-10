// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        /// The document that the request is for, if applicable
        /// </summary>
        public Document? Document { get; }
        /// <summary>
        /// The solution state that the request should operate on
        /// </summary>
        public Solution? Solution { get; }

        /// <summary>
        /// The client capabilities for the request.
        /// </summary>
        public ClientCapabilities ClientCapabilities { get; }

        /// <summary>
        /// The LSP client making the request
        /// </summary>
        public string? ClientName { get; }

        public RequestContext(Document? document, Solution? solution, Action<Solution>? solutionUpdater, ClientCapabilities clientCapabilities, string? clientName)
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
            _solutionUpdater?.Invoke(solution);
        }
    }
}
