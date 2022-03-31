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
        /// <summary>
        /// Whether or not the solution state on the server is modified
        /// as a part of handling this request.
        /// </summary>
        bool MutatesSolutionState { get; }

        /// <summary>
        /// Whether or not the handler execution queue should build a solution that represents the LSP
        /// state of the world. If this property is not set <see cref="RequestContext.Solution"/> will be <see langword="null"/>
        /// and <see cref="RequestContext.Document"/> will be <see langword="null"/>, even if <see cref="IRequestHandler{RequestType, ResponseType}.GetTextDocumentIdentifier(RequestType)"/>
        /// doesn't return null. Handlers should still provide text document information if possible to
        /// ensure the correct workspace is found and validated.
        /// </summary>
        bool RequiresLSPSolution { get; }
    }

    internal interface IRequestHandler<RequestType, ResponseType> : IRequestHandler
    {
        /// <summary>
        /// Gets the <see cref="TextDocumentIdentifier"/> from the request, if the request provides one.
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
