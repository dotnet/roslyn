// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal sealed class CompletionSet2 : CompletionSet
    {
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;
        private readonly CompletionPresenterSession _completionPresenterSession;
        private Dictionary<CompletionItem, VSCompletion> _completionItemMap;

        public CompletionSet2(CompletionPresenterSession completionPresenterSession, ITextView textView, ITextBuffer subjectBuffer)
        {
            _completionPresenterSession = completionPresenterSession;
            _textView = textView;
            _subjectBuffer = subjectBuffer;
            this.Moniker = "All";
            this.DisplayName = "All";
        }

        internal void SetTrackingSpan(ITrackingSpan trackingSpan)
        {
            this.ApplicableTo = trackingSpan;
        }

        internal void SetCompletionItems(
            IList<CompletionItem> completionItems,
            CompletionItem selectedItem,
            CompletionItem presetBuilder,
            bool suggestionMode,
            bool isSoftSelected)
        {
            VSCompletion selectedCompletionItem = null;

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            _completionItemMap = _completionItemMap ?? new Dictionary<CompletionItem, VSCompletion>(completionItems.Count + 1);

            try
            {
                this.WritableCompletionBuilders.BeginBulkOperation();
                this.WritableCompletionBuilders.Clear();

                var applicableToText = this.ApplicableTo.GetText(this.ApplicableTo.TextBuffer.CurrentSnapshot);
                var filteredBuilder = new CompletionItem(null, applicableToText,
                    this.ApplicableTo.GetSpan(this.ApplicableTo.TextBuffer.CurrentSnapshot).Span.ToTextSpan(), isBuilder: true);

                var showBuilder = suggestionMode || presetBuilder != null;
                var bestBuilder = applicableToText.Length > 0 ? filteredBuilder : presetBuilder ?? filteredBuilder;

                if (showBuilder && bestBuilder != null)
                {
                    var builderCompletion = ConvertCompletionItem(bestBuilder);
                    this.WritableCompletionBuilders.Add(builderCompletion);

                    if (selectedItem.IsBuilder)
                    {
                        selectedCompletionItem = builderCompletion;
                    }
                }
            }
            finally
            {
                this.WritableCompletionBuilders.EndBulkOperation();
            }

            try
            {
                this.WritableCompletions.BeginBulkOperation();
                this.WritableCompletions.Clear();

                foreach (var item in completionItems)
                {
                    var completionItem = ConvertCompletionItem(item);
                    this.WritableCompletions.Add(completionItem);

                    if (item == selectedItem)
                    {
                        selectedCompletionItem = completionItem;
                    }
                }
            }
            finally
            {
                this.WritableCompletions.EndBulkOperation();
            }

            Contract.ThrowIfNull(selectedCompletionItem);
            this.SelectionStatus = new CompletionSelectionStatus(
                selectedCompletionItem, isSelected: !isSoftSelected, isUnique: true);
        }

        private VSCompletion ConvertCompletionItem(CompletionItem item, string displayTextOpt = null)
        {
            VSCompletion value;
            if (!_completionItemMap.TryGetValue(item, out value))
            {
                value = new CustomCommitCompletion(
                    _completionPresenterSession,
                    item,
                    displayTextOpt ?? item.DisplayText);
                _completionItemMap.Add(item, value);
            }

            return value;
        }

        internal CompletionItem GetCompletionItem(VSCompletion completion)
        {
            // Linear search is ok since this is only called by the user manually selecting 
            // an item.  Creating a reverse mapping uses too much memory and affects GCs.
            foreach (var kvp in _completionItemMap)
            {
                if (kvp.Value == completion)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        public override void SelectBestMatch()
        {
            // Do nothing.  We do *not* want the default behavior that the editor has.  We've
            // already computed the best match.
        }

        public override void Filter()
        {
            // Do nothing.  We do *not* want the default behavior that the editor has.  We've
            // already filtered the list.
        }

        public override void Recalculate()
        {
            // Do nothing.  Our controller will already recalculate if necessary.
        }
    }
}
