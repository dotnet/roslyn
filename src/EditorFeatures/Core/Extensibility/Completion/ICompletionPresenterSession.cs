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
            ITextSnapshot textSnapshot, ITrackingSpan triggerSpan,
            IList<CompletionItem> items, CompletionItem selectedItem,
            CompletionItem suggestionModeItem, bool suggestionMode, bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText);

        void SelectPreviousItem();
        void SelectNextItem();
        void SelectPreviousPageItem();
        void SelectNextPageItem();

        event EventHandler<CompletionItemEventArgs> ItemSelected;
        event EventHandler<CompletionItemEventArgs> ItemCommitted;
        event EventHandler<CompletionItemFilterStateChangedEventArgs> FilterStateChanged;
    }
}
