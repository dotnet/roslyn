using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using VSCompletion = Microsoft.VisualStudio.Language.Intellisense.Completion;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.Presentation
{
    internal interface ICompletionSet
    {
        event EventHandler<ValueChangedEventArgs<CompletionSelectionStatus>> SelectionStatusChanged;
        void SetTrackingSpan(ITrackingSpan trackingSpan);

        CompletionSet CompletionSet { get; }

        void SetCompletionItems(
            IList<CompletionItem> completionItems,
            CompletionItem selectedItem,
            CompletionItem suggestionModeItem,
            bool suggestionMode,
            bool isSoftSelected,
            ImmutableArray<CompletionItemFilter> completionItemFilters,
            string filterText);
        CompletionItem GetCompletionItem(VSCompletion completion);
    }
}
