// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Completion
{
    internal readonly struct MatchResult<TEditorCompletionItem>
    {
        public readonly RoslynCompletionItem RoslynCompletionItem;

        // The value of `ShouldBeConsideredMatchingFilterText` doesn't 100% refect the actual PatternMatch result.
        // In certain cases, there'd be no match but we'd still want to consider it a match (e.g. when the item is in MRU list,)
        // and this is why PatternMatch can be null. There's also cases it's a match but we want to consider it a non-match
        // (e.g. when not a prefix match in deleteion sceanrio).
        public readonly PatternMatch? PatternMatch;
        public readonly bool ShouldBeConsideredMatchingFilterText;

        // Text used to create this match if it's one of the CompletionItem.AdditionalFilterTexts. null if it's FilterText.
        public readonly string? MatchedAddtionalFilterText;

        public string FilterTextUsed => MatchedAddtionalFilterText ?? RoslynCompletionItem.FilterText;

        public bool MatchedWithAdditionalFilterTexts => MatchedAddtionalFilterText is not null;

        /// <summary>
        /// The actual editor completion item associated with this <see cref="RoslynCompletionItem"/>
        /// In VS for example, this is the associated VS async completion item.
        /// </summary>
        public readonly TEditorCompletionItem EditorCompletionItem;

        // We want to preserve the original alphabetical order for items with same pattern match score,
        // but `ArrayBuilder.Sort` we currently use isn't stable. So we have to add a monotonically increasing 
        // integer to archieve this.
        private readonly int _indexInOriginalSortedOrder;

        public MatchResult(
            RoslynCompletionItem roslynCompletionItem,
            TEditorCompletionItem editorCompletionItem,
            bool shouldBeConsideredMatchingFilterText,
            PatternMatch? patternMatch,
            int index,
            string? matchedAdditionalFilterText)
        {
            RoslynCompletionItem = roslynCompletionItem;
            EditorCompletionItem = editorCompletionItem;
            ShouldBeConsideredMatchingFilterText = shouldBeConsideredMatchingFilterText;
            PatternMatch = patternMatch;
            _indexInOriginalSortedOrder = index;
            MatchedAddtionalFilterText = matchedAdditionalFilterText;
        }

        public static IComparer<MatchResult<TEditorCompletionItem>> SortingComparer => FilterResultSortingComparer.Instance;

        private class FilterResultSortingComparer : IComparer<MatchResult<TEditorCompletionItem>>
        {
            public static FilterResultSortingComparer Instance { get; } = new FilterResultSortingComparer();

            // This comparison is used for sorting items in the completion list for the original sorting.
            public int Compare(MatchResult<TEditorCompletionItem> x, MatchResult<TEditorCompletionItem> y)
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
                        return ret == 0 ? x._indexInOriginalSortedOrder - y._indexInOriginalSortedOrder : ret;
                    }

                    return -1;
                }

                if (matchY.HasValue)
                {
                    return 1;
                }

                return x._indexInOriginalSortedOrder - y._indexInOriginalSortedOrder;
            }
        }

        // This comparison is used in the deletion/backspace scenario for selecting best elements.
        public int CompareTo(MatchResult<TEditorCompletionItem> other, string pattern)
        {
            var thisItem = RoslynCompletionItem;
            var otherItem = other.RoslynCompletionItem;

            // Prefer the item that matches a longer prefix of the filter text.
            var comparison = FilterTextUsed.GetCaseInsensitivePrefixLength(pattern).CompareTo(other.FilterTextUsed.GetCaseInsensitivePrefixLength(pattern));
            if (comparison != 0)
                return comparison;

            // If there are "Abc" vs "abc", we should prefer the case typed by user.
            comparison = FilterTextUsed.GetCaseSensitivePrefixLength(pattern).CompareTo(other.FilterTextUsed.GetCaseSensitivePrefixLength(pattern));
            if (comparison != 0)
                return comparison;

            // If the lengths are the same, prefer the one with the higher match priority.
            // But only if it's an item that would have been hard selected.  We don't want
            // to aggressively select an item that was only going to be softly offered.
            comparison = GetPriority(thisItem).CompareTo(GetPriority(otherItem));
            if (comparison != 0)
                return comparison;

            // Prefer Intellicode items.
            return thisItem.IsPreferredItem().CompareTo(otherItem.IsPreferredItem());

            static int GetPriority(RoslynCompletionItem item)
                => item.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection ? item.Rules.MatchPriority : MatchPriority.Default;
        }
    }
}
