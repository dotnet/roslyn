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

        protected Dictionary<CompletionItem, VSCompletion> CompletionItemMap;
        protected CompletionItem SuggestionModeItem;

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
            IList<CompletionItem> completionItems,
            CompletionItem selectedItem,
            CompletionItem suggestionModeItem,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            string filterText)
        {
            this.AssertIsForeground();

            // Initialize the completion map to a reasonable default initial size (+1 for the builder)
            CompletionItemMap = CompletionItemMap ?? new Dictionary<CompletionItem, VSCompletion>(completionItems.Count + 1);
            FilterText = filterText;
            SuggestionModeItem = suggestionModeItem;

            this.SetupFilters(completionItemFilters);

            CreateCompletionListBuilder(selectedItem, suggestionModeItem, suggestionMode);
            CreateNormalCompletionListItems(completionItems);

            var selectedCompletionItem = selectedItem != null ? GetVSCompletion(selectedItem) : null;
            VsCompletionSet.SelectionStatus = new CompletionSelectionStatus(
                selectedCompletionItem, 
                isSelected: !isSoftSelected, isUnique: selectedCompletionItem != null);
        }

        private void CreateCompletionListBuilder(
            CompletionItem selectedItem, 
            CompletionItem suggestionModeItem, 
            bool suggestionMode)
        {
            try
            {
                VsCompletionSet.WritableCompletionBuilders.BeginBulkOperation();
                VsCompletionSet.WritableCompletionBuilders.Clear();

                if (suggestionMode)
                {
                    var applicableToText = VsCompletionSet.ApplicableTo.GetText(
                        VsCompletionSet.ApplicableTo.TextBuffer.CurrentSnapshot);

                    var text = applicableToText.Length > 0 ? applicableToText : suggestionModeItem.DisplayText;
                    var vsCompletion = GetVSCompletion(suggestionModeItem, text);
                    
                    VsCompletionSet.WritableCompletionBuilders.Add(vsCompletion);
                }
            }
            finally
            {
                VsCompletionSet.WritableCompletionBuilders.EndBulkOperation();
            }
        }

        private void CreateNormalCompletionListItems(IList<CompletionItem> completionItems)
        {
            try
            {
                VsCompletionSet.WritableCompletions.BeginBulkOperation();
                VsCompletionSet.WritableCompletions.Clear();

                foreach (var item in completionItems)
                {
                    var completionItem = GetVSCompletion(item);
                    VsCompletionSet.WritableCompletions.Add(completionItem);
                }
            }
            finally
            {
                VsCompletionSet.WritableCompletions.EndBulkOperation();
            }
        }

        protected virtual void SetupFilters(ImmutableArray<CompletionItemFilter> completionItemFilters)
        {
        }

        private VSCompletion GetVSCompletion(CompletionItem item, string displayText = null)
        {
            if (!CompletionItemMap.TryGetValue(item, out var value))
            {
                value = new CustomCommitCompletion(CompletionPresenterSession, item);
                CompletionItemMap.Add(item, value);
            }

            value.DisplayText = displayText ?? item.DisplayText;

            return value;
        }

        public CompletionItem GetCompletionItem(VSCompletion completion)
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

        protected Document GetDocument()
        {
            return SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        }
    }
}
