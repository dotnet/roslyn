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
        void PresentModels(ITrackingSpan triggerSpan, ImmutableArray<CompletionPresentationData> models, bool suggestionMode);

        void SelectPreviousItem();
        void SelectNextItem();
        void SelectPreviousPageItem();
        void SelectNextPageItem();

        event EventHandler<CompletionItemEventArgs> ItemSelected;
        event EventHandler<CompletionItemEventArgs> ItemCommitted;
        event EventHandler<CompletionListSelectedEventArgs> CompletionListSelected;
    }

    internal class CompletionPresentationData
    {
        public int ModelId { get; }

        public IList<CompletionItem> Items { get; }
        public CompletionItem SelectedItem { get; }
        public CompletionItem PresetBuilder { get; }
        public bool IsSoftSelected { get; }
        public string Title { get; }
        public bool IsSelectedList { get; }

        public CompletionPresentationData(IList<CompletionItem> items, 
            CompletionItem selectedItem, 
            CompletionItem presetBuilder, 
            bool isSoftSelected, 
            int modelId, 
            string title, 
            bool isSelectedModel)
        {
            this.Items = items;
            this.SelectedItem = selectedItem;
            this.PresetBuilder = presetBuilder;
            this.IsSoftSelected = isSoftSelected;
            this.ModelId = modelId;
            this.Title = title;
            this.IsSelectedList = isSelectedModel;
        }
    }
}
