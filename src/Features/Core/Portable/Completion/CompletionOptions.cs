// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Shared;

namespace Microsoft.CodeAnalysis.Completion;

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
    public bool? ShowItemsFromUnimportedNamespaces { get; init; } = true;
    public bool UnnamedSymbolCompletionDisabled { get; init; } = false;
    public bool TargetTypedCompletionFilter { get; init; } = false;
    public bool ProvideDateAndTimeCompletions { get; init; } = true;
    public bool ProvideRegexCompletions { get; init; } = true;
    public bool PerformSort { get; init; } = true;

    /// <summary>
    /// Force completion APIs to produce complete results, even in cases where caches have not been pre-populated.
    /// This is typically used for testing scenarios, and by public APIs where consumers do not have access to
    /// other internal APIs used to control cache creation and/or wait for caches to be populated before examining
    /// completion results.
    /// </summary>
    public bool ForceExpandedCompletionIndexCreation { get; init; } = false;

    /// <summary>
    /// Set to true to update import completion cache in background if the provider isn't supposed to be triggered in the context.
    /// (cache will always be refreshed when provider is triggered)
    /// </summary>
    public bool UpdateImportCompletionCacheInBackground { get; init; } = false;

    /// <summary>
    /// Whether completion can add import statement as part of committed change.
    /// For example, adding import is not allowed in debugger view.
    /// </summary>
    public bool CanAddImportStatement { get; init; } = true;

    public bool FilterOutOfScopeLocals { get; init; } = true;
    public bool ShowXmlDocCommentCompletion { get; init; } = true;
    public bool? ShowNewSnippetExperienceUserOption { get; init; } = null;
    public bool ShowNewSnippetExperienceFeatureFlag { get; init; } = true;
    public ExpandedCompletionMode ExpandedCompletionBehavior { get; init; } = ExpandedCompletionMode.AllItems;

    public static readonly CompletionOptions Default = new();

    public RecommendationServiceOptions ToRecommendationServiceOptions()
        => new()
        {
            FilterOutOfScopeLocals = FilterOutOfScopeLocals,
            HideAdvancedMembers = HideAdvancedMembers
        };

    /// <summary>
    /// Whether items from unimported namespaces should be included in the completion list.
    /// </summary>
    public bool ShouldShowItemsFromUnimportedNamespaces
        => !ShowItemsFromUnimportedNamespaces.HasValue || ShowItemsFromUnimportedNamespaces.Value;

    /// <summary>
    /// Whether items from new snippet experience should be included in the completion list.
    /// This takes into consideration the experiment we are running in addition to the value
    /// from user facing options.
    /// </summary>
    public bool ShouldShowNewSnippetExperience(Document document)
    {
        // Will be removed once semantic snippets will be added to razor.
        var solution = document.Project.Solution;
        var documentSupportsFeatureService = solution.Services.GetRequiredService<IDocumentSupportsFeatureService>();
        if (!documentSupportsFeatureService.SupportsSemanticSnippets(document))
        {
            return false;
        }

        if (document.IsRazorDocument())
        {
            return false;
        }

        // Don't trigger snippet completion if the option value is "default" and the experiment is disabled for the user. 
        return ShowNewSnippetExperienceUserOption ?? ShowNewSnippetExperienceFeatureFlag;
    }
}
