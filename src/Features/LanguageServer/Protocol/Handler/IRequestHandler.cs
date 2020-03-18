﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// <returns>the LSP response.</returns>
        Task<ResponseType> HandleRequestAsync(Solution solution, RequestType request, ClientCapabilities clientCapabilities, CancellationToken cancellationToken);
    }
}
