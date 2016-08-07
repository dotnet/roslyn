using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    /// <summary>
    /// See comment on VisualStudio15CompletionSet for an explanation of how these types 
    /// fit together and where code should go in them.
    /// </summary>
    internal class VisualStudio14CompletionSet : CompletionSet, IVisualStudioCompletionSet
    {
        private readonly Roslyn14CompletionSet _roslynCompletionSet;

        public VisualStudio14CompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            _roslynCompletionSet = new Roslyn14CompletionSet(this,
                completionPresenterSession, textView, subjectBuffer);
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

        #region ICompletionSet - Forward to underlying ICompletionSet

        CompletionSet ICompletionSet.CompletionSet => this;

        void ICompletionSet.SetTrackingSpan(ITrackingSpan trackingSpan)
        {
            _roslynCompletionSet.SetTrackingSpan(trackingSpan);
        }

        void ICompletionSet.SetCompletionItems(IList<PresentationItem> completionItems, PresentationItem selectedItem, PresentationItem presetBuilder, bool suggestionMode, bool isSoftSelected, ImmutableArray<CompletionItemFilter> completionItemFilters, string filterText)
        {
            _roslynCompletionSet.SetCompletionItems(completionItems, selectedItem, presetBuilder, suggestionMode, isSoftSelected, completionItemFilters, filterText);
        }

        PresentationItem ICompletionSet.GetPresentationItem(VSCompletion completion)
        {
            return _roslynCompletionSet.GetPresentationItem(completion);
        }

        #endregion

        #region IVsCompletionSet - Forward to base type.

        string IVisualStudioCompletionSet.DisplayName
        {
            get { return base.DisplayName; }
            set { base.DisplayName = value; }
        }

        string IVisualStudioCompletionSet.Moniker
        {
            get { return base.Moniker; }
            set { base.Moniker = value; }
        }

        ITrackingSpan IVisualStudioCompletionSet.ApplicableTo
        {
            get { return base.ApplicableTo; }
            set { base.ApplicableTo = value; }
        }

        BulkObservableCollection<VSCompletion> IVisualStudioCompletionSet.WritableCompletionBuilders =>
            base.WritableCompletionBuilders;

        BulkObservableCollection<VSCompletion> IVisualStudioCompletionSet.WritableCompletions =>
            base.WritableCompletions;

        CompletionSelectionStatus IVisualStudioCompletionSet.SelectionStatus
        {
            get { return base.SelectionStatus; }
            set { base.SelectionStatus = value; }
        }

        #endregion
    }
}