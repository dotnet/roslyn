// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensEditsDiffer : TextDiffer
    {
        private static readonly SemanticTokensEditsDiffer s_instance = new();

        // NOTE: This is the ideal size determined by benchmarking. Do not change unless perf tested.
        // See https://github.com/dotnet/roslyn/blob/main/src/Tools/IdeCoreBenchmarks/LSPSemanticTokensBenchmarks.cs.
        private const int MaxArraySize = 50;

        protected override bool ContentEquals(
            IReadOnlyList<int> oldSemanticTokens,
            int oldIndex,
            IReadOnlyList<int> newSemanticTokens,
            int newIndex)
        {
            return oldSemanticTokens[oldIndex] == newSemanticTokens[newIndex];
        }

        public static async Task<IReadOnlyList<DiffEdit>> ComputeSemanticTokensEditsAsync(
            int[] oldTokens,
            int[] newTokens)
        {
            using var _1 = ArrayBuilder<DiffEdit>.GetInstance(out var edits);
            using var _2 = ArrayBuilder<Task<ImmutableArray<DiffEdit>>>.GetInstance(out var tasks);

            // Partition the token sets into smaller pieces so we can do more processing concurrently.
            var numSets = Math.Max(oldTokens.Length / MaxArraySize, newTokens.Length / MaxArraySize) + 1;
            for (var i = 0; i < numSets; i++)
            {
                var setNum = i;
                var task = Task.Run(() =>
                {
                    var (oldTokensSubset, newTokensSubset) = GetTokenSubsets(oldTokens, newTokens, setNum);
                    var currentEditSet = s_instance.ComputeDiff(oldTokensSubset, newTokensSubset);

                    // Adjust the indices of our results since we partitioned them earlier into smaller sets.
                    var adjustedEdits = AdjustEditPositions(oldTokens, setNum, currentEditSet);
                    return adjustedEdits;
                });

                tasks.Add(task);
            }

            // After all the tasks are completed, combine them together.
            var editLists = await Task.WhenAll(tasks).ConfigureAwait(false);
            var combinedEdits = new List<DiffEdit>();
            foreach (var list in editLists)
            {
                combinedEdits.AddRange(list);
            }

            return combinedEdits.ToArray();

            static (ArraySegment<int> oldTokensSubset, ArraySegment<int> newTokensSubset) GetTokenSubsets(
                int[] oldTokens,
                int[] newTokens,
                int setIndex)
            {
                var oldTokensSubset = new ArraySegment<int>();
                var newTokensSubset = new ArraySegment<int>();

                if (oldTokens.Length > setIndex * MaxArraySize)
                {
                    var offset = setIndex * MaxArraySize;
                    var count = Math.Min(MaxArraySize, oldTokens.Length - offset);
                    oldTokensSubset = new ArraySegment<int>(oldTokens, offset, count);
                }

                if (newTokens.Length > setIndex * MaxArraySize)
                {
                    var offset = setIndex * MaxArraySize;
                    var count = Math.Min(MaxArraySize, newTokens.Length - offset);
                    newTokensSubset = new ArraySegment<int>(newTokens, offset, count);
                }

                return (oldTokensSubset, newTokensSubset);
            }

            static ImmutableArray<DiffEdit> AdjustEditPositions(
                int[] oldTokens,
                int setNum,
                IReadOnlyList<DiffEdit> currentEditSet)
            {
                var adjustedEdits = currentEditSet.SelectAsArray(edit =>
                {
                    // Due to the way partitioning works, we may have to adjust the position of the edit.
                    // For example, let's say we have a partition size of 50, old array 'O' with size 30, and
                    // new array 'N' with size 90. The first partition would be (O[0..30), N[0..50)), while the
                    // second partition would be (empty, N[50..90)). Any insertion or deletion within the second
                    // partition would be considered out of bounds when the LSP client applies the edit since the
                    // client applies edits in reverse order from largest->smallest index.
                    // In this case, what we want to do instead is set the position to be at the very end of the
                    // old array's boundary.
                    var position = edit.Position + (setNum * MaxArraySize);
                    position = Math.Min(position, oldTokens.Length);

                    return edit.Operation switch
                    {
                        DiffEdit.Type.Insert => new DiffEdit(DiffEdit.Type.Insert, position, newTextPosition: edit.NewTextPosition + (setNum * MaxArraySize)),
                        DiffEdit.Type.Delete => new DiffEdit(DiffEdit.Type.Delete, position, null),
                        _ => throw ExceptionUtilities.UnexpectedValue(edit.Operation),
                    };
                });

                return adjustedEdits;
            }
        }
    }
}
