// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public interface IRequestHandler<TRequest, TResponse, TRequestContext> : IMethodHandler
{
    /// <summary>
    /// Handles an LSP request in the context of the supplied document and/or solution.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="ITextDocumentIdentifierHandler{RequestType, TextDocumentIdentifierType}.GetTextDocumentIdentifier(RequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<TResponse> HandleRequestAsync(TRequest request, TRequestContext context, CancellationToken cancellationToken);
}

public interface IRequestHandler<TResponse, TRequestContext> : IMethodHandler
{
    /// <summary>
    /// Handles an LSP request in the context of the supplied document and/or solution.
    /// </summary>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="ITextDocumentIdentifierHandler{RequestType, TextDocumentIdentifierType}.GetTextDocumentIdentifier(RequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<TResponse> HandleRequestAsync(TRequestContext context, CancellationToken cancellationToken);
}
