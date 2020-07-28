// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Top level type for LSP request handler.
    /// </summary>
    internal interface IRequestHandler
    {
    }

    internal interface IRequestHandler<RequestType, ResponseType> : IRequestHandler
    {
        /// <summary>
        /// Handles an LSP request.
        /// </summary>
        /// <param name="request">the lsp request.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="clientName">the lsp client making the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the LSP response.</returns>
        Task<ResponseType> HandleRequestAsync(
            RequestType request,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken);
    }

    internal interface ISemanticTokensRequestHandler<RequestType, ResponseType> : IRequestHandler
    {
        /// <summary>
        /// Handles a semantic tokens LSP request.
        /// </summary>
        /// <param name="request">the lsp request.</param>
        /// <param name="tokensCache">the cached results from the previous semantic tokens request.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="clientName">the lsp client making the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <returns>the LSP response and updated cache.</returns>
        Task<ResponseType> HandleRequestAsync(
            RequestType request,
            SemanticTokensCache tokensCache,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken);
    }
}
