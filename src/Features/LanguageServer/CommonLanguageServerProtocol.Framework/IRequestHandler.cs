// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework;

public interface IRequestHandler<TRequestType, TResponseType, TRequestContextType> : IMethodHandler
{
    /// <summary>
    /// Gets the identifier of the document from the request, if the request provides one.
    /// </summary>
    object? GetTextDocumentIdentifier(TRequestType request);

    /// <summary>
    /// Handles an LSP request in the context of the supplied document and/or solutuion.
    /// </summary>
    /// <param name="request">The request parameters.</param>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="GetTextDocumentIdentifier(TRequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<TResponseType> HandleRequestAsync(TRequestType request, TRequestContextType context, CancellationToken cancellationToken);
}
