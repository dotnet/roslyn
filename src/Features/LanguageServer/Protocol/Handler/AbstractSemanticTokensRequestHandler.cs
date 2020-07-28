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
    /// Handles a semantic tokens LSP request. Semantic token handlers that use or require caching
    /// should inherit this class.
    /// </summary>
    internal abstract class AbstractSemanticTokensRequestHandler<RequestType, ResponseType> : ISemanticTokensRequestHandler<RequestType, ResponseType>
    {
        protected readonly ILspSolutionProvider SolutionProvider;

        protected AbstractSemanticTokensRequestHandler(ILspSolutionProvider solutionProvider)
        {
            SolutionProvider = solutionProvider;
        }

        public abstract Task<ResponseType> HandleRequestAsync(
            RequestType request,
            SemanticTokensCache tokensCache,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken);
    }
}
