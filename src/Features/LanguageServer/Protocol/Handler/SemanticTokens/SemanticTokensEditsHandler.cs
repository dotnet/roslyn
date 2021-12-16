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
    [ExportLspMethod(LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName, mutatesSolutionState: false), Shared]
    internal class SemanticTokensEditsHandler : IRequestHandler<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>
    {
        private readonly SemanticTokensCache _tokensCache;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensEditsHandler(SemanticTokensCache tokensCache)
        {
            _tokensCache = tokensCache;
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensEditsParams request) => request.TextDocument;

        public async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> HandleRequestAsync(
            LSP.SemanticTokensEditsParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(context.Document, "Document is null.");
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(request.PreviousResultId, "previousResultId is null.");

            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var newSemanticTokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document, SemanticTokensCache.TokenTypeToIndex,
                range: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(newSemanticTokensData, "newSemanticTokensData is null.");

            var resultId = _tokensCache.GetNextResultId();
            var newSemanticTokens = new LSP.SemanticTokens { ResultId = resultId, Data = newSemanticTokensData };

            await _tokensCache.UpdateCacheAsync(
                request.TextDocument.Uri, newSemanticTokens, cancellationToken).ConfigureAwait(false);

            // Getting the cached tokens for the document. If we don't have an applicable cached token set,
            // we can't calculate edits, so we must return all semantic tokens instead.
            var oldSemanticTokensData = await _tokensCache.GetCachedTokensDataAsync(
                request.TextDocument.Uri, request.PreviousResultId, cancellationToken).ConfigureAwait(false);
            if (oldSemanticTokensData == null)
            {
                return newSemanticTokens;
            }

            var edits = new SemanticTokensEdits
            {
                Edits = ComputeSemanticTokensEdits(oldSemanticTokensData, newSemanticTokensData),
                ResultId = resultId
            };

            return edits;
        }

        /// <summary>
        /// Compares two sets of SemanticTokens and returns the edits between them.
        /// </summary>
        private static LSP.SemanticTokensEdit[] ComputeSemanticTokensEdits(
            int[] oldSemanticTokens,
            int[] newSemanticTokens)
        {
            if (oldSemanticTokens.SequenceEqual(newSemanticTokens))
            {
                return Array.Empty<SemanticTokensEdit>();
            }

            // We use Roslyn's version of the Myers' Diff Algorithm to compute the minimal edits
            // between the old and new tokens.
            // Edits are computed by token (i.e. in sets of five integers), so if one value in the token
            // is changed, the entire token is replaced. We do this instead of directly comparing each
            // value in the token individually so that we can potentially save on computation costs, since
            // we can return early if we find that one value in the token doesn't match. However, there
            // are trade-offs since our insertions/deletions are usually larger.

            // Turning arrays into tuples of five ints, each representing one token
            var oldGroupedSemanticTokens = ConvertToGroupedSemanticTokens(oldSemanticTokens);
            var newGroupedSemanticTokens = ConvertToGroupedSemanticTokens(newSemanticTokens);

            var edits = LongestCommonSemanticTokensSubsequence.GetEdits(oldGroupedSemanticTokens, newGroupedSemanticTokens);

            return ConvertToSemanticTokenEdits(newGroupedSemanticTokens, edits);
        }

        private static SemanticTokensEdit[] ConvertToSemanticTokenEdits(SemanticToken[] newGroupedSemanticTokens, IEnumerable<SequenceEdit> edits)
        {
            // Our goal is to minimize the number of edits we return to LSP. It's possible an index
            // may have both an insertion and deletion, in which case we can combine the two into a
            // single update. We use the dictionary below to keep track of whether an index contains
            // an insertion, deletion, or both.
            using var _ = PooledDictionary<int, SemanticTokenEditKind>.GetInstance(out var indexToEditKinds);

            foreach (var edit in edits)
            {
                // We only care about EditKind.Insert and EditKind.Delete, since they encompass all
                // changes to the document. All other EditKinds are ignored.
                switch (edit.Kind)
                {
                    case EditKind.Insert:
                        indexToEditKinds.TryGetValue(edit.NewIndex, out var editKindWithoutInsert);
                        Contract.ThrowIfTrue(editKindWithoutInsert == SemanticTokenEditKind.Insert, "There cannot be two inserts at the same position.");
                        Contract.ThrowIfTrue(editKindWithoutInsert == SemanticTokenEditKind.Update, "There cannot be an insert and update at the same position.");
                        indexToEditKinds[edit.NewIndex] = editKindWithoutInsert == SemanticTokenEditKind.None ? SemanticTokenEditKind.Insert : SemanticTokenEditKind.Update;
                        break;
                    case EditKind.Delete:
                        indexToEditKinds.TryGetValue(edit.OldIndex, out var editKindWithoutDelete);
                        Contract.ThrowIfTrue(editKindWithoutDelete == SemanticTokenEditKind.Delete, "There cannot be two deletions at the same position.");
                        Contract.ThrowIfTrue(editKindWithoutDelete == SemanticTokenEditKind.Update, "There cannot be a deletion and update at the same position.");
                        indexToEditKinds[edit.OldIndex] = editKindWithoutDelete == SemanticTokenEditKind.None ? SemanticTokenEditKind.Delete : SemanticTokenEditKind.Update;
                        break;
                }
            }

            return CombineEditsIfPossible(newGroupedSemanticTokens, indexToEditKinds);
        }

        private static SemanticTokensEdit[] CombineEditsIfPossible(
            SemanticToken[] newGroupedSemanticTokens,
            Dictionary<int, SemanticTokenEditKind> indexToEditKinds)
        {
            // This method combines the edits into the minimal possible edits (for the most part).
            // For example, if an index contains both an insertion and deletion, we combine the two
            // edits into one.
            // We also combine edits if we have consecutive edits of the same types, i.e.
            // Delete->Delete, Insert->Insert, and Update->Update.
            // Technically, we could combine Update->Insert, and Update->Delete, but those cases have
            // special rules and would complicate the logic. They also generally do not result in a
            // huge reduction in the total number of edits, so we leave them out for now.

            using var _ = ArrayBuilder<LSP.SemanticTokensEdit>.GetInstance(out var semanticTokensEdits);

            var editIndices = indexToEditKinds.Keys.ToArray();

            // The indices in indexToEdit kinds are not guaranteed to be in chronological order when we
            // extract them from the dictionary. We must sort the edit kinds by index since we need to
            // know what kind of edits surround a given index in order to potentially combine them into
            // one edit.
            Array.Sort(editIndices);

            // Example to give clarity to currentEditIndex and currentTokenIndex variables defined below:
            // Non-grouped semantic tokens: 0 1 2 3 4 5 6 7 8 9 10 11 12 13 14
            // currentEditIndex:            0                   1
            // currentTokenIndex:           0         1         2
            for (var currentEditIndex = 0; currentEditIndex < editIndices.Length; currentEditIndex++)
            {
                var currentTokenIndex = editIndices[currentEditIndex];
                var initialEditKind = indexToEditKinds[currentTokenIndex];
                var editStartPosition = currentTokenIndex * 5;

                if (initialEditKind == SemanticTokenEditKind.Update)
                {
                    currentEditIndex = AddUpdateEdit(
                        newGroupedSemanticTokens, indexToEditKinds, semanticTokensEdits, editIndices,
                        currentEditIndex, editStartPosition);
                }
                else if (initialEditKind == SemanticTokenEditKind.Insert)
                {
                    currentEditIndex = AddInsertionEdit(
                        newGroupedSemanticTokens, indexToEditKinds, semanticTokensEdits, editIndices,
                        currentEditIndex, editStartPosition);
                }
                else
                {
                    Contract.ThrowIfFalse(initialEditKind == SemanticTokenEditKind.Delete, "Expected initialEditKind to be SemanticTokenEditKind.Delete.");
                    currentEditIndex = AddDeletionEdit(
                        indexToEditKinds, semanticTokensEdits, editIndices, currentEditIndex, editStartPosition);
                }
            }

            return semanticTokensEdits.ToArray();

            // Local functions
            static int AddUpdateEdit(
                SemanticToken[] newGroupedSemanticTokens,
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndices,
                int startEditIndex,
                int editStartPosition)
            {
                var _ = ArrayBuilder<int>.GetInstance(out var tokensToInsert);

                // For simplicitly, we only allow an "update" (i.e. a dual insertion/deletion) to be
                // combined with other updates.
                var endEditIndex = GetCombinedEditEndIndex(
                    SemanticTokenEditKind.Update, indexToEditKinds, editIndices, startEditIndex);

                var deleteCount = 5 * (1 + (endEditIndex - startEditIndex));
                for (var i = 0; i <= endEditIndex - startEditIndex; i++)
                {
                    newGroupedSemanticTokens[editIndices[startEditIndex + i]].AddToEnd(tokensToInsert);
                }

                semanticTokensEdits.Add(
                    GenerateEdit(start: editStartPosition, deleteCount: deleteCount, data: tokensToInsert.ToArray()));
                return endEditIndex;
            }

            static int AddInsertionEdit(
                SemanticToken[] newGroupedSemanticTokens,
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndices,
                int startEditIndex,
                int editStartPosition)
            {
                var _ = ArrayBuilder<int>.GetInstance(out var tokensToInsert);

                // An insert can only be combined with other inserts that directly follow it.
                var endEditIndex = GetCombinedEditEndIndex(
                    SemanticTokenEditKind.Insert, indexToEditKinds, editIndices, startEditIndex);

                for (var i = 0; i <= endEditIndex - startEditIndex; i++)
                {
                    newGroupedSemanticTokens[editIndices[startEditIndex + i]].AddToEnd(tokensToInsert);
                }

                semanticTokensEdits.Add(
                    GenerateEdit(start: editStartPosition, deleteCount: 0, data: tokensToInsert.ToArray()));
                return endEditIndex;
            }

            static int AddDeletionEdit(
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndices,
                int startEditIndex,
                int editStartPosition)
            {
                // A deletion can only be combined with other deletions that directly follow it.
                var endEditNumber = GetCombinedEditEndIndex(
                    SemanticTokenEditKind.Delete, indexToEditKinds, editIndices, startEditIndex);

                var deleteCount = 5 * (1 + (endEditNumber - startEditIndex));
                semanticTokensEdits.Add(
                    GenerateEdit(start: editStartPosition, deleteCount: deleteCount, data: Array.Empty<int>()));
                return endEditNumber;
            }

            // Returns the updated ordered edit number after we know how many edits we can combine.
            static int GetCombinedEditEndIndex(
                SemanticTokenEditKind editKind,
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                int[] editIndices,
                int currentEditIndex)
            {
                // To continue combining edits, we need to ensure:
                // 1) There is an edit following the current edit.
                // 2) The current and next edits involve tokens that are located right next to
                // each other in the file.
                // 3) The next edit is the same type as the current edit.
                while (currentEditIndex + 1 < editIndices.Length &&
                    indexToEditKinds[editIndices[currentEditIndex + 1]] == editKind &&
                    editIndices[currentEditIndex + 1] == editIndices[currentEditIndex] + 1)
                {
                    currentEditIndex++;
                }

                return currentEditIndex;
            }
        }

        /// <summary>
        /// Converts an array of individual semantic token values to an array of values grouped
        /// together by semantic token.
        /// </summary>
        private static SemanticToken[] ConvertToGroupedSemanticTokens(int[] tokens)
        {
            Contract.ThrowIfTrue(tokens.Length % 5 != 0, $"Tokens length should be divisible by 5. Actual length: {tokens.Length}");
            using var _ = ArrayBuilder<SemanticToken>.GetInstance(out var fullTokens);
            for (var i = 0; i < tokens.Length; i += 5)
            {
                fullTokens.Add(new SemanticToken(tokens[i], tokens[i + 1], tokens[i + 2], tokens[i + 3], tokens[i + 4]));
            }

            return fullTokens.ToArray();
        }

        internal static LSP.SemanticTokensEdit GenerateEdit(int start, int deleteCount, int[] data)
            => new()
            {
                Start = start,
                DeleteCount = deleteCount,
                Data = data
            };

        private sealed class LongestCommonSemanticTokensSubsequence : LongestCommonSubsequence<SemanticToken[]>
        {
            private static readonly LongestCommonSemanticTokensSubsequence s_instance = new();

            protected override bool ItemsEqual(
                SemanticToken[] oldSemanticTokens, int oldIndex,
                SemanticToken[] newSemanticTokens, int newIndex)
                => oldSemanticTokens[oldIndex].Equals(newSemanticTokens[newIndex]);

            public static IEnumerable<SequenceEdit> GetEdits(
                SemanticToken[] oldSemanticTokens, SemanticToken[] newSemanticTokens)
                => s_instance.GetEdits(oldSemanticTokens, oldSemanticTokens.Length, newSemanticTokens, newSemanticTokens.Length);
        }

        /// <summary>
        /// Stores the values that make up the LSP representation of an individual semantic token.
        /// </summary>
#pragma warning disable CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
        private readonly struct SemanticToken : IEquatable<SemanticToken>
#pragma warning restore CA1067 // Override Object.Equals(object) when implementing IEquatable<T>
        {
            private readonly int _deltaLine;
            private readonly int _deltaStartCharacter;
            private readonly int _length;
            private readonly int _tokenType;
            private readonly int _tokenModifiers;

            public SemanticToken(int deltaLine, int deltaStartCharacter, int length, int tokenType, int tokenModifiers)
            {
                _deltaLine = deltaLine;
                _deltaStartCharacter = deltaStartCharacter;
                _length = length;
                _tokenType = tokenType;
                _tokenModifiers = tokenModifiers;
            }

            public void AddToEnd(ArrayBuilder<int> tokensToInsert)
            {
                tokensToInsert.Add(_deltaLine);
                tokensToInsert.Add(_deltaStartCharacter);
                tokensToInsert.Add(_length);
                tokensToInsert.Add(_tokenType);
                tokensToInsert.Add(_tokenModifiers);
            }

            public bool Equals(SemanticToken otherToken)
            {
                return _deltaLine == otherToken._deltaLine &&
                    _deltaStartCharacter == otherToken._deltaStartCharacter &&
                    _length == otherToken._length &&
                    _tokenType == otherToken._tokenType &&
                    _tokenModifiers == otherToken._tokenModifiers;
            }
        }

        private enum SemanticTokenEditKind
        {
            None = 0,
            Insert = 1,
            Delete = 2,
            Update = 3
        }
    }
}
