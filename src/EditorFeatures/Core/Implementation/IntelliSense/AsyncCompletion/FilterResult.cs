// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
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
            => ComparerWithState.CompareTo(this, other, s_comparers);

        private readonly static ImmutableArray<Func<FilterResult, IComparable>> s_comparers =
            ImmutableArray.Create<Func<FilterResult, IComparable>>(
                // Prefer the item that matches a longer prefix of the filter text.
                f => f.CompletionItem.FilterText.GetCaseInsensitivePrefixLength(f.FilterText),
                // If the lengths are the same, prefer the one with the higher match priority.
                // But only if it's an item that would have been hard selected.  We don't want
                // to aggressively select an item that was only going to be softly offered.
                f => f.CompletionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? f.CompletionItem.Rules.MatchPriority
                    : MatchPriority.Default,
                // If there are "Abc" vs "abc", we should prefer the case typed by user.
                f => f.CompletionItem.FilterText.GetCaseSensitivePrefixLength(f.FilterText),
                // Prefer Intellicode items.
                f => f.CompletionItem.IsPreferredItem());
    }
}
