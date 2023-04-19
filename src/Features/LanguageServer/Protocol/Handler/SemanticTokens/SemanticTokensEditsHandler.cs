// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Differencing;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Computes the semantic tokens edits for a file. Clients may make edit requests on a timer,
    /// or every time an edit is made by the user.
    /// </summary>
    internal class SemanticTokensEditsHandler : IRequestHandler<LSP.SemanticTokensDeltaParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensDelta>>
    {
        private readonly SemanticTokensCache _tokensCache;

        public string Method => LSP.Methods.TextDocumentSemanticTokensFullDeltaName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public SemanticTokensEditsHandler(SemanticTokensCache tokensCache)
        {
            _tokensCache = tokensCache;
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensDeltaParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensDelta>> HandleRequestAsync(
            LSP.SemanticTokensDeltaParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(request.PreviousResultId, "previousResultId is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var (newSemanticTokensData, isFinalized) = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document, SemanticTokensCache.TokenTypeToIndex,
                range: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(newSemanticTokensData, "newSemanticTokensData is null.");

            // Getting the cached tokens for the document. If we don't have an applicable cached token set,
            // we can't calculate edits, so we must return all semantic tokens instead. Likewise, if the new
            // token set is empty, there's no need to calculate edits.
            var oldSemanticTokensData = await _tokensCache.GetCachedTokensDataAsync(
                request.TextDocument.Uri, request.PreviousResultId, cancellationToken).ConfigureAwait(false);
            if (oldSemanticTokensData == null || newSemanticTokensData.Length == 0)
            {
                var newResultId = _tokensCache.GetNextResultId();
                var updatedTokens = new RoslynSemanticTokens
                {
                    ResultId = newResultId,
                    Data = newSemanticTokensData,
                    IsFinalized = isFinalized,
                };

                if (newSemanticTokensData.Length > 0)
                {
                    await _tokensCache.UpdateCacheAsync(
                        request.TextDocument.Uri, updatedTokens, cancellationToken).ConfigureAwait(false);
                }

                return updatedTokens;
            }

            var editArray = ComputeSemanticTokensEdits(oldSemanticTokensData, newSemanticTokensData);
            var resultId = request.PreviousResultId;

            // If we have edits, generate a new ResultId. Otherwise, re-use the previous one.
            if (editArray.Length != 0)
            {
                resultId = _tokensCache.GetNextResultId();
                var updatedTokens = new RoslynSemanticTokens
                {
                    ResultId = resultId,
                    Data = newSemanticTokensData,
                    IsFinalized = isFinalized
                };

                await _tokensCache.UpdateCacheAsync(
                    request.TextDocument.Uri, updatedTokens, cancellationToken).ConfigureAwait(false);
            }

            var edits = new RoslynSemanticTokensDelta
            {
                ResultId = resultId,
                Edits = editArray,
                IsFinalized = isFinalized
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

            // We use Roslyn's version of the Myers' Diff Algorithm to compute the minimal edits between
            // the old and new tokens. Edits are computed on an int level, with five ints representing
            // one token. We compute on int level rather than token level to minimize the amount of
            // edits we send back to the client.
            var edits = LongestCommonSemanticTokensSubsequence.GetEdits(oldSemanticTokens, newSemanticTokens);

            var processedEdits = ProcessEdits(newSemanticTokens, edits.ToArray());
            return processedEdits;
        }

        private static LSP.SemanticTokensEdit[] ProcessEdits(
            int[] newSemanticTokens,
            SequenceEdit[] edits)
        {
            using var _ = ArrayBuilder<RoslynSemanticTokensEdit>.GetInstance(out var results);
            var insertIndex = 0;

            // Go through and attempt to combine individual edits into larger edits. By default,
            // edits are returned from Roslyn's LCS ordered from largest -> smallest index.
            // However, to simplify computation, we process edits ordered from smallest -> largest
            // index.
            for (var i = edits.Length - 1; i >= 0; i--)
            {
                var edit = edits[i];

                // Retrieve the most recent edit to see if it can be expanded.
                var editInProgress = results.Count > 0 ? results[^1] : null;

                switch (edit.Kind)
                {
                    case EditKind.Delete:
                        // If we have a deletion edit, we should see if there's an edit in progress
                        // we can combine with. If not, we'll generate a new edit.
                        //
                        // Note we've set up the logic such that deletion edits can be combined with
                        // an insertion edit in progress, but not vice versa. This works out
                        // because the edits list passed into this method always orders the
                        // insertions for a given start index before deletions.
                        if (editInProgress != null &&
                            editInProgress.Start + editInProgress.DeleteCount == edit.OldIndex)
                        {
                            editInProgress.DeleteCount++;
                        }
                        else
                        {
                            results.Add(new RoslynSemanticTokensEdit
                            {
                                Start = edit.OldIndex,
                                DeleteCount = 1,
                            });
                        }

                        break;
                    case EditKind.Insert:
                        // If we have an insertion edit, we should see if there's an insertion edit
                        // in progress we can combine with. If not, we'll generate a new edit.
                        //
                        // As mentioned above, we only combine insertion edits with in-progress
                        // insertion edits.
                        if (editInProgress != null &&
                            editInProgress.Data != null &&
                            editInProgress.Data.Count > 0 &&
                            editInProgress.Start == insertIndex)
                        {
                            editInProgress.Data.Add(newSemanticTokens[edit.NewIndex]);
                        }
                        else
                        {
                            var semanticTokensEdit = new RoslynSemanticTokensEdit
                            {
                                Start = insertIndex,
                                Data = new List<int>
                                {
                                    newSemanticTokens[edit.NewIndex],
                                },
                                DeleteCount = 0,
                            };

                            results.Add(semanticTokensEdit);
                        }

                        break;
                    case EditKind.Update:
                        // For EditKind.Inserts, we need to keep track of where in the old sequence we should be
                        // inserting. This location is based off the location of the previous update.
                        insertIndex = edit.OldIndex + 1;
                        break;
                    default:
                        throw new InvalidOperationException("Only EditKind.Insert and EditKind.Delete are valid.");
                }
            }

            var processedResults = results.Select(e => e.ToSemanticTokensEdit());
            return processedResults.ToArray();
        }

        private sealed class LongestCommonSemanticTokensSubsequence : LongestCommonSubsequence<int[]>
        {
            private static readonly LongestCommonSemanticTokensSubsequence s_instance = new();

            protected override bool ItemsEqual(
                int[] oldSemanticTokens, int oldIndex,
                int[] newSemanticTokens, int newIndex)
                => oldSemanticTokens[oldIndex] == newSemanticTokens[newIndex];

            public static IEnumerable<SequenceEdit> GetEdits(int[] oldSemanticTokens, int[] newSemanticTokens)
            {
                try
                {
                    var edits = s_instance.GetEdits(
                        oldSemanticTokens, oldSemanticTokens.Length, newSemanticTokens, newSemanticTokens.Length);
                    return edits;
                }
                catch (OutOfMemoryException e) when (FatalError.ReportAndCatch(e))
                {
                    // The algorithm is superlinear in memory usage so we might potentially run out in rare cases.
                    // Report telemetry and return no edits.
                    return SpecializedCollections.EmptyEnumerable<SequenceEdit>();
                }
            }
        }

        // We need to have a shim class because SemanticTokensEdit.Data is an array type, so if we
        // operate on it directly then every time we append an element we're allocating a new array.
        private class RoslynSemanticTokensEdit
        {
            /// <summary>
            /// Index where edit begins in the original sequence.
            /// </summary>
            public int Start { get; set; }

            /// <summary>
            /// Number of values to delete from tokens array.
            /// </summary>
            public int DeleteCount { get; set; }

            /// <summary>
            /// Values to add to tokens array.
            /// </summary>
            public IList<int>? Data { get; set; }

            public SemanticTokensEdit ToSemanticTokensEdit()
            {
                return new SemanticTokensEdit
                {
                    Data = Data?.ToArray(),
                    Start = Start,
                    DeleteCount = DeleteCount,
                };
            }
        }
    }
}
