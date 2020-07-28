// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens edits for a file. An edit request is received every 500ms,
    /// or every time an edit is made by the user.
    /// </summary>
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName), Shared]
    internal class SemanticTokensEditsHandler : AbstractSemanticTokensRequestHandler<LSP.SemanticTokensEditsParams, SemanticTokensEditsResult>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensEditsHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<SemanticTokensEditsResult> HandleRequestAsync(
            SemanticTokensEditsParams request,
            SemanticTokensCache tokensCache,
            ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var previousResultId = tokensCache.Tokens.ResultId == null ? 0 : int.Parse(tokensCache.Tokens.ResultId);
            var updatedTokens = await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, previousResultId, clientName, SolutionProvider,
                range: null, cancellationToken).ConfigureAwait(false);

            // If any of the following is true, we do not return any edits and instead only return the fully
            // computed semantic tokens:
            // - Previous resultId does not match the cached resultId, or either is null
            // - Previous document's URI does not match the current document's URI, or either is null
            // - Previous tokens data or updated tokens data is null
            if (request.PreviousResultId == null || tokensCache.Tokens.ResultId == null ||
                request.PreviousResultId != tokensCache.Tokens.ResultId ||
                request.TextDocument == null || tokensCache.Document == null ||
                request.TextDocument.Uri != tokensCache.Document.Uri ||
                tokensCache.Tokens.Data == null || updatedTokens.Data == null)
            {
                return new SemanticTokensEditsResult(updatedTokens);
            }

            var edits = SemanticTokensHelpers.ComputeSemanticTokensEdits(previousResultId, tokensCache.Tokens.Data, updatedTokens.Data);
            return new SemanticTokensEditsResult(updatedTokens, edits);
        }
    }
}
