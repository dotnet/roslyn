using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    /// <summary>
    /// See comment on <see cref="VisualStudio15CompletionSet"/> for an explanation of how
    /// these types fit together and where code should go in them.
    ///
    /// This class is where all code that would normally be in our derived type of 
    /// <see cref="CompletionSet2"/> should go.  <see cref="VisualStudio15CompletionSet"/> 
    /// should then just forward all calls to us and we can share all our Dev14 logic with 
    /// <see cref="Roslyn14CompletionSet"/>.
    /// </summary>
    internal class Roslyn15CompletionSet : Roslyn14CompletionSet
    {
        private CompletionHelper _completionHelper;
        public IReadOnlyList<IntellisenseFilter2> Filters;

        private readonly bool _highlightMatchingPortions;
        private readonly bool _showFilters;

        public Roslyn15CompletionSet(
            IVisualStudioCompletionSet vsCompletionSet,
            CompletionPresenterSession completionPresenterSession,
            ITextView textView,
            ITextBuffer subjectBuffer)
            : base(vsCompletionSet, completionPresenterSession, textView, subjectBuffer)
        {
            var document = GetDocument();

            if (document != null)
            {
                var options = document.Options;
                _highlightMatchingPortions = options.GetOption(CompletionOptions.HighlightMatchingPortionsOfCompletionListItems);
                _showFilters = options.GetOption(CompletionOptions.ShowCompletionItemFilters);
            }
        }

        protected override void SetupFilters(ImmutableArray<CompletionItemFilter> completionItemFilters)
        {
            // If more than one filter was provided, then present it to the user.
            if (_showFilters && Filters == null && completionItemFilters.Length > 1)
            {
                Filters = completionItemFilters.Select(f => new IntellisenseFilter2(this, f))
                                               .ToArray();
            }
        }

        private CompletionHelper GetCompletionHelper()
        {
            this.AssertIsForeground();
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

        public IReadOnlyList<Span> GetHighlightedSpansInDisplayText(string displayText)
        {
            if (_highlightMatchingPortions && !string.IsNullOrWhiteSpace(FilterText))
            {
                var completionHelper = this.GetCompletionHelper();
                if (completionHelper != null)
                {
                    var presentationItem = this.PresentationItemMap.Keys.FirstOrDefault(k => k.Item.DisplayText == displayText);

                    if (presentationItem != null && !presentationItem.IsSuggestionModeItem)
                    {
                        var highlightedSpans = completionHelper.GetHighlightedSpans(
                            presentationItem.Item, FilterText, CultureInfo.CurrentCulture);
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
            this.CompletionPresenterSession.OnIntelliSenseFiltersChanged(
                Filters.ToImmutableDictionary(f => f.CompletionItemFilter, f => f.IsChecked));
        }
    }
}