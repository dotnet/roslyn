// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.Completion
{
    internal readonly record struct CompletionOptions(
        bool TriggerOnTyping = true,
        bool TriggerOnTypingLetters = true,
        bool? TriggerOnDeletion = null,
        bool TriggerInArgumentLists = true,
        EnterKeyRule EnterKeyBehavior = EnterKeyRule.Default,
        SnippetsRule SnippetsBehavior = SnippetsRule.Default,
        bool HideAdvancedMembers = false,
        bool ShowNameSuggestions = true,
        bool? ShowItemsFromUnimportedNamespaces = null,
        bool UnnamedSymbolCompletionDisabled = false,
        bool TargetTypedCompletionFilter = false,
        bool TypeImportCompletion = false,
        bool ProvideDateAndTimeCompletions = true,
        bool ProvideRegexCompletions = true,
        bool ForceExpandedCompletionIndexCreation = false,
        bool UpdateImportCompletionCacheInBackground = false,
        bool FilterOutOfScopeLocals = true,
        bool ShowXmlDocCommentCompletion = true,
        ExpandedCompletionMode ExpandedCompletionBehavior = ExpandedCompletionMode.AllItems)
    {
        // note: must pass at least one parameter to avoid calling default ctor:
        public static readonly CompletionOptions Default = new(TriggerOnTyping: true);

        public RecommendationServiceOptions ToRecommendationServiceOptions()
            => new(
                FilterOutOfScopeLocals: FilterOutOfScopeLocals,
                HideAdvancedMembers: HideAdvancedMembers);

        /// <summary>
        /// Whether items from unimported namespaces should be included in the completion list.
        /// This takes into consideration the experiment we are running in addition to the value
        /// from user facing options.
        /// </summary>
        public bool ShouldShowItemsFromUnimportNamspaces()
        {
            // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
            return ShowItemsFromUnimportedNamespaces ?? TypeImportCompletion;
        }
    }
}
