// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Completion
{
    internal readonly struct MatchResult<TEditorCompletionItem>
    {
        public readonly RoslynCompletionItem RoslynCompletionItem;
        public readonly bool MatchedFilterText;

        // In certain cases, there'd be no match but we'd still set `MatchedFilterText` to true,
        // e.g. when the item is in MRU list. Therefore making this nullable.
        public readonly PatternMatch? PatternMatch;

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
            bool matchedFilterText,
            PatternMatch? patternMatch,
            int index)
        {
            RoslynCompletionItem = roslynCompletionItem;
            EditorCompletionItem = editorCompletionItem;
            MatchedFilterText = matchedFilterText;
            PatternMatch = patternMatch;
            _indexInOriginalSortedOrder = index;
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
        public int CompareTo(MatchResult<TEditorCompletionItem> other, string filterText)
            => ComparerWithState.CompareTo(this, other, filterText, s_comparers);

        private static readonly ImmutableArray<Func<MatchResult<TEditorCompletionItem>, string, IComparable>> s_comparers =
            ImmutableArray.Create<Func<MatchResult<TEditorCompletionItem>, string, IComparable>>(
                // Prefer the item that matches a longer prefix of the filter text.
                (f, s) => f.RoslynCompletionItem.FilterText.GetCaseInsensitivePrefixLength(s),
                // If there are "Abc" vs "abc", we should prefer the case typed by user.
                (f, s) => f.RoslynCompletionItem.FilterText.GetCaseSensitivePrefixLength(s),
                // If the lengths are the same, prefer the one with the higher match priority.
                // But only if it's an item that would have been hard selected.  We don't want
                // to aggressively select an item that was only going to be softly offered.
                (f, s) => f.RoslynCompletionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? f.RoslynCompletionItem.Rules.MatchPriority
                    : MatchPriority.Default,
                // Prefer Intellicode items.
                (f, s) => f.RoslynCompletionItem.IsPreferredItem());
    }
}
