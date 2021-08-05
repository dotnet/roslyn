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
    /// Computes the semantic tokens edits for a file. An edit request is received every 500ms,
    /// or every time an edit is made by the user.
    /// </summary>
    internal class SemanticTokensEditsHandler : IRequestHandler<LSP.SemanticTokensEditsParams, SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>>
    {
        private readonly SemanticTokensCache _tokensCache;

        public string Method => LSP.SemanticTokensMethods.TextDocumentSemanticTokensEditsName;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public SemanticTokensEditsHandler(SemanticTokensCache tokensCache)
        {
            _tokensCache = tokensCache;
        }

        public TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.SemanticTokensEditsParams request)
        {
            Contract.ThrowIfNull(request.TextDocument);
            return request.TextDocument;
        }

        public async Task<SumType<LSP.SemanticTokens, LSP.SemanticTokensEdits>> HandleRequestAsync(
            LSP.SemanticTokensEditsParams request,
            RequestContext context,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
            Contract.ThrowIfNull(request.PreviousResultId, "previousResultId is null.");
            Contract.ThrowIfNull(context.Document, "Document is null.");

            // Even though we want to ultimately pass edits back to LSP, we still need to compute all semantic tokens,
            // both for caching purposes and in order to have a baseline comparison when computing the edits.
            var newSemanticTokensData = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                context.Document, SemanticTokensCache.TokenTypeToIndex,
                range: null, cancellationToken).ConfigureAwait(false);

            Contract.ThrowIfNull(newSemanticTokensData, "newSemanticTokensData is null.");

            // Getting the cached tokens for the document. If we don't have an applicable cached token set,
            // we can't calculate edits, so we must return all semantic tokens instead.
            var oldSemanticTokensData = await _tokensCache.GetCachedTokensDataAsync(
                request.TextDocument.Uri, request.PreviousResultId, cancellationToken).ConfigureAwait(false);
            if (oldSemanticTokensData == null)
            {
                var newResultId = _tokensCache.GetNextResultId();
                return new LSP.SemanticTokens { ResultId = newResultId, Data = newSemanticTokensData };
            }

            var resultId = request.PreviousResultId;
            var editArray = ComputeSemanticTokensEdits(oldSemanticTokensData, newSemanticTokensData);

            // If we have edits, generate a new ResultId. Otherwise, re-use the previous one.
            if (editArray.Length != 0)
            {
                resultId = _tokensCache.GetNextResultId();
                var updatedTokens = new LSP.SemanticTokens { ResultId = resultId, Data = newSemanticTokensData };
                await _tokensCache.UpdateCacheAsync(
                    request.TextDocument.Uri, updatedTokens, cancellationToken).ConfigureAwait(false);
            }

            var edits = new SemanticTokensEdits
            {
                Edits = editArray,
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

            // We use Roslyn's version of the Myers' Diff Algorithm to compute the minimal edits between
            // the old and new tokens. Edits are computed on an int level, with five ints representing
            // one token. We compute on int level rather than token level to minimize the amount of
            // edits we send back to the client.
            var edits = LongestCommonSemanticTokensSubsequence.GetEdits(oldSemanticTokens, newSemanticTokens);

            var processedEdits = ProcessEdits(newSemanticTokens, edits);
            return processedEdits;
        }

        private static LSP.SemanticTokensEdit[] ProcessEdits(
            int[] newSemanticTokens,
            IEnumerable<SequenceEdit> edits)
        {
            using var _ = ArrayBuilder<RoslynSemanticTokensEdit>.GetInstance(out var results);

            // Go through and attempt to combine individual edits into larger edits.
            foreach (var edit in edits)
            {
                var editInProgress = results.Count > 0 ? results[^1] : null;
                switch (edit.Kind)
                {
                    case EditKind.Delete:
                        if (editInProgress != null &&
                            editInProgress.Start + editInProgress.DeleteCount == edit.OldIndex)
                        {
                            editInProgress.DeleteCount += 1;
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
                        if (editInProgress != null &&
                            editInProgress.Data != null &&
                            editInProgress.Data.Count > 0 &&
                            editInProgress.Start == edit.OldIndex)
                        {
                            editInProgress.Data.Add(newSemanticTokens[edit.NewIndex]);
                        }
                        else
                        {
                            var semanticTokensEdit = new RoslynSemanticTokensEdit
                            {
                                Start = edit.OldIndex,
                                Data = new List<int>
                                {
                                    newSemanticTokens[edit.NewIndex],
                                },
                                DeleteCount = 0,
                            };

                            results.Add(semanticTokensEdit);
                        }

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

                    // We don't care about updates and can ignore them since they don't indicate changes.
                    var editsWithIgnoredUpdates = edits.Where(e => e.Kind is not EditKind.Update);

                    // By default, edits are returned largest -> smallest index. For computation purposes later on,
                    // we can want to have the edits ordered smallest -> largest index.
                    return editsWithIgnoredUpdates.Reverse();
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
            public int Start { get; set; }
            public int DeleteCount { get; set; }
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
