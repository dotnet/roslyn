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
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument);

            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var newSemanticTokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                request.TextDocument, context.ClientName, SolutionProvider, SemanticTokensCache.TokenTypesToIndex,
                range: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(newSemanticTokensData);

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

            // The Data property is always populated on the server side, so it should never be null.
            Contract.ThrowIfNull(oldSemanticTokensData);

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
            var indexToEditKinds = new Dictionary<int, SemanticTokenEditKind>();

            foreach (var edit in edits)
            {
                // We only care about EditKind.Insert and EditKind.Delete, since they encompass all
                // changes to the document. All other EditKinds are ignored.
                switch (edit.Kind)
                {
                    case EditKind.Insert:
                        indexToEditKinds.TryGetValue(edit.NewIndex, out var editKindWithoutInsert);
                        indexToEditKinds[edit.NewIndex] = editKindWithoutInsert |= SemanticTokenEditKind.Insert;
                        break;
                    case EditKind.Delete:
                        indexToEditKinds.TryGetValue(edit.OldIndex, out var editKindWithoutDelete);
                        indexToEditKinds[edit.OldIndex] = editKindWithoutDelete |= SemanticTokenEditKind.Delete;
                        break;
                    default:
                        break;
                }
            }

            return CombineEditsIfPossible(newGroupedSemanticTokens, indexToEditKinds);
        }

        private static SemanticTokensEdit[] CombineEditsIfPossible(
            SemanticToken[] newGroupedSemanticTokens,
            Dictionary<int, SemanticTokenEditKind> indexToEditKinds)
        {
            // This method combines the edits into the minimal possible edits.
            // For example, if an index contains both an insertion and deletion, we can combine the two
            // edits into one.
            // We can also combine edits if we have consecutive edits of certain types:
            // Delete->Delete, Insert->Insert, Update->Update, Update->Insert, and Update->Delete.
            // Note for the Update->Insert and Update->Delete cases, any further edits we combine can
            // only be an Insert or Delete, respectively.

            using var _ = ArrayBuilder<LSP.SemanticTokensEdit>.GetInstance(out var semanticTokensEdits);

            // We sort the edit kinds by index since we need to know what kind of edits surround
            // a given index in order to potentially combine them into one edit.
            var editIndexes = indexToEditKinds.Keys.ToArray();
            Array.Sort(editIndexes);

            for (var i = 0; i < editIndexes.Length; i++)
            {
                var initialEditIndex = editIndexes[i];
                var initialEditKind = indexToEditKinds[initialEditIndex];

                if (initialEditKind.HasFlag(SemanticTokenEditKind.Insert) && initialEditKind.HasFlag(SemanticTokenEditKind.Delete))
                {
                    i = AddInsertionDeletionEdit(
                        newGroupedSemanticTokens, indexToEditKinds, semanticTokensEdits, editIndexes, i, initialEditIndex);
                }
                else if (initialEditKind.HasFlag(SemanticTokenEditKind.Insert))
                {
                    i = AddInsertionEdit(
                        newGroupedSemanticTokens, indexToEditKinds, semanticTokensEdits, editIndexes, i, initialEditIndex);
                }
                else
                {
                    i = AddDeletionEdit(
                        indexToEditKinds, semanticTokensEdits, editIndexes, i, initialEditIndex);
                }
            }

            return semanticTokensEdits.ToArray();

            static int AddInsertionDeletionEdit(
                SemanticToken[] newGroupedSemanticTokens,
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndexes,
                int i,
                int initialEditIndex)
            {
                var deleteCount = 5;
                var _ = ArrayBuilder<int>.GetInstance(out var tokensToInsert);
                tokensToInsert.AddRange(newGroupedSemanticTokens[initialEditIndex].ToArray());

                // An "update" (i.e. a dual insertion/deletion) can be combined with an update,
                // deletion, or insertion that directly follows it. If combined with an insertion
                // or deletion, only that type is allowed in the combined edit afterwards.
                var editKind = SemanticTokenEditKind.None;

                // To continue combining edits, we need to ensure:
                // 1) There is an edit following the current edit
                // 2) The current edit and next edit involve tokens that are located right next to
                // each other in the file.
                while (i + 1 < editIndexes.Length && editIndexes[i + 1] == editIndexes[i] + 1)
                {
                    var currentEditKind = indexToEditKinds[editIndexes[i + 1]];
                    var isCurrentEditKindUpdate = currentEditKind.HasFlag(SemanticTokenEditKind.Insert) &&
                        currentEditKind.HasFlag(SemanticTokenEditKind.Delete);

                    // If the current edit is combined with an insertion or deletion, only that type is allowed
                    // in the combined edit afterwards. The check below will return true if this is not the case.
                    if (editKind != SemanticTokenEditKind.None && currentEditKind != editKind)
                    {
                        break;
                    }

                    if (!isCurrentEditKindUpdate)
                    {
                        // From now on, all future edits combined with the current edit must be of the same edit
                        // kind as the current edit.
                        editKind = currentEditKind;
                    }

                    if (isCurrentEditKindUpdate || currentEditKind == SemanticTokenEditKind.Insert)
                    {
                        tokensToInsert.AddRange(newGroupedSemanticTokens[editIndexes[i + 1]].ToArray());
                    }

                    if (isCurrentEditKindUpdate || currentEditKind == SemanticTokenEditKind.Delete)
                    {
                        deleteCount += 5;
                    }

                    i++;
                }

                semanticTokensEdits.Add(
                    GenerateEdit(start: initialEditIndex * 5, deleteCount: deleteCount, data: tokensToInsert.ToArray()));

                return i;
            }

            static int AddInsertionEdit(
                SemanticToken[] newGroupedSemanticTokens,
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndexes,
                int i,
                int initialEditIndex)
            {
                var _ = ArrayBuilder<int>.GetInstance(out var tokensToInsert);
                tokensToInsert.AddRange(newGroupedSemanticTokens[initialEditIndex].ToArray());

                // An insert can only be combined with other inserts that directly follow it.
                while (i + 1 < editIndexes.Length && indexToEditKinds[editIndexes[i + 1]] == SemanticTokenEditKind.Insert &&
                    editIndexes[i + 1] == editIndexes[i] + 1)
                {
                    tokensToInsert.AddRange(newGroupedSemanticTokens[editIndexes[i + 1]].ToArray());
                    i++;
                }

                semanticTokensEdits.Add(
                    GenerateEdit(start: initialEditIndex * 5, deleteCount: 0, data: tokensToInsert.ToArray()));
                return i;
            }

            static int AddDeletionEdit(
                Dictionary<int, SemanticTokenEditKind> indexToEditKinds,
                ArrayBuilder<SemanticTokensEdit> semanticTokensEdits,
                int[] editIndexes,
                int i,
                int initialEditIndex)
            {
                var deleteCount = 5;

                // A deletion can only be combined with other deletions that directly follow it.
                while (i + 1 < editIndexes.Length && indexToEditKinds[editIndexes[i + 1]] == SemanticTokenEditKind.Delete &&
                    editIndexes[i + 1] == editIndexes[i] + 1)
                {
                    deleteCount += 5;
                    i++;
                }

                semanticTokensEdits.Add(
                    GenerateEdit(start: initialEditIndex * 5, deleteCount: deleteCount, data: Array.Empty<int>()));
                return i;
            }
        }

        /// <summary>
        /// Converts an array of individual semantic token values to an array of values grouped
        /// together by semantic token.
        /// </summary>
        private static SemanticToken[] ConvertToGroupedSemanticTokens(int[] tokens)
        {
            Contract.ThrowIfTrue(tokens.Length % 5 != 0);
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
                => oldSemanticTokens[oldIndex].Equals(newSemanticTokens[newIndex]);

            public static IEnumerable<SequenceEdit> GetEdits(
                SemanticToken[] oldSemanticTokens, SemanticToken[] newSemanticTokens)
                => s_instance.GetEdits(oldSemanticTokens, oldSemanticTokens.Length, newSemanticTokens, newSemanticTokens.Length);
        }

        /// <summary>
        /// Stores the values that make up the LSP representation of an individual semantic token.
        /// </summary>
        private readonly struct SemanticToken
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

            public int[] ToArray()
            {
                return new int[] { _deltaLine, _deltaStartCharacter, _length, _tokenType, _tokenModifiers };
            }

            public override bool Equals(object? obj)
            {
                return obj is SemanticToken token &&
                       _deltaLine == token._deltaLine &&
                       _deltaStartCharacter == token._deltaStartCharacter &&
                       _length == token._length &&
                       _tokenType == token._tokenType &&
                       _tokenModifiers == token._tokenModifiers;
            }

            public override int GetHashCode()
            {
                return Hash.Combine(_deltaLine.GetHashCode(),
                    Hash.Combine(_deltaStartCharacter.GetHashCode(),
                    Hash.Combine(_length.GetHashCode(),
                    Hash.Combine(_tokenType.GetHashCode(),
                    _tokenModifiers.GetHashCode()))));
            }
        }

        [Flags]
        private enum SemanticTokenEditKind
        {
            None = 0,
            Insert = 1,
            Delete = 2,
        }
    }
}
