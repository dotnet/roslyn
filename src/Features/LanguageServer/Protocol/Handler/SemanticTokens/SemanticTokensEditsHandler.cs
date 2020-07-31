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
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens edits for a file. An edit request is received every 500ms,
    /// or every time an edit is made by the user.
    /// </summary>
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName), Shared]
    internal class SemanticTokensEditsHandler : AbstractRequestHandler<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>
    {
        private readonly SemanticTokensCache _tokensCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensEditsHandler(
            ILspSolutionProvider solutionProvider,
            SemanticTokensCache tokensCache) : base(solutionProvider)
        {
            _tokensCache = tokensCache;
        }

        public override async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> HandleRequestAsync(
            LSP.SemanticTokensEditsParams request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument);
            var resultId = await _tokensCache.GetNextResultIdAsync(request.TextDocument.Uri, cancellationToken).ConfigureAwait(false);

            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var updatedTokens = await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, resultId, clientName, SolutionProvider,
                range: null, cancellationToken).ConfigureAwait(false);

            // Getting the cached tokens for the document. If we don't have an applicable cached token set,
            // we can't calculate edits, so we must return all semantic tokens instead.
            var cachedTokens = await _tokensCache.GetCachedTokensAsync(
                request.TextDocument.Uri, request.PreviousResultId, cancellationToken).ConfigureAwait(false);
            if (cachedTokens == null)
            {
                return updatedTokens;
            }

            // The Data property is always populated on the server side, so it should never be null.
            Contract.ThrowIfNull(cachedTokens.Data);
            Contract.ThrowIfNull(updatedTokens.Data);

            var edits = ComputeSemanticTokensEdits(resultId, cachedTokens.Data, updatedTokens.Data);
            await _tokensCache.UpdateCacheAsync(request.TextDocument.Uri, updatedTokens, cancellationToken).ConfigureAwait(false);
            return edits;
        }

        /// <summary>
        /// Compares two sets of SemanticTokens and returns the edits between them.
        /// </summary>
        private static LSP.SemanticTokensEdits ComputeSemanticTokensEdits(
            string resultId,
            int[] cachedSemanticTokens,
            int[] updatedSemanticTokens)
        {
            using var _1 = ArrayBuilder<LSP.SemanticTokensEdit>.GetInstance(out var edits);
            var index = 0;

            // There are three cases where we might need to create an edit:
            //     Case 1: Both cached and updated tokens have values at an index, but the tokens don't match
            //     Case 2: Cached tokens set is longer than updated tokens set - need to make deletion
            //     Case 3: Updated tokens set is longer than cached tokens set - need to make insertion

            using var _2 = ArrayBuilder<int>.GetInstance(out var currentEdit);
            while (index < cachedSemanticTokens.Length && index < updatedSemanticTokens.Length)
            {
                // Case 1: Both cached and updated tokens have values at index, but the tokens don't match.
                // We want to make the least number of edits possible, so we keep track of consecutive changes
                // and report them as a singular edit.
                if (cachedSemanticTokens[index] != updatedSemanticTokens[index])
                {
                    currentEdit.Add(updatedSemanticTokens[index]);
                }
                else if (!currentEdit.IsEmpty())
                {
                    edits.Add(GenerateEdit(
                        start: index - currentEdit.Count, deleteCount: currentEdit.Count, data: currentEdit.ToArray()));
                    currentEdit.Clear();
                }

                index += 1;
            }

            // Report any edit in progress. If step 3 applies, we skip this step since we can just report
            // one giant edit later on.
            if (!currentEdit.IsEmpty() && index >= updatedSemanticTokens.Length)
            {
                edits.Add(GenerateEdit(
                    start: index - currentEdit.Count, deleteCount: currentEdit.Count, data: currentEdit.ToArray()));
            }

            // Case 2: Cached token set is longer than updated tokens - need to make deletion edit
            if (index < cachedSemanticTokens.Length)
            {
                var deleteCount = cachedSemanticTokens.Length - updatedSemanticTokens.Length;
                edits.Add(GenerateEdit(start: index, deleteCount: deleteCount, data: Array.Empty<int>()));
            }
            // Case 3: Updated tokens set is longer than cached tokens set - need to make insertion edit
            else if (index < updatedSemanticTokens.Length)
            {
                // If there are any leftover edits that we haven't reported from case 1, report them
                // in one giant edit.
                var data = updatedSemanticTokens.Skip(index - currentEdit.Count).ToArray();
                edits.Add(GenerateEdit(start: index, deleteCount: 0, data: data));
            }

            return new LSP.SemanticTokensEdits { Edits = edits.ToArray(), ResultId = resultId };
        }

        internal static LSP.SemanticTokensEdit GenerateEdit(int start, int deleteCount, int[] data)
            => new LSP.SemanticTokensEdit
            {
                Start = start,
                DeleteCount = deleteCount,
                Data = data
            };
    }
}
