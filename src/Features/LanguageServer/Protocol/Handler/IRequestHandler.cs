// Licensed to the .NET Foundation under one or more agreements.
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
        /// Gets the TestDocumentIdentifier from the request, if the request provides one.
        /// </summary>
        TextDocumentIdentifier? GetTextDocumentIdentifier(RequestType request);

        /// <summary>
        /// Handles an LSP request in the context of the supplied document and/or solutuion.
        /// </summary>
        /// <param name="context">The LSP request context, which should have been filled in with document information from <see cref="GetTextDocumentIdentifier(RequestType)"/> if applicable.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the request processing.</param>
        /// <returns>The LSP response.</returns>
        Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);
    }
}
