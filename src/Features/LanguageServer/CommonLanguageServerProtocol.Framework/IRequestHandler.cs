// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace CommonLanguageServerProtocol.Framework;

/// <summary>
/// Top level type for LSP request handler.
/// </summary>
public interface IRequestHandler
{
    /// <summary>
    /// Whether or not the solution state on the server is modified
    /// as a part of handling this request.
    /// </summary>
    bool MutatesSolutionState { get; }

    /// <summary>
    /// Whether or not the handler execution queue should build a solution that represents the LSP
    /// state of the world. If this property is not set <see cref="RequestContext.Solution"/> will be <see langword="null"/>
    /// and <see cref="RequestContext.Document"/> will be <see langword="null"/>, even if <see cref="IRequestHandler{RequestType, ResponseType, RequestContextType}.GetTextDocumentUri(RequestType)"/>
    /// doesn't return null. Handlers should still provide text document information if possible to
    /// ensure the correct workspace is found and validated.
    /// </summary>
    bool RequiresLSPSolution { get; }
}

public interface IRequestHandler<RequestType, ResponseType, RequestContextType> : IRequestHandler
{
    /// <summary>
    /// Gets the <see cref="Uri"/> of the document from the request, if the request provides one.
    /// </summary>
    Uri? GetTextDocumentUri(RequestType request);

    /// <summary>
    /// Handles an LSP request in the context of the supplied document and/or solutuion.
    /// </summary>
    /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="GetTextDocumentUri(RequestType)"/> if applicable.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
    /// <returns>The LSP response.</returns>
    Task<ResponseType> HandleRequestAsync(RequestType request, RequestContextType context, CancellationToken cancellationToken);
}
