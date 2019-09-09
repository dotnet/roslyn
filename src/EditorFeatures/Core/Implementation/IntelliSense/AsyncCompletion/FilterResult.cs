
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.PatternMatching;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal struct FilterResult : IComparable<FilterResult>
    {
        public readonly CompletionItem CompletionItem;
        public readonly bool MatchedFilterText;
        public readonly string FilterText;

        // In certain cases, there'd be no match but we'd still set `MatchedFilterText` to true,
        // e.g. when the item is in MRU list. Therefore making this nullable.
        public readonly PatternMatch? PatternMatch;

        public FilterResult(CompletionItem completionItem, string filterText, bool matchedFilterText, PatternMatch? patternMatch)
        {
            CompletionItem = completionItem;
            MatchedFilterText = matchedFilterText;
            FilterText = filterText;
            PatternMatch = patternMatch;
        }

        public int CompareTo(FilterResult other)
        {
            var item1 = this.CompletionItem;
            var item2 = other.CompletionItem;

            var prefixLength1 = item1.FilterText.GetCaseInsensitivePrefixLength(this.FilterText);
            var prefixLength2 = item2.FilterText.GetCaseInsensitivePrefixLength(other.FilterText);

            // Prefer the item that matches a longer prefix of the filter text.
            if (prefixLength1 != prefixLength2)
            {
                return prefixLength1.CompareTo(prefixLength2);
            }

            // If the lengths are the same, prefer the one with the higher match priority.
            // But only if it's an item that would have been hard selected.  We don't want
            // to aggressively select an item that was only going to be softly offered.
            var item1Priority = item1.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                ? item1.Rules.MatchPriority : MatchPriority.Default;
            var item2Priority = item2.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                ? item2.Rules.MatchPriority : MatchPriority.Default;

            if (item1Priority != item2Priority)
            {
                return item1Priority.CompareTo(item2Priority);
            }

            prefixLength1 = item1.FilterText.GetCaseSensitivePrefixLength(this.FilterText);
            prefixLength2 = item2.FilterText.GetCaseSensitivePrefixLength(other.FilterText);

            // If there are "Abc" vs "abc", we should prefer the case typed by user.
            if (prefixLength1 != prefixLength2)
            {
                return prefixLength1.CompareTo(prefixLength2);
            }

            return this.CompletionItem.IsPreferredItem().CompareTo(other.CompletionItem.IsPreferredItem());
        }
    }
}
