// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ICompletionPresenterSession : IIntelliSensePresenterSession
    {
        void PresentItems(ITrackingSpan triggerSpan, IList<CompletionItem> items, CompletionItem selectedItem,
            CompletionItem presetBuilder, bool suggestionMode, bool isSoftSelected);

        void SelectPreviousItem();
        void SelectNextItem();
        void SelectPreviousPageItem();
        void SelectNextPageItem();

        event EventHandler<CompletionItemEventArgs> ItemSelected;
        event EventHandler<CompletionItemEventArgs> ItemCommitted;
    }
}
