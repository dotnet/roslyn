
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal struct FilterResult
    {
        public readonly CompletionItem CompletionItem;
        public readonly bool MatchedFilterText;
        public readonly string FilterText;

        public FilterResult(CompletionItem completionItem, string filterText, bool matchedFilterText)
        {
            CompletionItem = completionItem;
            MatchedFilterText = matchedFilterText;
            FilterText = filterText;
        }

        public static int Compare(FilterResult result1, FilterResult result2)
        {
            var item1 = result1.CompletionItem;
            var item2 = result2.CompletionItem;

            var prefixLength1 = item1.FilterText.GetCaseInsensitivePrefixLength(result1.FilterText);
            var prefixLength2 = item2.FilterText.GetCaseInsensitivePrefixLength(result2.FilterText);

            // Prefer the item that matches a longer prefix of the filter text.
            if (prefixLength1 != prefixLength2)
            {
                return prefixLength1 > prefixLength2 ? 1 : -1;
            }
            else
            {
                // If the lengths are the same, prefer the one with the higher match priority.
                // But only if it's an item that would have been hard selected.  We don't want
                // to aggressively select an item that was only going to be softly offered.
                var item1Priority = item1.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? item1.Rules.MatchPriority : MatchPriority.Default;
                var item2Priority = item2.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? item2.Rules.MatchPriority : MatchPriority.Default;

                if (item1Priority != item2Priority)
                {
                    return item1Priority > item2Priority ? 1 : -1;
                }

                prefixLength1 = item1.FilterText.GetCaseSensitivePrefixLength(result1.FilterText);
                prefixLength2 = item2.FilterText.GetCaseSensitivePrefixLength(result2.FilterText);

                // If there are "Abc" vs "abc", we should prefer the case typed by user.
                if (prefixLength1 != prefixLength2)
                {
                    return prefixLength1 > prefixLength2 ? 1 : -1;
                }

                if (result1.CompletionItem.IsPreferredItem() != result2.CompletionItem.IsPreferredItem())
                {
                    return result1.CompletionItem.IsPreferredItem() ? 1 : -1;
                }

                return 0;
            }
        }
    }
}
