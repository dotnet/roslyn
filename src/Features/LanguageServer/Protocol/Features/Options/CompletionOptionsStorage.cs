// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion;

internal static class CompletionOptionsStorage
{
    public static CompletionOptions GetCompletionOptions(this IGlobalOptionService options, string language)
        => new()
        {
            TriggerOnTyping = options.GetOption(TriggerOnTyping, language),
            TriggerOnTypingLetters = options.GetOption(TriggerOnTypingLetters, language),
            TriggerOnDeletion = options.GetOption(TriggerOnDeletion, language),
            TriggerInArgumentLists = options.GetOption(TriggerInArgumentLists, language),
            EnterKeyBehavior = options.GetOption(EnterKeyBehavior, language),
            SnippetsBehavior = options.GetOption(SnippetsBehavior, language),
            HideAdvancedMembers = options.GetOption(HideAdvancedMembers, language),
            ShowNameSuggestions = options.GetOption(ShowNameSuggestions, language),
            ShowItemsFromUnimportedNamespaces = options.GetOption(ShowItemsFromUnimportedNamespaces, language),
            UnnamedSymbolCompletionDisabled = options.GetOption(UnnamedSymbolCompletionDisabledFeatureFlag),
            ProvideDateAndTimeCompletions = options.GetOption(ProvideDateAndTimeCompletions, language),
            ProvideRegexCompletions = options.GetOption(ProvideRegexCompletions, language),
            ForceExpandedCompletionIndexCreation = options.GetOption(ForceExpandedCompletionIndexCreation),
            NamingStyleFallbackOptions = options.GetNamingStylePreferences(language),
            ShowNewSnippetExperienceUserOption = options.GetOption(ShowNewSnippetExperienceUserOption, language),
            ShowNewSnippetExperienceFeatureFlag = options.GetOption(ShowNewSnippetExperienceFeatureFlag)
        };

    // feature flags

    public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new("CompletionOptions", "UnnamedSymbolCompletionDisabledFeatureFlag", CompletionOptions.Default.UnnamedSymbolCompletionDisabled);
    public static readonly Option2<bool> ShowNewSnippetExperienceFeatureFlag = new("CompletionOptions", "ShowNewSnippetExperienceFeatureFlag", CompletionOptions.Default.ShowNewSnippetExperienceFeatureFlag);
    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new("CompletionOptions", "HideAdvancedMembers", CompletionOptions.Default.HideAdvancedMembers);
    public static readonly PerLanguageOption2<bool> TriggerOnTyping = new("CompletionOptions", "TriggerOnTyping", CompletionOptions.Default.TriggerOnTyping);
    public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new("CompletionOptions", "TriggerOnTypingLetters", CompletionOptions.Default.TriggerOnTypingLetters);
    public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new("CompletionOptions", "TriggerOnDeletion", CompletionOptions.Default.TriggerOnDeletion);
    public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior = new("CompletionOptions", "EnterKeyBehavior", CompletionOptions.Default.EnterKeyBehavior);
    public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior = new("CompletionOptions", "SnippetsBehavior", CompletionOptions.Default.SnippetsBehavior);
    public static readonly PerLanguageOption2<bool> ShowNameSuggestions = new("CompletionOptions", "ShowNameSuggestions", CompletionOptions.Default.ShowNameSuggestions);

    //Dev16 options

    // Use tri-value so the default state can be used to turn on the feature with experimentation service.
    public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces = new("CompletionOptions", "ShowItemsFromUnimportedNamespaces", CompletionOptions.Default.ShowItemsFromUnimportedNamespaces);

    public static readonly PerLanguageOption2<bool> TriggerInArgumentLists = new("CompletionOptions", "TriggerInArgumentLists", CompletionOptions.Default.TriggerInArgumentLists);

    // Test-only option
    public static readonly Option2<bool> ForceExpandedCompletionIndexCreation = new("CompletionOptions", "ForceExpandedCompletionIndexCreation", defaultValue: false);

    // Embedded languages:

    public static PerLanguageOption2<bool> ProvideRegexCompletions = new("RegularExpressionsOptions", "ProvideRegexCompletions", CompletionOptions.Default.ProvideRegexCompletions);
    public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions = new("DateAndTime", "ProvideDateAndTimeCompletions", CompletionOptions.Default.ProvideDateAndTimeCompletions);
    public static readonly PerLanguageOption2<bool?> ShowNewSnippetExperienceUserOption = new("CompletionOptions", "ShowNewSnippetExperienceUserOption", CompletionOptions.Default.ShowNewSnippetExperienceUserOption);
}
