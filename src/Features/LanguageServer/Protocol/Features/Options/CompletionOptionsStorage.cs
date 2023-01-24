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

    public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new("CompletionOptions_UnnamedSymbolCompletionDisabledFeatureFlag", CompletionOptions.Default.UnnamedSymbolCompletionDisabled);
    public static readonly Option2<bool> ShowNewSnippetExperienceFeatureFlag = new("CompletionOptions_ShowNewSnippetExperienceFeatureFlag", CompletionOptions.Default.ShowNewSnippetExperienceFeatureFlag);
    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new("CompletionOptions_HideAdvancedMembers", CompletionOptions.Default.HideAdvancedMembers);
    public static readonly PerLanguageOption2<bool> TriggerOnTyping = new("CompletionOptions_TriggerOnTyping", CompletionOptions.Default.TriggerOnTyping);
    public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new("CompletionOptions_TriggerOnTypingLetters", CompletionOptions.Default.TriggerOnTypingLetters);
    public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new("CompletionOptions_TriggerOnDeletion", CompletionOptions.Default.TriggerOnDeletion);
    public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior = new("CompletionOptions_EnterKeyBehavior", CompletionOptions.Default.EnterKeyBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<EnterKeyRule>());
    public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior = new("CompletionOptions_SnippetsBehavior", CompletionOptions.Default.SnippetsBehavior);
    public static readonly PerLanguageOption2<bool> ShowNameSuggestions = new("CompletionOptions_ShowNameSuggestions", CompletionOptions.Default.ShowNameSuggestions);

    //Dev16 options

    // Use tri-value so the default state can be used to turn on the feature with experimentation service.
    public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces = new("CompletionOptions_ShowItemsFromUnimportedNamespaces", CompletionOptions.Default.ShowItemsFromUnimportedNamespaces);

    public static readonly PerLanguageOption2<bool> TriggerInArgumentLists = new("CompletionOptions_TriggerInArgumentLists", CompletionOptions.Default.TriggerInArgumentLists);

    // Test-only option
    public static readonly Option2<bool> ForceExpandedCompletionIndexCreation = new("CompletionOptions_ForceExpandedCompletionIndexCreation", defaultValue: false);

    // Embedded languages:

    public static PerLanguageOption2<bool> ProvideRegexCompletions = new("RegularExpressionsOptions_ProvideRegexCompletions", CompletionOptions.Default.ProvideRegexCompletions);
    public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions = new("DateAndTime_ProvideDateAndTimeCompletions", CompletionOptions.Default.ProvideDateAndTimeCompletions);
    public static readonly PerLanguageOption2<bool?> ShowNewSnippetExperienceUserOption = new("CompletionOptions_ShowNewSnippetExperienceUserOption", CompletionOptions.Default.ShowNewSnippetExperienceUserOption);
}
