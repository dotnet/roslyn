using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    /// <summary>
    /// See comment on VisualStudio15CompletionSet for an explanation of how these types 
    /// fit together and where code should go in them.
    /// 
    /// This class is where all code that would normally be in our derived type of 
    /// <see cref="CompletionSet"/> should go.  By putting it here, we can share it
    /// in our Dev14 and Dev15 completion sets (which otherwise have to derive from 
    /// different <see cref="CompletionSet"/> types.
    /// 
    /// <see cref="VisualStudio14CompletionSet"/> should then just forward all calls 
    /// to us.
    /// </summary>
    internal class Roslyn14CompletionSet : ForegroundThreadAffinitizedObject
    {
        protected readonly IVisualStudioCompletionSet VsCompletionSet;

        private readonly ITextView _textView;

        protected readonly ITextBuffer SubjectBuffer;
        protected readonly CompletionPresenterSession CompletionPresenterSession;
        protected Dictionary<PresentationItem, VSCompletion> PresentationItemMap;

        protected string FilterText;

        public Roslyn14CompletionSet(
            IVisualStudioCompletionSet vsCompletionSet,
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            VsCompletionSet = vsCompletionSet;

            CompletionPresenterSession = completionPresenterSession;
            _textView = textView;
            SubjectBuffer = subjectBuffer;

            vsCompletionSet.Moniker = "All";
            vsCompletionSet.DisplayName = "All";
        }

        public void SetTrackingSpan(ITrackingSpan trackingSpan)
        {
            VsCompletionSet.ApplicableTo = trackingSpan;
        }

        public void SetCompletionItems(
            IList<PresentationItem> completionItems,
            PresentationItem selectedItem,
            PresentationItem presetBuilder,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            string filterText)
        {
            this.AssertIsForeground();

            VSCompletion selectedCompletionItem = null;

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            PresentationItemMap = PresentationItemMap ?? new Dictionary<PresentationItem, VSCompletion>(completionItems.Count + 1);
            FilterText = filterText;

            try
            {
                VsCompletionSet.WritableCompletionBuilders.BeginBulkOperation();
                VsCompletionSet.WritableCompletionBuilders.Clear();

                this.SetupFilters(completionItemFilters);

                var applicableToText = VsCompletionSet.ApplicableTo.GetText(
                    VsCompletionSet.ApplicableTo.TextBuffer.CurrentSnapshot);

                SimplePresentationItem filteredSuggestionModeItem = null;
                if (selectedItem != null)
                {
                    var completionItem = CompletionItem.Create(displayText: applicableToText);
                    completionItem.Span = VsCompletionSet.ApplicableTo.GetSpan(
                        VsCompletionSet.ApplicableTo.TextBuffer.CurrentSnapshot).Span.ToTextSpan();

                    filteredSuggestionModeItem = new SimplePresentationItem(
                        completionItem,
                        selectedItem.CompletionService,
                        isSuggestionModeItem: true);
                }

                var showBuilder = suggestionMode || presetBuilder != null;
                var bestSuggestionModeItem = applicableToText.Length > 0 ? filteredSuggestionModeItem : presetBuilder ?? filteredSuggestionModeItem;

                if (showBuilder && bestSuggestionModeItem != null)
                {
                    var suggestionModeCompletion = GetVSCompletion(bestSuggestionModeItem);
                    VsCompletionSet.WritableCompletionBuilders.Add(suggestionModeCompletion);

                    if (selectedItem != null && selectedItem.IsSuggestionModeItem)
                    {
                        selectedCompletionItem = suggestionModeCompletion;
                    }
                }
            }
            finally
            {
                VsCompletionSet.WritableCompletionBuilders.EndBulkOperation();
            }

            try
            {
                VsCompletionSet.WritableCompletions.BeginBulkOperation();
                VsCompletionSet.WritableCompletions.Clear();

                foreach (var item in completionItems)
                {
                    var completionItem = GetVSCompletion(item);
                    VsCompletionSet.WritableCompletions.Add(completionItem);

                    if (item == selectedItem)
                    {
                        selectedCompletionItem = completionItem;
                    }
                }
            }
            finally
            {
                VsCompletionSet.WritableCompletions.EndBulkOperation();
            }

            VsCompletionSet.SelectionStatus = new CompletionSelectionStatus(
                selectedCompletionItem, isSelected: !isSoftSelected, isUnique: selectedCompletionItem != null);
        }

        protected virtual void SetupFilters(ImmutableArray<CompletionItemFilter> completionItemFilters)
        {
        }

        private VSCompletion GetVSCompletion(PresentationItem item)
        {
            VSCompletion value;
            if (!PresentationItemMap.TryGetValue(item, out value))
            {
                value = new CustomCommitCompletion(
                    CompletionPresenterSession,
                    item);
                PresentationItemMap.Add(item, value);
            }

            return value;
        }

        public PresentationItem GetPresentationItem(VSCompletion completion)
        {
            // Linear search is ok since this is only called by the user manually selecting 
            // an item.  Creating a reverse mapping uses too much memory and affects GCs.
            foreach (var kvp in PresentationItemMap)
            {
                if (kvp.Value == completion)
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        protected Document GetDocument()
        {
            return SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        }
    }
}