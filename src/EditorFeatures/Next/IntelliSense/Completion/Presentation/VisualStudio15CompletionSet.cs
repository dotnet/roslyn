using System;
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
    /// <see cref="VisualStudio15CompletionSet"/> and <see cref="VisualStudio14CompletionSet"/>
    /// are facades that allow us to properly subclass <see cref="CompletionSet"/> and 
    /// <see cref="CompletionSet2"/> respectively.  This is necessary as this is the editor
    /// extensibility model for completion.  However, this poses a small problem in terms of 
    /// targetting both Dev14 and Dev15.  Dev14 does not know about CompletionSet2, while
    /// CompletionSet2 is necessary to get new functionality (like completion filters).
    /// 
    /// As such, we must derive from different types depending on if we are targetting dev14
    /// or dev15.  In order to do this, while still maximally sharing code, we use a mixin
    /// pattern.  The actual code is in <see cref="Roslyn14CompletionSet"/> and it's derived
    /// class <see cref="Roslyn15CompletionSet"/>.  These two types can share code and properly
    /// specialize behavior through a standard subclassing pattern.  
    /// 
    /// VisualStudio15CompletionSet and VisualStudio14CompletionSet then do nothing but forward
    /// their respective methods to the actual underlying type which has all the real logic.
    /// 
    /// Important! Do not put any actual logic into this type.  Instead, forward any work to
    /// <see cref="VisualStudio15CompletionSet._roslynCompletionSet"/>.  If that code then
    /// needs information from this <see cref="CompletionSet2"/> then expose that data through
    /// the <see cref="IVisualStudioCompletionSet"/> interface.
    /// </summary>
    internal class VisualStudio15CompletionSet : CompletionSet2, IVisualStudioCompletionSet
    {
        private readonly Roslyn15CompletionSet _roslynCompletionSet;

        public VisualStudio15CompletionSet(
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
        {
            _roslynCompletionSet = new Roslyn15CompletionSet(this,
                completionPresenterSession, textView, subjectBuffer);
        }

        public override IReadOnlyList<IIntellisenseFilter> Filters => _roslynCompletionSet.Filters;

        public override IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            return _roslynCompletionSet.GetHighlightedSpansInDisplayText(displayText);
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

        // These functions exist because portions of these properties are protected and thus
        // not settable except through the subclass.  Here we essentially make those properties
        // available so that Roslyn15CompletionSet and Roslyn14CompletionSet can read/write them.

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