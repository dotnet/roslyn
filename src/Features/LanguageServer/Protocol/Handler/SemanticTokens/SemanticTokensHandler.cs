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
    /// Computes the semantic tokens for a whole document.
    /// </summary>
    /// <remarks>
    /// This handler is invoked when a user opens a file. Depending on the size of the file, the full token set may be
    /// slow to compute, so the <see cref="SemanticTokensRangeHandler"/> is also called when a file is opened in order
    /// to render UI results quickly until this handler finishes running.
    /// Unlike the range handler, the whole document handler may be called again if the LSP client finds an edit that
    /// is difficult to correctly apply to their tags cache. This allows for reliable recovery from errors and accounts
    /// for limitations in the edits application logic.
    /// </remarks>
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensName, mutatesSolutionState: false), Shared]
    internal class SemanticTokensHandler : IRequestHandler<LSP.SemanticTokensParams, LSP.SemanticTokens>
    {
        private readonly SemanticTokensCache _tokensCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensHandler(SemanticTokensCache tokensCache)
        {
            _tokensCache = tokensCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensParams request) => request.TextDocument;

        public async Task<LSP.SemanticTokens> HandleRequestAsync(
            LSP.SemanticTokensParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Document, "Document is null.");
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");

            var resultId = _tokensCache.GetNextResultId();
            var tokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document, SemanticTokensCache.TokenTypeToIndex,
                range: null, cancellationToken).ConfigureAwait(false);

            var tokens = new LSP.SemanticTokens { ResultId = resultId, Data = tokensData };
            await _tokensCache.UpdateCacheAsync(request.TextDocument.Uri, tokens, cancellationToken).ConfigureAwait(false);
            return tokens;
        }
    }
}
