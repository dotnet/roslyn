// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
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

            var edits = ComputeSemanticTokensEdits(previousResultId, tokensCache.Tokens.Data, updatedTokens.Data);
            return new SemanticTokensEditsResult(updatedTokens, edits);
        }

        /// <summary>
        /// Compares two sets of SemanticTokens and returns the edits between them.
        /// </summary>
        private static LSP.SemanticTokensEdits ComputeSemanticTokensEdits(
            int previousResultId,
            int[] cachedSemanticTokens,
            int[] updatedSemanticTokens)
        {
            using var _ = ArrayBuilder<SemanticTokensEdit>.GetInstance(out var edits);
            var index = 0;

            // There are three cases where we might need to create an edit:
            //     Case 1: Both cached and updated tokens have values at an index, but the tokens don't match
            //     Case 2: Cached tokens set is longer than updated tokens set - need to make deletion
            //     Case 3: Updated tokens set is longer than cached tokens set - need to make insertion

            while (index < cachedSemanticTokens.Length && index < updatedSemanticTokens.Length)
            {
                // Case 1: Both cached and updated tokens have values at index, but the tokens don't match
                if (cachedSemanticTokens[index] != updatedSemanticTokens[index])
                {
                    edits.Add(GenerateEdit(start: index, deleteCount: 1, data: new int[] { updatedSemanticTokens[index] }));
                }

                index++;
            }

            // Case 2: Cached tokens is longer than updated tokens - need to make deletion
            if (index < cachedSemanticTokens.Length)
            {
                var deleteCount = cachedSemanticTokens.Length - updatedSemanticTokens.Length;
                edits.Add(GenerateEdit(start: index, deleteCount: deleteCount, data: Array.Empty<int>()));
            }
            // Case 3: Updated tokens set has value at index but cached tokens set does not - need to make insertion
            else if (index < updatedSemanticTokens.Length)
            {
                edits.Add(GenerateEdit(start: index, deleteCount: 0, data: updatedSemanticTokens.Skip(index).ToArray()));
            }

            return new SemanticTokensEdits { Edits = edits.ToArray(), ResultId = (previousResultId + 1).ToString() };
        }

        internal static SemanticTokensEdit GenerateEdit(int start, int deleteCount, int[] data)
            => new SemanticTokensEdit
            {
                Start = start,
                DeleteCount = deleteCount,
                Data = data
            };
    }
}
