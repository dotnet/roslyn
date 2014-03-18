// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum BestIndexKind
    {
        None,
        Best,
        Ambiguous
    }

    internal struct BestIndex
    {
        internal readonly BestIndexKind Kind;
        internal readonly int Best;
        internal readonly int Ambiguous1;
        internal readonly int Ambiguous2;

        public static BestIndex None() { return new BestIndex(BestIndexKind.None, 0, 0, 0); }
        public static BestIndex HasBest(int best) { return new BestIndex(BestIndexKind.Best, best, 0, 0); }
        public static BestIndex IsAmbiguous(int ambig1, int ambig2) { return new BestIndex(BestIndexKind.Ambiguous, 0, ambig1, ambig2); }

        private BestIndex(BestIndexKind kind, int best, int ambig1, int ambig2)
        {
            this.Kind = kind;
            this.Best = best;
            this.Ambiguous1 = ambig1;
            this.Ambiguous2 = ambig2;
        }
    }

    internal static class ImmutableArrayExtensions
    {
        public static T First<T>(this ImmutableArray<T> items, IComparer<T> compare)
        {
            if (items.IsEmpty)
            {
                throw new System.ArgumentException(CSharpResources.ItemsMustBeNonEmpty);
            }

            T candidate = items[0];
            for (int i = 1; i < items.Length; ++i)
            {
                T item = items[i];
                if (compare.Compare(item, candidate) < 0)
                {
                    candidate = item;
                }
            }

            return candidate;
        }

        public static int Count<T>(this ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Length; ++i)
            {
                if (predicate(items[i]))
                {
                    ++count;
                }
            }
            return count;
        }

        // Return the index of the *unique* item in the array that matches the predicate,
        // or null if there is not one.
        public static BestIndex UniqueIndex<T>(this ImmutableArray<T> items, Func<T, bool> predicate)
        {
            if (items.IsEmpty)
            {
                return BestIndex.None();
            }

            int? result = null;
            for (int i = 0; i < items.Length; ++i)
            {
                if (predicate(items[i]))
                {
                    if (result == null)
                    {
                        result = i;
                    }
                    else
                    {
                        // Not unique.
                        return BestIndex.IsAmbiguous(result.Value, i);
                    }
                }
            }

            return result == null ? BestIndex.None() : BestIndex.HasBest(result.Value);
        }

        // This method takes an array of items and a predicate which filters out the valid items.
        // From the valid items we find the index of the *unique best item* in the array.
        // In order for a valid item x to be considered best, x must be better than every other
        // item. The "better" relation must be consistent; that is:
        //
        // better(x,y) == Left     requires that    better(y,x) == Right
        // better(x,y) == Right    requires that    better(y,x) == Left
        // better(x,y) == Neither  requires that    better(y,x) == Neither 
        //
        // It is possible for the array to contain the same item twice; if it does then
        // the duplicate is ignored. That is, having the "best" item twice does not preclude
        // it from being the best.

        // UNDONE: Update this to give a BestIndex result that indicates ambiguity.
        public static int? UniqueBestValidIndex<T>(this ImmutableArray<T> items, Func<T, bool> valid, Func<T, T, BetterResult> better)
        {
            if (items.IsEmpty)
            {
                return null;
            }

            int? candidateIndex = null;
            T candidateItem = default(T);

            for (int currentIndex = 0; currentIndex < items.Length; ++currentIndex)
            {
                T currentItem = items[currentIndex];
                if (!valid(currentItem))
                {
                    continue;
                }

                if (candidateIndex == null)
                {
                    candidateIndex = currentIndex;
                    candidateItem = currentItem;
                    continue;
                }

                BetterResult result = better(candidateItem, currentItem);

                if (result == BetterResult.Equal)
                {
                    // The list had the same item twice. Just ignore it.
                    continue;
                }
                else if (result == BetterResult.Neither)
                {
                    // Neither the current item nor the candidate item are better,
                    // and therefore neither of them can be the best. We no longer
                    // have a candidate for best item.
                    candidateIndex = null;
                    candidateItem = default(T);
                }
                else if (result == BetterResult.Right)
                {
                    // The candidate is worse than the current item, so replace it
                    // with the current item.
                    candidateIndex = currentIndex;
                    candidateItem = currentItem;
                }
                // Otherwise, the candidate is better than the current item, so
                // it continues to be the candidate.
            }

            if (candidateIndex == null)
            {
                return null;
            }

            // We had a candidate that was better than everything that came *after* it.
            // Now verify that it was better than everything that came before it.

            for (int currentIndex = 0; currentIndex < candidateIndex.Value; ++currentIndex)
            {
                T currentItem = items[currentIndex];
                if (!valid(currentItem))
                {
                    continue;
                }

                BetterResult result = better(candidateItem, currentItem);
                if (result != BetterResult.Left && result != BetterResult.Equal)
                {
                    // The candidate was not better than everything that came before it. There is 
                    // no best item.
                    return null;
                }
            }

            // The candidate was better than everything that came before it.

            return candidateIndex;
        }
    }
}
