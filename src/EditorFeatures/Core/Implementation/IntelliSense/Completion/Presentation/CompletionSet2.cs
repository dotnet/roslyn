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
    internal sealed class CompletionSet3 : CompletionSet2
    {
        private readonly ForegroundThreadAffinitizedObject _foregroundObject = new ForegroundThreadAffinitizedObject();
        private readonly ITextView _textView;
        private readonly ITextBuffer _subjectBuffer;
        private readonly CompletionPresenterSession _completionPresenterSession;
        private Dictionary<CompletionItem, VSCompletion> _completionItemToVSCompletion;
        private Dictionary<VSCompletion, CompletionItem> _vsCompletionToCompletionItem;

        private CompletionRules _completionRules;
        private IReadOnlyList<IntellisenseFilter2> _filters;

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

        public override IReadOnlyList<IIntellisenseFilter> Filters => _filters;

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
            ImmutableArray<CompletionItemFilter> completionItemFilters)
        {
            this._foregroundObject.AssertIsForeground();

            VSCompletion selectedCompletionItem = null;

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            _completionItemToVSCompletion = _completionItemToVSCompletion ?? new Dictionary<CompletionItem, VSCompletion>(completionItems.Count + 1);
            _vsCompletionToCompletionItem = _vsCompletionToCompletionItem ?? new Dictionary<VSCompletion, CompletionItem>(completionItems.Count + 1);

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
            if (!_completionItemToVSCompletion.TryGetValue(item, out value))
            {
                value = new CustomCommitCompletion(
                    _completionPresenterSession,
                    item,
                    displayTextOpt ?? item.DisplayText);
                _completionItemToVSCompletion.Add(item, value);
                _vsCompletionToCompletionItem.Add(value, item);
            }

            return value;
        }

        internal CompletionItem GetCompletionItem(VSCompletion completion)
        {
            CompletionItem completionItem;
            if (_vsCompletionToCompletionItem.TryGetValue(completion, out completionItem))
            {
                return completionItem;
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

#if false
        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
#else
        public IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
#endif
        {
            var rules = this.GetCompletionRules();
            if (rules != null)
            {
                var status = this.SelectionStatus;
                var vsCompletion = status.Completion;
                if (vsCompletion != null)
                {
                    var completionItem = GetCompletionItem(vsCompletion);
                    if (completionItem != null)
                    {
                        var textSnapshot = _subjectBuffer.CurrentSnapshot;
                        var filterText = textSnapshot.GetText(completionItem.FilterSpan.ToSpan());
                        var highlightedSpans = rules.GetHighlightedSpans(completionItem, filterText);
                        if (highlightedSpans != null)
                        {
                            return highlightedSpans.Select(s => s.ToSpan()).ToArray();
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

    internal class IntellisenseFilter2 : IntellisenseFilter
    {
        private readonly CompletionSet3 _completionSet;
        public readonly CompletionItemFilter CompletionItemFilter;

        public IntellisenseFilter2(
            CompletionSet3 completionSet, CompletionItemFilter filter)
            : base(filter.Glyph.GetImageMoniker(), filter.Glyph.ToString(),
                   filter.AccessKey.ToString(), automationText: filter.Glyph.ToString())
        {
            _completionSet = completionSet;
            CompletionItemFilter = filter;
        }

        public override bool IsChecked
        {
            get
            {
                return base.IsChecked;
            }

            set
            {
                base.IsChecked = value;
                _completionSet.OnIntelliSenseFiltersChanged();
            }
        }
    }
}
