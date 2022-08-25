// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Recommendations;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed record class CompletionOptions
    {
        public bool TriggerOnTyping { get; init; } = true;
        public bool TriggerOnTypingLetters { get; init; } = true;
        public bool? TriggerOnDeletion { get; init; } = null;
        public bool TriggerInArgumentLists { get; init; } = true;
        public EnterKeyRule EnterKeyBehavior { get; init; } = EnterKeyRule.Default;
        public SnippetsRule SnippetsBehavior { get; init; } = SnippetsRule.Default;
        public bool HideAdvancedMembers { get; init; } = false;
        public bool ShowNameSuggestions { get; init; } = true;
        public bool? ShowItemsFromUnimportedNamespaces { get; init; } = null;
        public bool UnnamedSymbolCompletionDisabled { get; init; } = false;
        public bool TargetTypedCompletionFilter { get; init; } = false;
        public bool TypeImportCompletion { get; init; } = false;
        public bool ProvideDateAndTimeCompletions { get; init; } = true;
        public bool ProvideRegexCompletions { get; init; } = true;
        public bool ForceExpandedCompletionIndexCreation { get; init; } = false;
        public bool UpdateImportCompletionCacheInBackground { get; init; } = false;
        public bool FilterOutOfScopeLocals { get; init; } = true;
        public bool ShowXmlDocCommentCompletion { get; init; } = true;
        public bool? ShowNewSnippetExperience { get; init; } = null;
        public bool SnippetCompletion { get; init; } = false;
        public ExpandedCompletionMode ExpandedCompletionBehavior { get; init; } = ExpandedCompletionMode.AllItems;
        public NamingStylePreferences? NamingStyleFallbackOptions { get; init; } = null;

        public static readonly CompletionOptions Default = new();

        public RecommendationServiceOptions ToRecommendationServiceOptions()
            => new(
                FilterOutOfScopeLocals: FilterOutOfScopeLocals,
                HideAdvancedMembers: HideAdvancedMembers);

        /// <summary>
        /// Whether items from unimported namespaces should be included in the completion list.
        /// This takes into consideration the experiment we are running in addition to the value
        /// from user facing options.
        /// </summary>
        public bool ShouldShowItemsFromUnimportedNamespaces()
        {
            // Don't trigger import completion if the option value is "default" and the experiment is disabled for the user. 
            return ShowItemsFromUnimportedNamespaces ?? TypeImportCompletion;
        }

        /// <summary>
        /// Whether items from new snippet experience should be included in the completion list.
        /// This takes into consideration the experiment we are running in addition to the value
        /// from user facing options.
        /// </summary>
        public bool ShouldShowNewSnippetExperience()
        {
            // Don't trigger snippet completion if the option value is "default" and the experiment is disabled for the user. 
            return ShowNewSnippetExperience ?? SnippetCompletion;
        }
    }
}
