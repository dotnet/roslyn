// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
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
        /// <param name="solution">the solution to apply the request to.</param>
        /// <param name="request">the lsp request.</param>
        /// <param name="clientCapabilities">the client capabilities for the request.</param>
        /// <param name="cancellationToken">a cancellation token.</param>
        /// <param name="keepThreadContext">a value to set if the threading context in the handler should be kept from the caller.</param>
        /// <returns>the lps response.</returns>
        Task<ResponseType> HandleRequestAsync(Solution solution, RequestType request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken, bool keepThreadContext = false);
    }
}
