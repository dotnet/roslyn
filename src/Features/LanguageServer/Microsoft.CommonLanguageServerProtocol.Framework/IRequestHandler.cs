// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework;

public interface IRequestHandler<TRequestType, TResponseType, TRequestContextType> : IMethodHandler
{
    /// <summary>
    /// Gets the identifier of the document from the request, if the request provides one.
    /// </summary>
    /// <remarks>Despite a return type of <see cref="object"/>, you are advised to severly restrict variety of possible return values.
    /// It is left open here to allow for flexibility and variability in finding the TextDocumentIdentifier.
    /// For example, some Param types only have a URI instead of a "TextDocumentIdenfier" object, and others have custom TDI's, or choose to parse JSON.</remarks>
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
