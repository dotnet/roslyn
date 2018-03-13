using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;
using CACompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal class RoslynCompletionSet : CompletionSet2
    {
        private readonly ITextView _textView;
        private readonly ForegroundThreadAffinitizedObject _foregroundThread = new ForegroundThreadAffinitizedObject();

        private readonly bool _highlightMatchingPortions;
        private readonly bool _showFilters;
        private IReadOnlyList<IntellisenseFilter2> _filters;

        protected readonly ITextBuffer SubjectBuffer;
        protected readonly CompletionPresenterSession CompletionPresenterSession;
        private CompletionHelper _completionHelper;

        protected Dictionary<CACompletionItem, VSCompletion> CompletionItemMap;
        protected CACompletionItem SuggestionModeItem;

        protected string FilterText;

        public RoslynCompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            CompletionPresenterSession = completionPresenterSession;
            _textView = textView;
            SubjectBuffer = subjectBuffer;

            this.Moniker = "All";
            this.DisplayName = "All";

            var document = GetDocument();

            if (document != null)
            {
                var options = document.Project.Solution.Options;
                _highlightMatchingPortions = options.GetOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems, document.Project.Language);
                _showFilters = options.GetOption(CompletionOptions.ShowCompletionItemFilters, document.Project.Language);
            }
        }

        public void SetTrackingSpan(ITrackingSpan trackingSpan)
        {
            this.ApplicableTo = trackingSpan;
        }

        public override void SelectBestMatch()
        {
            // Do nothing.  We do *not* want the default behavior that the editor has.  We've
            // already computed the best match.

            // this will get called right after completion set is painted on the screen.
            // we will use this chance to report completion set performance
            CompletionPresenterSession.ReportPerformance();
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

        public void SetCompletionItems(
            IList<CACompletionItem> completionItems,
            CACompletionItem selectedItem,
            CACompletionItem suggestionModeItem,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            string filterText)
        {
            _foregroundThread.AssertIsForeground();

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            CompletionItemMap = CompletionItemMap ?? new Dictionary<CACompletionItem, VSCompletion>(completionItems.Count + 1);
            FilterText = filterText;
            SuggestionModeItem = suggestionModeItem;

            // If more than one filter was provided, then present it to the user.
            if (_showFilters && _filters == null && completionItemFilters.Length > 1)
            {
                _filters = completionItemFilters.Select(f => new IntellisenseFilter2(this, f))
                                               .ToArray();
            }

            CreateCompletionListBuilder(selectedItem, suggestionModeItem, suggestionMode);
            CreateNormalCompletionListItems(completionItems);

            var selectedCompletionItem = selectedItem != null ? GetVSCompletion(selectedItem) : null;
            SelectionStatus = new CompletionSelectionStatus(
                selectedCompletionItem,
                isSelected: !isSoftSelected, isUnique: selectedCompletionItem != null);
        }

        private void CreateCompletionListBuilder(
            CACompletionItem selectedItem,
            CACompletionItem suggestionModeItem,
            bool suggestionMode)
        {
            try
            {
                WritableCompletionBuilders.BeginBulkOperation();
                WritableCompletionBuilders.Clear();

                if (suggestionMode)
                {
                    var applicableToText = ApplicableTo.GetText(
                        ApplicableTo.TextBuffer.CurrentSnapshot);

                    var text = applicableToText.Length > 0 ? applicableToText : suggestionModeItem.DisplayText;
                    var vsCompletion = GetVSCompletion(suggestionModeItem, text);

                    WritableCompletionBuilders.Add(vsCompletion);
                }
            }
            finally
            {
                WritableCompletionBuilders.EndBulkOperation();
            }
        }

        private void CreateNormalCompletionListItems(IList<CACompletionItem> completionItems)
        {
            try
            {
                WritableCompletions.BeginBulkOperation();
                WritableCompletions.Clear();

                foreach (var item in completionItems)
                {
                    var completionItem = GetVSCompletion(item);
                    WritableCompletions.Add(completionItem);
                }
            }
            finally
            {
                WritableCompletions.EndBulkOperation();
            }
        }

        private VSCompletion GetVSCompletion(CACompletionItem item, string displayText = null)
        {
            if (!CompletionItemMap.TryGetValue(item, out var value))
            {
                value = new CustomCommitCompletion(CompletionPresenterSession, item);
                CompletionItemMap.Add(item, value);
            }

            value.DisplayText = displayText ?? item.DisplayText;

            return value;
        }

        public CACompletionItem GetCompletionItem(VSCompletion completion)
        {
            // Linear search is ok since this is only called by the user manually selecting 
            // an item.  Creating a reverse mapping uses too much memory and affects GCs.
            foreach (var kvp in CompletionItemMap)
            {
                if (kvp.Value == completion)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        private Document GetDocument()
        {
            return SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        }

        public override IReadOnlyList<IIntellisenseFilter> Filters => _filters;

        private CompletionHelper GetCompletionHelper()
        {
            _foregroundThread.AssertIsForeground();
            if (_completionHelper == null)
            {
                var document = GetDocument();
                if (document != null)
                {
                    _completionHelper = CompletionHelper.GetHelper(document);
                }
            }

            return _completionHelper;
        }

        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            if (SuggestionModeItem != null && SuggestionModeItem.DisplayText == displayText)
            {
                // Don't highlight the builder-completion-item.
                return null;
            }

            var pattern = this.FilterText;
            if (_highlightMatchingPortions && !string.IsNullOrWhiteSpace(pattern))
            {
                var completionHelper = this.GetCompletionHelper();
                if (completionHelper != null)
                {
                    var highlightedSpans = completionHelper.GetHighlightedSpans(
                        displayText, pattern, CultureInfo.CurrentCulture);

                    return highlightedSpans.SelectAsArray(s => s.ToSpan());
                }
            }

            return null;
        }

        internal void OnIntelliSenseFiltersChanged()
        {
            this.CompletionPresenterSession.OnIntelliSenseFiltersChanged(
                _filters.ToImmutableDictionary(f => f.CompletionItemFilter, f => f.IsChecked));
        }
    }
}
