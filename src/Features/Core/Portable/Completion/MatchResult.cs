// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PatternMatching;

namespace Microsoft.CodeAnalysis.Completion
{
    internal readonly struct MatchResult
    {
        /// <summary>
        /// The CompletinoItem used to create this MatchResult.
        /// </summary>
        public readonly CompletionItem CompletionItem;

        public readonly PatternMatch? PatternMatch;

        // The value of `ShouldBeConsideredMatchingFilterText` doesn't 100% refect the actual PatternMatch result.
        // In certain cases, there'd be no match but we'd still want to consider it a match (e.g. when the item is in MRU list,)
        // and this is why PatternMatch can be null. There's also cases it's a match but we want to consider it a non-match
        // (e.g. when not a prefix match in deleteion sceanrio).
        public readonly bool ShouldBeConsideredMatchingFilterText;

        public string FilterTextUsed => _matchedAddtionalFilterText ?? CompletionItem.FilterText;

        // We want to preserve the original alphabetical order for items with same pattern match score,
        // but `ArrayBuilder.Sort` we currently use isn't stable. So we have to add a monotonically increasing 
        // integer to archieve this.
        public readonly int IndexInOriginalSortedOrder;
        public readonly int RecentItemIndex;

        /// <summary>
        /// If `CompletionItem.AdditionalFilterTexts` was used to create this MatchResult, then this is set to the one that was used.
        /// Otherwise this is set to null.
        /// </summary>
        private readonly string? _matchedAddtionalFilterText;

        public bool MatchedWithAdditionalFilterTexts => _matchedAddtionalFilterText is not null;

        public MatchResult(
            CompletionItem completionItem,
            bool shouldBeConsideredMatchingFilterText,
            PatternMatch? patternMatch,
            int index,
            string? matchedAdditionalFilterText,
            int recentItemIndex = -1)
        {
            CompletionItem = completionItem;
            ShouldBeConsideredMatchingFilterText = shouldBeConsideredMatchingFilterText;
            PatternMatch = patternMatch;
            IndexInOriginalSortedOrder = index;
            RecentItemIndex = recentItemIndex;
            _matchedAddtionalFilterText = matchedAdditionalFilterText;
        }

        public static IComparer<MatchResult> SortingComparer { get; } = new Comparer();

        private class Comparer : IComparer<MatchResult>
        {
            // This comparison is used for sorting items in the completion list for the original sorting.

            public int Compare(MatchResult x, MatchResult y)
            {
                var matchX = x.PatternMatch;
                var matchY = y.PatternMatch;

                if (matchX.HasValue)
                {
                    if (matchY.HasValue)
                    {
                        var ret = matchX.Value.CompareTo(matchY.Value);

                        // We'd rank match of FilterText over match of any of AdditionalFilterTexts if they has same pattern match score
                        if (ret == 0)
                            ret = x.MatchedWithAdditionalFilterTexts.CompareTo(y.MatchedWithAdditionalFilterTexts);

                        // We want to preserve the original order for items with same pattern match score.
                        return ret == 0 ? x.IndexInOriginalSortedOrder - y.IndexInOriginalSortedOrder : ret;
                    }

                    return -1;
                }

                if (matchY.HasValue)
                {
                    return 1;
                }

                return x.IndexInOriginalSortedOrder - y.IndexInOriginalSortedOrder;
            }
        }
    }
}
