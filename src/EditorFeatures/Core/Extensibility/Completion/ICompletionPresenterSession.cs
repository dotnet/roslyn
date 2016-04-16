// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICompletionPresenterSession : IIntelliSensePresenterSession
    {
        void PresentItems(
            ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem,
            CompletionItem presetBuilder, bool suggestionMode, bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            IReadOnlyDictionary<CompletionItem, string> completionItemToFilterText);

        void SelectPreviousItem();
        void SelectNextItem();
        void SelectPreviousPageItem();
        void SelectNextPageItem();

        event EventHandler<CompletionItemEventArgs> ItemSelected;
        event EventHandler<CompletionItemEventArgs> ItemCommitted;
        event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
    }
}
