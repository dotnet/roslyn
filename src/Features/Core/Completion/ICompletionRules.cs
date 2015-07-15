// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Completion
{
    internal interface ICompletionRules
    {
        /// <summary>
        /// Returns true if the completion item matches the filter text typed so far.  Returns 'true'
        /// iff the completion item matches and should be included in the filtered completion
        /// results, or false if it should not be.
        /// </summary>
        bool MatchesFilterText(CompletionItem item, string filterText, CompletionTriggerInfo triggerInfo, CompletionFilterReason filterReason);

        /// <summary>
        /// Returns true if item1 is a better completion item than item2 given the provided filter
        /// text, or false if it is not better.
        /// </summary>
        bool IsBetterFilterMatch(CompletionItem item1, CompletionItem item2, string filterText, CompletionTriggerInfo triggerInfo, CompletionFilterReason filterReason);

        /// <summary>
        /// Returns true if the completion item should be "soft" selected, or false if it should be "hard"
        /// selected.
        /// </summary>
        bool ShouldSoftSelectItem(CompletionItem item, string filterText, CompletionTriggerInfo triggerInfo);

        /// <summary>
        /// Called by completion engine when a completion item is committed.  Completion rules can
        /// use this information to affect future calls to MatchesFilterText or IsBetterFilterMatch.
        /// </summary>
        void CompletionItemCommitted(CompletionItem item);

        /// <summary>
        /// Returns true if item1 and item2 are similar enough that only one should be shown in the completion list; otherwise, false.
        /// </summary>
        bool ItemsMatch(CompletionItem item1, CompletionItem item2);
    }
}
