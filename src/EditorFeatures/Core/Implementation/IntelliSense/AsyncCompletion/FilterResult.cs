// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion
{
    internal struct FilterResult : IComparable<FilterResult>
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

        public int CompareTo(FilterResult other)
            => IComparableHelper.CompareTo(this, other, GetComparisonComponents);

        private static IEnumerable<IComparable> GetComparisonComponents(FilterResult filterResult)
        {
            var completionItem = filterResult.CompletionItem;
            yield return completionItem.FilterText.GetCaseInsensitivePrefixLength(filterResult.FilterText);
            yield return completionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                ? filterResult.CompletionItem.Rules.MatchPriority
                : MatchPriority.Default;

            yield return completionItem.FilterText.GetCaseSensitivePrefixLength(filterResult.FilterText);
            yield return completionItem.IsPreferredItem();
        }
    }
}
