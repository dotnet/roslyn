// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion
{
    internal readonly struct MatchResult
    {
        public readonly CompletionItem CompletionItem;

        // The value of `ShouldBeConsideredMatchingFilterText` doesn't 100% refect the actual PatternMatch result.
        // In certain cases, there'd be no match but we'd still want to consider it a match (e.g. when the item is in MRU list,)
        // and this is why PatternMatch can be null. There's also cases it's a match but we want to consider it a non-match
        // (e.g. when not a prefix match in deleteion sceanrio).
        public readonly PatternMatch? PatternMatch;
        public readonly bool ShouldBeConsideredMatchingFilterText;

        // Text used to create this match if it's one of the CompletionItem.AdditionalFilterTexts. null if it's FilterText.
        public readonly string? MatchedAddtionalFilterText;

        public string FilterTextUsed => MatchedAddtionalFilterText ?? CompletionItem.FilterText;

        public bool MatchedWithAdditionalFilterTexts => MatchedAddtionalFilterText is not null;

        // We want to preserve the original alphabetical order for items with same pattern match score,
        // but `ArrayBuilder.Sort` we currently use isn't stable. So we have to add a monotonically increasing 
        // integer to archieve this.
        public readonly int IndexInOriginalSortedOrder;

        public MatchResult(
            CompletionItem completionItem,
            bool shouldBeConsideredMatchingFilterText,
            PatternMatch? patternMatch,
            int index,
            string? matchedAdditionalFilterText)
        {
            CompletionItem = completionItem;
            ShouldBeConsideredMatchingFilterText = shouldBeConsideredMatchingFilterText;
            PatternMatch = patternMatch;
            IndexInOriginalSortedOrder = index;
            MatchedAddtionalFilterText = matchedAdditionalFilterText;
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

        /// <summary>
        /// This comparison is used in the deletion/backspace scenario for selecting best elements.
        /// </summary>
        public static int CompareForDeletion(MatchResult x, MatchResult y, string pattern)
        {
            var xItem = x.CompletionItem;
            var yItem = y.CompletionItem;

            // Prefer the item that matches a longer prefix of the filter text.
            var comparison = x.FilterTextUsed.GetCaseInsensitivePrefixLength(pattern).CompareTo(y.FilterTextUsed.GetCaseInsensitivePrefixLength(pattern));
            if (comparison != 0)
                return comparison;

            // If there are "Abc" vs "abc", we should prefer the case typed by user.
            comparison = x.FilterTextUsed.GetCaseSensitivePrefixLength(pattern).CompareTo(y.FilterTextUsed.GetCaseSensitivePrefixLength(pattern));
            if (comparison != 0)
                return comparison;

            // If the lengths are the same, prefer the one with the higher match priority.
            // But only if it's an item that would have been hard selected.  We don't want
            // to aggressively select an item that was only going to be softly offered.
            comparison = GetPriority(xItem).CompareTo(GetPriority(yItem));
            if (comparison != 0)
                return comparison;

            // Prefer Intellicode items.
            return xItem.IsPreferredItem().CompareTo(yItem.IsPreferredItem());

            static int GetPriority(CompletionItem item)
                => item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection ? item.Rules.MatchPriority : MatchPriority.Default;
        }
    }
}
