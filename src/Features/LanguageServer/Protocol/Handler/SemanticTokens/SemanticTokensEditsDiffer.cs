// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    internal class SemanticTokensEditsDiffer : TextDiffer
    {
        private SemanticTokensEditsDiffer(IReadOnlyList<int> oldArray, IReadOnlyList<int> newArray)
        {
            if (oldArray is null)
            {
                throw new ArgumentNullException(nameof(oldArray));
            }

            OldArray = oldArray;
            NewArray = newArray;
        }

        private IReadOnlyList<int> OldArray { get; }
        private IReadOnlyList<int> NewArray { get; }

        protected override int OldTextLength => OldArray.Count;
        protected override int NewTextLength => NewArray.Count;

        private const int MaxArraySize = 50;

        protected override bool ContentEquals(int oldTextIndex, int newTextIndex)
        {
            return OldArray[oldTextIndex] == NewArray[newTextIndex];
        }

        public static async Task<IReadOnlyList<DiffEdit>> ComputeSemanticTokensEditsAsync(
            int[] oldTokens,
            int[] newTokens)
        {
            using var _1 = ArrayBuilder<DiffEdit>.GetInstance(out var edits);
            using var _2 = ArrayBuilder<Task<DiffEdit[]>>.GetInstance(out var tasks);
            var numSets = Math.Max(oldTokens.Length / MaxArraySize, newTokens.Length / MaxArraySize) + 1;
            for (var i = 0; i < numSets; i++)
            {
                var j = i;
                var task = Task.Run(() =>
                {
                    var oldTokenSet = new ArraySegment<int>();
                    var newTokenSet = new ArraySegment<int>();

                    if (oldTokens.Length > j * MaxArraySize)
                    {
                        var offset = j * MaxArraySize;
                        var count = Math.Min(MaxArraySize, oldTokens.Length - offset);
                        oldTokenSet = new ArraySegment<int>(oldTokens, offset, count);
                    }

                    if (newTokens.Length > j * MaxArraySize)
                    {
                        var offset = j * MaxArraySize;
                        var count = Math.Min(MaxArraySize, newTokens.Length - offset);
                        newTokenSet = new ArraySegment<int>(newTokens, offset, count);
                    }

                    var differ = new SemanticTokensEditsDiffer(oldTokenSet, newTokenSet);
                    var currentEditSet = differ.ComputeDiff();

                    using var _ = ArrayBuilder<DiffEdit>.GetInstance(out var adjustedEdits);
                    adjustedEdits.AddRange(currentEditSet.SelectAsArray(edit =>
                    {
                        if (edit.Operation is DiffEdit.Type.Insert)
                        {
                            // max array size
                            return new DiffEdit(DiffEdit.Type.Insert, edit.InsertPosition + (j * MaxArraySize), edit.NewTextPosition + (j * MaxArraySize));
                        }
                        else if (edit.Operation is DiffEdit.Type.Delete)
                        {
                            return new DiffEdit(DiffEdit.Type.Delete, edit.InsertPosition + (j * MaxArraySize), null);
                        }
                        else
                        {
                            throw new ArgumentException("Unexpected EditKind.");
                        }
                    }));

                    return adjustedEdits.ToArray();
                });

                tasks.Add(task);
            }

            var completedTasks = await Task.WhenAll(tasks).ConfigureAwait(false);
            var finalEdits = new List<DiffEdit>();
            foreach (var li in completedTasks)
            {
                finalEdits.AddRange(li);
            }

            return finalEdits.ToArray();
        }
    }
}
