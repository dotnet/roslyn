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

    public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new("csharp_disable_unnamed_symbol_completion", CompletionOptions.Default.UnnamedSymbolCompletionDisabled);
    public static readonly Option2<bool> ShowNewSnippetExperienceFeatureFlag = new("csharp_show_new_snippet_experience_feature_flag", CompletionOptions.Default.ShowNewSnippetExperienceFeatureFlag);
    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new("dotnet_hide_advanced_members_in_completion", CompletionOptions.Default.HideAdvancedMembers);
    public static readonly PerLanguageOption2<bool> TriggerOnTyping = new("dotnet_trigger_completion_on_typing", CompletionOptions.Default.TriggerOnTyping);
    public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new("dotnet_trigger_completion_on_typing_letters", CompletionOptions.Default.TriggerOnTypingLetters);
    public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new("dotnet_trigger_completion_on_deletion", CompletionOptions.Default.TriggerOnDeletion);
    public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior = new("dotnet_return_key_completion_behavior", CompletionOptions.Default.EnterKeyBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<EnterKeyRule>());
    public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior = new("dotnet_snippets_behavior", CompletionOptions.Default.SnippetsBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<SnippetsRule>());
    public static readonly PerLanguageOption2<bool> ShowNameSuggestions = new("csharp_show_name_completion_suggestions", CompletionOptions.Default.ShowNameSuggestions);

    //Dev16 options

    // Use tri-value so the default state can be used to turn on the feature with experimentation service.
    public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces = new("dotnet_show_completion_items_from_unimported_namespaces", CompletionOptions.Default.ShowItemsFromUnimportedNamespaces);

    public static readonly PerLanguageOption2<bool> TriggerInArgumentLists = new("dotnet_trigger_completion_in_argument_lists", CompletionOptions.Default.TriggerInArgumentLists);

    // Test-only option
    public static readonly Option2<bool> ForceExpandedCompletionIndexCreation = new("CompletionOptions_ForceExpandedCompletionIndexCreation", defaultValue: false);

    // Embedded languages:

    public static PerLanguageOption2<bool> ProvideRegexCompletions = new("dotnet_provide_regex_completions", CompletionOptions.Default.ProvideRegexCompletions);
    public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions = new("dotnet_provide_date_and_time_completions", CompletionOptions.Default.ProvideDateAndTimeCompletions);
    public static readonly PerLanguageOption2<bool?> ShowNewSnippetExperienceUserOption = new("csharp_show_new_snippet_experience", CompletionOptions.Default.ShowNewSnippetExperienceUserOption);
}
