﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Recommendations;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Completion
{
    internal record struct CompletionOptions(
        bool TriggerOnTyping,
        bool TriggerOnTypingLetters,
        bool? TriggerOnDeletion,
        bool TriggerInArgumentLists,
        bool IsExpandedCompletion,
        EnterKeyRule EnterKeyBehavior,
        SnippetsRule SnippetsBehavior,
        bool HideAdvancedMembers,
        bool ShowNameSuggestions,
        bool? ShowItemsFromUnimportedNamespaces,
        bool UnnamedSymbolCompletionDisabled,
        bool TargetTypedCompletionFilter,
        bool TypeImportCompletion,
        bool ProvideDateAndTimeCompletions,
        bool ProvideRegexCompletions,
        int TimeoutInMillisecondsForExtensionMethodImportCompletion,
        bool FilterOutOfScopeLocals = true,
        bool ShowXmlDocCommentCompletion = true)
    {
        public static readonly CompletionOptions Default
          = new(
              TriggerOnTyping: Metadata.TriggerOnTyping.DefaultValue,
              TriggerOnTypingLetters: Metadata.TriggerOnTypingLetters.DefaultValue,
              TriggerOnDeletion: Metadata.TriggerOnDeletion.DefaultValue,
              TriggerInArgumentLists: Metadata.TriggerInArgumentLists.DefaultValue,
              IsExpandedCompletion: Metadata.IsExpandedCompletion.DefaultValue,
              EnterKeyBehavior: Metadata.EnterKeyBehavior.DefaultValue,
              SnippetsBehavior: Metadata.SnippetsBehavior.DefaultValue,
              HideAdvancedMembers: Metadata.HideAdvancedMembers.DefaultValue,
              ShowNameSuggestions: Metadata.ShowNameSuggestions.DefaultValue,
              ShowItemsFromUnimportedNamespaces: Metadata.ShowItemsFromUnimportedNamespaces.DefaultValue,
              UnnamedSymbolCompletionDisabled: Metadata.UnnamedSymbolCompletionDisabledFeatureFlag.DefaultValue,
              TargetTypedCompletionFilter: Metadata.TargetTypedCompletionFilterFeatureFlag.DefaultValue,
              TypeImportCompletion: Metadata.TypeImportCompletionFeatureFlag.DefaultValue,
              ProvideDateAndTimeCompletions: Metadata.ProvideDateAndTimeCompletions.DefaultValue,
              ProvideRegexCompletions: Metadata.ProvideRegexCompletions.DefaultValue,
              TimeoutInMillisecondsForExtensionMethodImportCompletion: Metadata.TimeoutInMillisecondsForExtensionMethodImportCompletion.DefaultValue);

        public static CompletionOptions From(Project project)
            => From(project.Solution.Options, project.Language);

        public static CompletionOptions From(OptionSet options, string language)
          => new(
              TriggerOnTyping: options.GetOption(Metadata.TriggerOnTyping, language),
              TriggerOnTypingLetters: options.GetOption(Metadata.TriggerOnTypingLetters, language),
              TriggerOnDeletion: options.GetOption(Metadata.TriggerOnDeletion, language),
              TriggerInArgumentLists: options.GetOption(Metadata.TriggerInArgumentLists, language),
              IsExpandedCompletion: options.GetOption(Metadata.IsExpandedCompletion),
              EnterKeyBehavior: options.GetOption(Metadata.EnterKeyBehavior, language),
              SnippetsBehavior: options.GetOption(Metadata.SnippetsBehavior, language),
              HideAdvancedMembers: options.GetOption(Metadata.HideAdvancedMembers, language),
              ShowNameSuggestions: options.GetOption(Metadata.ShowNameSuggestions, language),
              ShowItemsFromUnimportedNamespaces: options.GetOption(Metadata.ShowItemsFromUnimportedNamespaces, language),
              UnnamedSymbolCompletionDisabled: options.GetOption(Metadata.UnnamedSymbolCompletionDisabledFeatureFlag),
              TargetTypedCompletionFilter: options.GetOption(Metadata.TargetTypedCompletionFilterFeatureFlag),
              TypeImportCompletion: options.GetOption(Metadata.TypeImportCompletionFeatureFlag),
              ProvideDateAndTimeCompletions: options.GetOption(Metadata.ProvideDateAndTimeCompletions, language),
              ProvideRegexCompletions: options.GetOption(Metadata.ProvideRegexCompletions, language),
              TimeoutInMillisecondsForExtensionMethodImportCompletion: options.GetOption(Metadata.TimeoutInMillisecondsForExtensionMethodImportCompletion));

        public OptionSet WithChangedOptions(OptionSet set, string language)
            => set.
                WithChangedOption(Metadata.TriggerOnTyping, language, TriggerOnTyping).
                WithChangedOption(Metadata.TriggerOnTypingLetters, language, TriggerOnTypingLetters).
                WithChangedOption(Metadata.TriggerOnDeletion, language, TriggerOnDeletion).
                WithChangedOption(Metadata.TriggerInArgumentLists, language, TriggerInArgumentLists).
                WithChangedOption(Metadata.IsExpandedCompletion, IsExpandedCompletion).
                WithChangedOption(Metadata.EnterKeyBehavior, language, EnterKeyBehavior).
                WithChangedOption(Metadata.SnippetsBehavior, language, SnippetsBehavior).
                WithChangedOption(Metadata.HideAdvancedMembers, language, HideAdvancedMembers).
                WithChangedOption(Metadata.ShowNameSuggestions, language, ShowNameSuggestions).
                WithChangedOption(Metadata.ShowItemsFromUnimportedNamespaces, language, ShowItemsFromUnimportedNamespaces).
                WithChangedOption(Metadata.UnnamedSymbolCompletionDisabledFeatureFlag, UnnamedSymbolCompletionDisabled).
                WithChangedOption(Metadata.TargetTypedCompletionFilterFeatureFlag, TargetTypedCompletionFilter).
                WithChangedOption(Metadata.TypeImportCompletionFeatureFlag, TypeImportCompletion).
                WithChangedOption(Metadata.ProvideDateAndTimeCompletions, language, ProvideDateAndTimeCompletions).
                WithChangedOption(Metadata.ProvideRegexCompletions, language, ProvideRegexCompletions).
                WithChangedOption(Metadata.TimeoutInMillisecondsForExtensionMethodImportCompletion, TimeoutInMillisecondsForExtensionMethodImportCompletion);

        public RecommendationServiceOptions ToRecommendationServiceOptions()
            => new(
                FilterOutOfScopeLocals: FilterOutOfScopeLocals,
                HideAdvancedMembers: HideAdvancedMembers);

        public OptionSet ToSet(string language)
            => WithChangedOptions(OptionValueSet.Empty, language);

        [ExportOptionProvider, Shared]
        internal sealed class Metadata : IOptionProvider
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Metadata()
            {
            }

            public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
                TypeImportCompletionFeatureFlag,
                TargetTypedCompletionFilterFeatureFlag,
                UnnamedSymbolCompletionDisabledFeatureFlag,
                HideAdvancedMembers,
                TriggerOnTyping,
                TriggerOnTypingLetters,
                EnterKeyBehavior,
                SnippetsBehavior,
                ShowItemsFromUnimportedNamespaces,
                TriggerInArgumentLists,
                ProvideRegexCompletions,
                ProvideDateAndTimeCompletions);

            // feature flags

            public static readonly Option2<bool> TypeImportCompletionFeatureFlag = new(nameof(CompletionOptions), nameof(TypeImportCompletionFeatureFlag), defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.TypeImportCompletion"));

            public static readonly Option2<bool> TargetTypedCompletionFilterFeatureFlag = new(nameof(CompletionOptions), nameof(TargetTypedCompletionFilterFeatureFlag), defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.TargetTypedCompletionFilter"));

            public static readonly Option2<bool> UnnamedSymbolCompletionDisabledFeatureFlag = new(nameof(CompletionOptions), nameof(UnnamedSymbolCompletionDisabledFeatureFlag), defaultValue: false,
                new FeatureFlagStorageLocation("Roslyn.UnnamedSymbolCompletionDisabled"));

            // This is serialized by the Visual Studio-specific LanguageSettingsPersister
            public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new(nameof(CompletionOptions), nameof(HideAdvancedMembers), defaultValue: false);

            // This is serialized by the Visual Studio-specific LanguageSettingsPersister
            public static readonly PerLanguageOption2<bool> TriggerOnTyping = new(nameof(CompletionOptions), nameof(TriggerOnTyping), defaultValue: true);

            public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters = new(nameof(CompletionOptions), nameof(TriggerOnTypingLetters), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters"));

            public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new(nameof(CompletionOptions), nameof(TriggerOnDeletion), defaultValue: null,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion"));

            public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior =
                new(nameof(CompletionOptions), nameof(EnterKeyBehavior), defaultValue: EnterKeyRule.Default,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.EnterKeyBehavior"));

            public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior =
                new(nameof(CompletionOptions), nameof(SnippetsBehavior), defaultValue: SnippetsRule.Default,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SnippetsBehavior"));

            public static readonly PerLanguageOption2<bool> ShowNameSuggestions =
                new(nameof(CompletionOptions), nameof(ShowNameSuggestions), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowNameSuggestions"));

            //Dev16 options

            // Use tri-value so the default state can be used to turn on the feature with experimentation service.
            public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces =
                new(nameof(CompletionOptions), nameof(ShowItemsFromUnimportedNamespaces), defaultValue: null,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces"));

            public static readonly PerLanguageOption2<bool> TriggerInArgumentLists =
                new(nameof(CompletionOptions), nameof(TriggerInArgumentLists), defaultValue: true,
                storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerInArgumentLists"));

            /// <summary>
            /// Indicates if the completion is trigger by toggle the expander.
            /// </summary>
            public static readonly Option2<bool> IsExpandedCompletion
                = new("CompletionServiceOptions", nameof(IsExpandedCompletion), defaultValue: false);

            /// <summary>
            /// Timeout value used for time-boxing completion of unimported extension methods.
            /// Value less than 0 means no timebox; value == 0 means immediate timeout (for testing purpose)
            /// </summary>
            public static readonly Option2<int> TimeoutInMillisecondsForExtensionMethodImportCompletion
                = new("CompletionServiceOptions", nameof(TimeoutInMillisecondsForExtensionMethodImportCompletion), defaultValue: 500);

            // Embedded languages:

            public static PerLanguageOption2<bool> ProvideRegexCompletions =
                new(
                    "RegularExpressionsOptions",
                    nameof(ProvideRegexCompletions),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideRegexCompletions"));

            public static readonly PerLanguageOption2<bool> ProvideDateAndTimeCompletions =
                new(
                    "DateAndTime",
                    nameof(ProvideDateAndTimeCompletions),
                    defaultValue: true,
                    storageLocation: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ProvideDateAndTimeCompletions"));
        }
    }
}
