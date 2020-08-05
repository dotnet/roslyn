// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
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
            var newSemanticTokens = await SemanticTokensHelpers.ComputeSemanticTokensAsync(
                request.TextDocument, resultId, clientName, SolutionProvider,
                range: null, cancellationToken).ConfigureAwait(false);

            // Getting the cached tokens for the document. If we don't have an applicable cached token set,
            // we can't calculate edits, so we must return all semantic tokens instead.
            var oldSemanticTokens = await _tokensCache.GetCachedTokensAsync(
                request.TextDocument.Uri, request.PreviousResultId, cancellationToken).ConfigureAwait(false);
            if (oldSemanticTokens == null)
            {
                return newSemanticTokens;
            }

            // The Data property is always populated on the server side, so it should never be null.
            Contract.ThrowIfNull(oldSemanticTokens.Data);
            Contract.ThrowIfNull(newSemanticTokens.Data);

            var edits = new SemanticTokensEdits
            {
                Edits = ComputeSemanticTokensEdits(oldSemanticTokens.Data, newSemanticTokens.Data),
                ResultId = resultId
            };

            await _tokensCache.UpdateCacheAsync(request.TextDocument.Uri, newSemanticTokens, cancellationToken).ConfigureAwait(false);
            return edits;
        }

        /// <summary>
        /// Compares two sets of SemanticTokens and returns the edits between them.
        /// </summary>
        private static LSP.SemanticTokensEdit[] ComputeSemanticTokensEdits(
            int[] oldSemanticTokens,
            int[] newSemanticTokens)
        {
            // We use Roslyn's version of the Myers' Diff Algorithm to compute the minimal edits
            // between the old and new tokens.
            // Edits are computed by token (i.e. in sets of five integers), so if one value in the token
            // is changed, the entire token is replaced. We do this instead of directly comparing each
            // value in the token individually so that we can potentially save on computation costs, since
            // we can return early if we find that one value in the token doesn't match. However, there
            // are trade-offs since our insertions/deletions are usually larger.
            using var _ = ArrayBuilder<LSP.SemanticTokensEdit>.GetInstance(out var semanticTokensEdits);

            // Turning arrays into tuples of five ints, each representing one token
            var oldGroupedSemanticTokens = ConvertToGroupedSemanticTokens(oldSemanticTokens);
            var newGroupedSemanticTokens = ConvertToGroupedSemanticTokens(newSemanticTokens);

            var edits = LongestCommonSemanticTokensSubsequence.GetEdits(oldGroupedSemanticTokens, newGroupedSemanticTokens);

            // Since edits are reported relative to each other, we only care about an 'Update' EditKind
            // if it's preceded by an insertion or deletion.
            var adjustToken = false;

            foreach (var edit in edits)
            {
                switch (edit.Kind)
                {
                    case EditKind.Insert:
                        semanticTokensEdits.Add(
                            GenerateEdit(start: edit.NewIndex * 5, deleteCount: 0, newGroupedSemanticTokens[edit.NewIndex].ToArray()));
                        adjustToken = true;
                        break;
                    case EditKind.Delete:
                        semanticTokensEdits.Add(
                            GenerateEdit(start: edit.OldIndex * 5, deleteCount: 5, data: Array.Empty<int>()));
                        adjustToken = true;
                        break;
                    case EditKind.Update:
                        // We only care about an update if (1) the original token has moved somewhere else and
                        // (2) it is immediately preceded by an insertion or deletion.
                        if (edit.NewIndex == edit.OldIndex || !adjustToken)
                        {
                            break;
                        }

                        semanticTokensEdits.Add(
                            GenerateEdit(start: edit.OldIndex * 5, deleteCount: 5, data: newGroupedSemanticTokens[edit.NewIndex].ToArray()));
                        adjustToken = false;
                        break;
                    default:
                        break;
                }
            }

            return semanticTokensEdits.ToArray();
        }

        /// <summary>
        /// Converts an array of individual semantic token values to an array of values grouped
        /// together by semantic token.
        /// </summary>
        private static SemanticToken[] ConvertToGroupedSemanticTokens(int[] tokens)
        {
            using var _ = ArrayBuilder<SemanticToken>.GetInstance(out var fullTokens);
            for (var i = 0; i < tokens.Length; i += 5)
            {
                fullTokens.Add(new SemanticToken(tokens[i], tokens[i + 1], tokens[i + 2], tokens[i + 3], tokens[i + 4]));
            }

            return fullTokens.ToArray();
        }

        internal static LSP.SemanticTokensEdit GenerateEdit(int start, int deleteCount, int[] data)
            => new LSP.SemanticTokensEdit
            {
                Start = start,
                DeleteCount = deleteCount,
                Data = data
            };

        private sealed class LongestCommonSemanticTokensSubsequence : LongestCommonSubsequence<SemanticToken[]>
        {
            private static readonly LongestCommonSemanticTokensSubsequence s_instance = new LongestCommonSemanticTokensSubsequence();

            protected override bool ItemsEqual(
                SemanticToken[] oldSemanticTokens, int oldIndex,
                SemanticToken[] newSemanticTokens, int newIndex)
            {
                var oldToken = oldSemanticTokens[oldIndex];
                var newToken = newSemanticTokens[newIndex];

                return oldToken.DeltaLine == newToken.DeltaLine && oldToken.DeltaStartCharacter == newToken.DeltaStartCharacter &&
                    oldToken.Length == newToken.Length && oldToken.TokenType == newToken.TokenType &&
                    oldToken.TokenModifiers == newToken.TokenModifiers;
            }

            // We reverse the result array since the original edits are reported with last tokens in the
            // document output first in the IEnumerable. This is due to the nature of the Myers diff
            // algorithm in which the optimal steps are retraced from the goal. However, when creating
            // SemanticTokensEdits to report back to LSP, we need to analyze the edits in chronological order.
            public static IEnumerable<SequenceEdit> GetEdits(
                SemanticToken[] oldSemanticTokens, SemanticToken[] newSemanticTokens)
                => s_instance.GetEdits(oldSemanticTokens, oldSemanticTokens.Length, newSemanticTokens, newSemanticTokens.Length).Reverse();
        }

        /// <summary>
        /// Stores the values that make up the LSP representation of an individual semantic token.
        /// </summary>
        private struct SemanticToken
        {
            public int DeltaLine { get; }
            public int DeltaStartCharacter { get; }
            public int Length { get; }
            public int TokenType { get; }
            public int TokenModifiers { get; }

            public SemanticToken(int deltaLine, int deltaStartCharacter, int length, int tokenType, int tokenModifiers)
            {
                DeltaLine = deltaLine;
                DeltaStartCharacter = deltaStartCharacter;
                Length = length;
                TokenType = tokenType;
                TokenModifiers = tokenModifiers;
            }

            public int[] ToArray()
            {
                return new int[] { DeltaLine, DeltaStartCharacter, Length, TokenType, TokenModifiers };
            }
        }
    }
}
