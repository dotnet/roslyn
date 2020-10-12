﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using VSCompletionItem = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal readonly struct MatchResult
    {
        public readonly RoslynCompletionItem RoslynCompletionItem;
        public readonly bool MatchedFilterText;
        public readonly ImmutableArray<Span> HighlightedSpans;

        // In certain cases, there'd be no match but we'd still set `MatchedFilterText` to true,
        // e.g. when the item is in MRU list. Therefore making this nullable.
        public readonly PatternMatch? PatternMatch;

        public readonly VSCompletionItem VSCompletionItem;

        // We want to preserve the original alphabetical order for items with same pattern match score,
        // but `ArrayBuilder.Sort` we currently use isn't stable. So we have to add a monotonically increasing 
        // integer to archieve this.
        private readonly int _indexInOriginalSortedOrder;

        public MatchResult(
            RoslynCompletionItem roslynCompletionItem, VSCompletionItem vsCompletionItem,
            bool matchedFilterText, PatternMatch? patternMatch, int index,
            ImmutableArray<Span> highlightedSpans)
        {
            RoslynCompletionItem = roslynCompletionItem;
            MatchedFilterText = matchedFilterText;
            PatternMatch = patternMatch;
            VSCompletionItem = vsCompletionItem;
            _indexInOriginalSortedOrder = index;
            HighlightedSpans = highlightedSpans;
        }

        public static IComparer<MatchResult> SortingComparer => FilterResultSortingComparer.Instance;

        private class FilterResultSortingComparer : IComparer<MatchResult>
        {
            public static FilterResultSortingComparer Instance { get; } = new FilterResultSortingComparer();

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
                        // We want to preserve the original order for items with same pattern match score.
                        return ret == 0
                            ? x._indexInOriginalSortedOrder - y._indexInOriginalSortedOrder
                            : ret;
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
        public int CompareTo(MatchResult other, string filterText)
            => ComparerWithState.CompareTo(this, other, filterText, s_comparers);

        private static readonly ImmutableArray<Func<MatchResult, string, IComparable>> s_comparers =
            ImmutableArray.Create<Func<MatchResult, string, IComparable>>(
                // Prefer the item that matches a longer prefix of the filter text.
                (f, s) => f.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(s),
                // If the lengths are the same, prefer the one with the higher match priority.
                // But only if it's an item that would have been hard selected.  We don't want
                // to aggressively select an item that was only going to be softly offered.
                (f, s) => f.RoslynCompletionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? f.RoslynCompletionItem.Rules.MatchPriority
                    : MatchPriority.Default,
                // If there are "Abc" vs "abc", we should prefer the case typed by user.
                (f, s) => f.RoslynCompletionItem.FilterText.GetCaseSensitivePrefixLength(s),
                // Prefer Intellicode items.
                (f, s) => f.RoslynCompletionItem.IsPreferredItem());
    }
}
