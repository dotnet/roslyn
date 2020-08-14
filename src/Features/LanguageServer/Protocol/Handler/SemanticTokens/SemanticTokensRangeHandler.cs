// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
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
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensRangeName), Shared]
    internal class SemanticTokensRangeHandler : AbstractRequestHandler<LSP.SemanticTokensRangeParams, LSP.SemanticTokens>
    {
        private readonly SemanticTokensCache _tokensCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensRangeHandler(
            ILspSolutionProvider solutionProvider,
            SemanticTokensCache tokensCache) : base(solutionProvider)
        {
            _tokensCache = tokensCache;
        }

        public override async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensRangeParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument);
            var resultId = _tokensCache.GetNextResultId();

            // The results from the range handler should not be cached since we don't want to cache
            // partial token results. In addition, a range request is only ever called with a whole
            // document request, so caching range results is unnecessary since the whole document
            // handler will cache the results anyway.
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                request.TextDocument, context.ClientName, SolutionProvider, SemanticTokensCache.TokenTypeToIndex,
                request.Range, cancellationToken).ConfigureAwait(false);
            return new LSP.SemanticTokens { ResultId = resultId, Data = tokensData };
        }
    }
}
