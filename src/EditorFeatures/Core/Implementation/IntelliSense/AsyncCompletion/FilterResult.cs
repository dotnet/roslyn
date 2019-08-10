﻿
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        {
            return IComparableHelper.CompareTo(
                this,
                other,
                f => f.CompletionItem.FilterText.GetCaseInsensitivePrefixLength(f.FilterText),
                f => f.CompletionItem.Rules.SelectionBehavior == CompletionItemSelectionBehavior.HardSelection
                    ? f.CompletionItem.Rules.MatchPriority
                    : MatchPriority.Default,
                f => f.CompletionItem.FilterText.GetCaseSensitivePrefixLength(f.FilterText),
                f => f.CompletionItem.IsPreferredItem());
        }
    }
}
