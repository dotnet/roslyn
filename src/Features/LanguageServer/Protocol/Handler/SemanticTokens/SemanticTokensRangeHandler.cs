// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens for a given range.
    /// </summary>
    /// <remarks>
    /// When a user opens a file, it can be beneficial to only compute the semantic tokens for the visible range
    /// for faster UI rendering.
    /// The range handler is only invoked when a file is opened. When the first whole document request completes
    /// via <see cref="SemanticTokensHandler"/>, the range handler is not invoked again for the rest of the session.
    /// </remarks>
    internal class SemanticTokensRangeHandler : IRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly SemanticTokensCache _tokensCache;

        public SemanticTokensRangeHandler(SemanticTokensCache tokensCache)
        {
            _tokensCache = tokensCache;
        }

        public string Method => LSP.SemanticTokensMethods.TextDocumentSemanticTokensRangeName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensRangeParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            var resultId = _tokensCache.GetNextResultId();

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document, SemanticTokensCache.TokenTypeToIndex,
                request.Range, cancellationToken).ConfigureAwait(false);
            return new LSP.SemanticTokens { ResultId = resultId, Data = tokensData };
        }
    }
}
