// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
#if NEWCOMPLETION
    internal sealed class CompletionSet3 : CompletionSet2
#else
    internal sealed class CompletionSet3 : CompletionSet
#endif
    {
        private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;
        private readonly CompletionPresenterSession _completionPresenterSession;
        private Dictionary<CompletionItem, VSCompletion> _completionItemToVSCompletion;

        private CompletionRules _completionRules;
        private IReadOnlyList<IntellisenseFilter2> _filters;
        private IReadOnlyDictionary<CompletionItem, string> _completionItemToFilterText;

        public CompletionSet3(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            _completionPresenterSession = completionPresenterSession;
            _textView = textView;
            _subjectBuffer = subjectBuffer;
            this.Moniker = "All";
            this.DisplayName = "All";
        }

#if NEWCOMPLETION
        public override IReadOnlyList<IIntellisenseFilter> Filters => _filters;
#endif

        internal void SetTrackingSpan(ITrackingSpan trackingSpan)
        {
            this.ApplicableTo = trackingSpan;
        }

        internal void SetCompletionItems(
            IList<CompletionItem> completionItems,
            CompletionItem selectedItem,
            CompletionItem presetBuilder,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            IReadOnlyDictionary<CompletionItem, string> completionItemToFilterText)
        {
            this._foregroundObject.AssertIsForeground();

            VSCompletion selectedCompletionItem = null;

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            _completionItemToVSCompletion = _completionItemToVSCompletion ?? new Dictionary<CompletionItem, VSCompletion>(completionItems.Count + 1);
            _completionItemToFilterText = completionItemToFilterText;

            try
            {
                this.WritableCompletionBuilders.BeginBulkOperation();
                this.WritableCompletionBuilders.Clear();

                // If more than one filter was provided, then present it to the user.
                if (_filters == null && completionItemFilters.Length > 1)
                {
                    _filters = completionItemFilters.Select(f => new IntellisenseFilter2(this, f))
                                                    .ToArray();
                }

                var applicableToText = this.ApplicableTo.GetText(this.ApplicableTo.TextBuffer.CurrentSnapshot);
                var filteredBuilder = new CompletionItem(null, applicableToText,
                    this.ApplicableTo.GetSpan(this.ApplicableTo.TextBuffer.CurrentSnapshot).Span.ToTextSpan(), isBuilder: true);

                var showBuilder = suggestionMode || presetBuilder != null;
                var bestBuilder = applicableToText.Length > 0 ? filteredBuilder : presetBuilder ?? filteredBuilder;

                if (showBuilder && bestBuilder != null)
                {
                    var builderCompletion = ConvertCompletionItem(bestBuilder);
                    this.WritableCompletionBuilders.Add(builderCompletion);

                    if (selectedItem != null && selectedItem.IsBuilder)
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

            this.SelectionStatus = new CompletionSelectionStatus(
                selectedCompletionItem, isSelected: !isSoftSelected, isUnique: selectedCompletionItem != null);
        }

        private VSCompletion ConvertCompletionItem(CompletionItem item, string displayTextOpt = null)
        {
            VSCompletion value;
            if (!_completionItemToVSCompletion.TryGetValue(item, out value))
            {
                value = new CustomCommitCompletion(
                    _completionPresenterSession,
                    item,
                    displayTextOpt ?? item.DisplayText);
                _completionItemToVSCompletion.Add(item, value);
            }

            return value;
        }

        internal CompletionItem GetCompletionItem(VSCompletion completion)
        {
            // Linear search is ok since this is only called by the user manually selecting 
            // an item.  Creating a reverse mapping uses too much memory and affects GCs.
            foreach (var kvp in _completionItemToVSCompletion)
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

        private CompletionRules GetCompletionRules()
        {
            _foregroundObject.AssertIsForeground();
            if (_completionRules == null)
            {
                var document = _subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document != null)
                {
                    var service = document.Project.LanguageServices.GetService<ICompletionService>();
                    if (service != null)
                    {
                        _completionRules = service.GetCompletionRules();
                    }
                }
            }

            return _completionRules;
        }

#if NEWCOMPLETION
        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
#else
        public IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
#endif
        {
            if (_completionItemToFilterText != null)
            {
                var rules = this.GetCompletionRules();
                if (rules != null)
                {
                    var completionItem = this._completionItemToVSCompletion.Keys.FirstOrDefault(k => k.DisplayText == displayText);

                    if (completionItem != null && !completionItem.IsBuilder)
                    {
                        string filterText;
                        if (_completionItemToFilterText.TryGetValue(completionItem, out filterText))
                        {
                            var highlightedSpans = rules.GetHighlightedSpans(completionItem, filterText);
                            if (highlightedSpans != null)
                            {
                                return highlightedSpans.Select(s => s.ToSpan()).ToArray();
                            }
                        }
                    }
                }
            }

            return null;
        }

        internal void OnIntelliSenseFiltersChanged()
        {
            this._completionPresenterSession.OnIntelliSenseFiltersChanged(_filters);
        }
    }
}
