// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return Equals(oldSequence[oldIndex], newSequence[newIndex]);
        }

        public IEnumerable<SequenceEdit> GetEdits(ImmutableArray<TElement> oldSequence, ImmutableArray<TElement> newSequence)
        {
            return GetEdits(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
        }

        public double ComputeDistance(ImmutableArray<TElement> oldSequence, ImmutableArray<TElement> newSequence)
        {
            return ComputeDistance(oldSequence, oldSequence.Length, newSequence, newSequence.Length);
        }
    }
}
