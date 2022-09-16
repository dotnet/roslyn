// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
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
            TypeImportCompletion = options.GetOption(TypeImportCompletionFeatureFlag),
            ProvideDateAndTimeCompletions = options.GetOption(ProvideDateAndTimeCompletions, language),
            ProvideRegexCompletions = options.GetOption(ProvideRegexCompletions, language),
            ForceExpandedCompletionIndexCreation = options.GetOption(ForceExpandedCompletionIndexCreation),
            UpdateImportCompletionCacheInBackground = options.GetOption(UpdateImportCompletionCacheInBackground),
            NamingStyleFallbackOptions = options.GetNamingStylePreferences(language),
            ShowNewSnippetExperience = options.GetOption(ShowNewSnippetExperience, language),
            SnippetCompletion = options.GetOption(ShowNewSnippetExperienceFeatureFlag)
        };

    // feature flags

    public static readonly Option2<bool> TypeImportCompletionFeatureFlag = new(nameof(CompletionOptions), nameof(TypeImportCompletionFeatureFlag),
        CompletionOptions.Default.TypeImportCompletion,
        new FeatureFlagStorageLocation("Roslyn.TypeImportCompletion"));

    public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new(nameof(CompletionOptions), nameof(UnnamedSymbolCompletionDisabledFeatureFlag),
        CompletionOptions.Default.UnnamedSymbolCompletionDisabled,
        new FeatureFlagStorageLocation("Roslyn.UnnamedSymbolCompletionDisabled"));

    public static readonly Option2<bool> ShowNewSnippetExperienceFeatureFlag = new(nameof(CompletionOptions), nameof(ShowNewSnippetExperienceFeatureFlag),
        CompletionOptions.Default.SnippetCompletion,
        new FeatureFlagStorageLocation("Roslyn.SnippetCompletion"));

    public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new(
        "CompletionOptions", "HideAdvancedMembers", CompletionOptions.Default.HideAdvancedMembers,
        new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Hide Advanced Auto List Members"));

    public static readonly PerLanguageOption2<bool> TriggerOnTyping = new(
        "CompletionOptions", "TriggerOnTyping", CompletionOptions.Default.TriggerOnTyping,
        new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Auto List Members"));

    public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new(nameof(CompletionOptions), nameof(TriggerOnTypingLetters), CompletionOptions.Default.TriggerOnTypingLetters,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters"));

    public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new(nameof(CompletionOptions), nameof(TriggerOnDeletion), CompletionOptions.Default.TriggerOnDeletion,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion"));

    public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior =
        new(nameof(CompletionOptions), nameof(EnterKeyBehavior), CompletionOptions.Default.EnterKeyBehavior,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.EnterKeyBehavior"));

    public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior =
        new(nameof(CompletionOptions), nameof(SnippetsBehavior), CompletionOptions.Default.SnippetsBehavior,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SnippetsBehavior"));

    public static readonly PerLanguageOption2<bool> ShowNameSuggestions =
        new(nameof(CompletionOptions), nameof(ShowNameSuggestions), CompletionOptions.Default.ShowNameSuggestions,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowNameSuggestions"));

    //Dev16 options

    // Use tri-value so the default state can be used to turn on the feature with experimentation service.
    public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces =
        new(nameof(CompletionOptions), nameof(ShowItemsFromUnimportedNamespaces), CompletionOptions.Default.ShowItemsFromUnimportedNamespaces,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces"));

    public static readonly PerLanguageOption2<bool> TriggerInArgumentLists =
        new(nameof(CompletionOptions), nameof(TriggerInArgumentLists), CompletionOptions.Default.TriggerInArgumentLists,
        storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerInArgumentLists"));

    // Test-only option
    public static readonly Option2<bool> ForceExpandedCompletionIndexCreation
        = new(nameof(CompletionOptions), nameof(ForceExpandedCompletionIndexCreation), defaultValue: false);

    // Set to true to update import completion cache in background if the provider isn't supposed to be triggered in the context.
    // (cache will alsways be refreshed when provider is triggered)
    public static readonly Option2<bool> UpdateImportCompletionCacheInBackground
        = new(nameof(CompletionOptions), nameof(UpdateImportCompletionCacheInBackground), defaultValue: false);

    // Embedded languages:

    public static PerLanguageOption2<bool> ProvideRegexCompletions =
        new(
            "RegularExpressionsOptions",
            nameof(ProvideRegexCompletions),
            CompletionOptions.Default.ProvideRegexCompletions,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions"));

    public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions =
        new(
            "DateAndTime",
            nameof(ProvideDateAndTimeCompletions),
            CompletionOptions.Default.ProvideDateAndTimeCompletions,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeCompletions"));

    public static readonly PerLanguageOption2<bool?> ShowNewSnippetExperience
        = new(nameof(CompletionOptions), nameof(ShowNewSnippetExperience), CompletionOptions.Default.ShowNewSnippetExperience,
            storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowNewSnippetExperience"));
}
