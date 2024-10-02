// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PatternMatching;

namespace Microsoft.CodeAnalysis.Completion;

internal readonly struct MatchResult(
    CompletionItem completionItem,
    bool shouldBeConsideredMatchingFilterText,
    PatternMatch? patternMatch,
    int index,
    string? matchedAdditionalFilterText,
    int recentItemIndex = -1)
{
    /// <summary>
    /// The CompletionItem used to create this MatchResult.
    /// </summary>
    public readonly CompletionItem CompletionItem = completionItem;

    public readonly PatternMatch? PatternMatch = patternMatch;

    // The value of `ShouldBeConsideredMatchingFilterText` doesn't 100% reflect the actual PatternMatch result.
    // In certain cases, there'd be no match but we'd still want to consider it a match (e.g. when the item is in MRU list,)
    // and this is why PatternMatch can be null. There's also cases it's a match but we want to consider it a non-match
    // (e.g. when not a prefix match in deletion scenario).
    public readonly bool ShouldBeConsideredMatchingFilterText = shouldBeConsideredMatchingFilterText;

    public string FilterTextUsed => MatchedAdditionalFilterText ?? CompletionItem.FilterText;

    // We want to preserve the original alphabetical order for items with same pattern match score,
    // but `ArrayBuilder.Sort` we currently use isn't stable. So we have to add a monotonically increasing 
    // integer to achieve this.
    public readonly int IndexInOriginalSortedOrder = index;
    public readonly int RecentItemIndex = recentItemIndex;

    /// <summary>
    /// If `CompletionItem.AdditionalFilterTexts` was used to create this MatchResult, then this is set to the one that was used.
    /// Otherwise this is set to null.
    /// </summary>
    public readonly string? MatchedAdditionalFilterText = matchedAdditionalFilterText;

    public bool MatchedWithAdditionalFilterTexts => MatchedAdditionalFilterText is not null;

    public static IComparer<MatchResult> SortingComparer { get; } = new Comparer();

    private sealed class Comparer : IComparer<MatchResult>
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
