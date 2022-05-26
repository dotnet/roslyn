// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Differencing
{
    /// <summary>
    /// Calculates Longest Common Subsequence for immutable arrays.
    /// </summary>
    internal abstract class LongestCommonImmutableArraySubsequence<TElement> : LongestCommonSubsequence<ImmutableArray<TElement>>
    {
        protected abstract bool Equals(TElement oldElement, TElement newElement);

        protected sealed override bool ItemsEqual(ImmutableArray<TElement> oldSequence, int oldIndex, ImmutableArray<TElement> newSequence, int newIndex)
            => Equals(oldSequence[oldIndex], newSequence[newIndex]);

        public IEnumerable<SequenceEdit> GetEdits(ImmutableArray<TElement> oldSequence, ImmutableArray<TElement> newSequence)
            => GetEdits(oldSequence, oldSequence.Length, newSequence, newSequence.Length);

        public double ComputeDistance(ImmutableArray<TElement> oldSequence, ImmutableArray<TElement> newSequence)
            => ComputeDistance(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
    }
}
