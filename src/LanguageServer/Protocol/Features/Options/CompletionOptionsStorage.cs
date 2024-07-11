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
            TriggerOnDeletion = language switch
            {
                LanguageNames.CSharp => options.GetOption(TriggerOnTypingLetters, language) && options.GetOption(TriggerOnDeletion, language) is true,
                // If the option is null (i.e. default) or 'true', then we want to trigger completion.
                // Only if the option is false do we not want to trigger.
                LanguageNames.VisualBasic => options.GetOption(TriggerOnDeletion, language) is not false,
                // Other languages might want to get completion options, like Razor, just forward the call to option service when it happens.
                _ => options.GetOption(TriggerOnDeletion, language),
            },
            TriggerInArgumentLists = options.GetOption(TriggerInArgumentLists, language),
            EnterKeyBehavior = options.GetOption(EnterKeyBehavior, language),
            SnippetsBehavior = options.GetOption(SnippetsBehavior, language),
            HideAdvancedMembers = options.GetOption(MemberDisplayOptionsStorage.HideAdvancedMembers, language),
            ShowNameSuggestions = options.GetOption(ShowNameSuggestions, language),
            ShowItemsFromUnimportedNamespaces = options.GetOption(ShowItemsFromUnimportedNamespaces, language),
            UnnamedSymbolCompletionDisabled = options.GetOption(UnnamedSymbolCompletionDisabledFeatureFlag),
            ProvideDateAndTimeCompletions = options.GetOption(ProvideDateAndTimeCompletions, language),
            ProvideRegexCompletions = options.GetOption(ProvideRegexCompletions, language),
            ForceExpandedCompletionIndexCreation = options.GetOption(ForceExpandedCompletionIndexCreation),
            ShowNewSnippetExperienceUserOption = options.GetOption(ShowNewSnippetExperienceUserOption, language),
            ShowNewSnippetExperienceFeatureFlag = options.GetOption(ShowNewSnippetExperienceFeatureFlag)
        };

    private static readonly OptionGroup s_completionOptionGroup = new(name: "completion", description: "");

    // feature flags

    public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new("dotnet_disable_unnamed_symbol_completion", CompletionOptions.Default.UnnamedSymbolCompletionDisabled, group: s_completionOptionGroup);
    public static readonly Option2<bool> ShowNewSnippetExperienceFeatureFlag = new("dotnet_show_new_snippet_experience_feature_flag", CompletionOptions.Default.ShowNewSnippetExperienceFeatureFlag, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool> TriggerOnTyping = new("dotnet_trigger_completion_on_typing", CompletionOptions.Default.TriggerOnTyping, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new("dotnet_trigger_completion_on_typing_letters", CompletionOptions.Default.TriggerOnTypingLetters, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new("dotnet_trigger_completion_on_deletion", CompletionOptions.Default.TriggerOnDeletion, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior = new("dotnet_return_key_completion_behavior", CompletionOptions.Default.EnterKeyBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<EnterKeyRule>(), group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior = new("dotnet_snippets_behavior", CompletionOptions.Default.SnippetsBehavior, serializer: EditorConfigValueSerializer.CreateSerializerForEnum<SnippetsRule>(), group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool> ShowNameSuggestions = new("dotnet_show_name_completion_suggestions", CompletionOptions.Default.ShowNameSuggestions, group: s_completionOptionGroup);

    //Dev16 options

    // Use tri-value so the default state can be used to turn on the feature with experimentation service.
    public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces = new("dotnet_show_completion_items_from_unimported_namespaces", CompletionOptions.Default.ShowItemsFromUnimportedNamespaces, group: s_completionOptionGroup);

    public static readonly PerLanguageOption2<bool> TriggerInArgumentLists = new("dotnet_trigger_completion_in_argument_lists", CompletionOptions.Default.TriggerInArgumentLists, group: s_completionOptionGroup);

    // Test-only option
    public static readonly Option2<bool> ForceExpandedCompletionIndexCreation = new("CompletionOptions_ForceExpandedCompletionIndexCreation", defaultValue: false);

    // Embedded languages:

    public static PerLanguageOption2<bool> ProvideRegexCompletions = new("dotnet_provide_regex_completions", CompletionOptions.Default.ProvideRegexCompletions, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions = new("dotnet_provide_date_and_time_completions", CompletionOptions.Default.ProvideDateAndTimeCompletions, group: s_completionOptionGroup);
    public static readonly PerLanguageOption2<bool?> ShowNewSnippetExperienceUserOption = new("dotnet_show_new_snippet_experience", CompletionOptions.Default.ShowNewSnippetExperienceUserOption, group: s_completionOptionGroup);
}
