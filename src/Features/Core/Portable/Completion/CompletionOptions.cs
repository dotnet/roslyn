// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class CompletionOptions
    {
        // This is serialized by the Visual Studio-specific LanguageSettingsPersister
        public static readonly PerLanguageOption2<bool> HideAdvancedMembers = new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(HideAdvancedMembers), defaultValue: false);

        // This is serialized by the Visual Studio-specific LanguageSettingsPersister
        public static readonly PerLanguageOption2<bool> TriggerOnTyping = new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(TriggerOnTyping), defaultValue: true);

        public static readonly PerLanguageOption2<bool> TriggerOnTypingLetters2 = new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(TriggerOnTypingLetters), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnTypingLetters"));

#pragma warning disable RS0030 // Do not used banned APIs - Used by TypeScript through IVT, so we cannot change the field type.
        public static readonly PerLanguageOption<bool> TriggerOnTypingLetters = (PerLanguageOption<bool>)TriggerOnTypingLetters2!;
#pragma warning restore RS0030 // Do not used banned APIs

        public static readonly PerLanguageOption2<bool?> TriggerOnDeletion = new PerLanguageOption2<bool?>(nameof(CompletionOptions), nameof(TriggerOnDeletion), defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerOnDeletion"));

        public static readonly PerLanguageOption2<EnterKeyRule> EnterKeyBehavior =
            new PerLanguageOption2<EnterKeyRule>(nameof(CompletionOptions), nameof(EnterKeyBehavior), defaultValue: EnterKeyRule.Default,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.EnterKeyBehavior"));

        public static readonly PerLanguageOption2<SnippetsRule> SnippetsBehavior =
            new PerLanguageOption2<SnippetsRule>(nameof(CompletionOptions), nameof(SnippetsBehavior), defaultValue: SnippetsRule.Default,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.SnippetsBehavior"));

        // Dev15 options
        public static readonly PerLanguageOption2<bool> ShowCompletionItemFilters = new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(ShowCompletionItemFilters), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowCompletionItemFilters"));

        public static readonly PerLanguageOption2<bool> HighlightMatchingPortionsOfCompletionListItems = new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(HighlightMatchingPortionsOfCompletionListItems), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.HighlightMatchingPortionsOfCompletionListItems"));

        public static readonly PerLanguageOption2<bool> BlockForCompletionItems2 = new PerLanguageOption2<bool>(
            nameof(CompletionOptions), nameof(BlockForCompletionItems), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation($"TextEditor.%LANGUAGE%.Specific.BlockForCompletionItems"));

#pragma warning disable RS0030 // Do not used banned APIs - Used by TypeScript through IVT, so we cannot change the field type.
        public static readonly PerLanguageOption<bool> BlockForCompletionItems = (PerLanguageOption<bool>)BlockForCompletionItems2!;
#pragma warning restore RS0030 // Do not used banned APIs

        public static readonly PerLanguageOption2<bool> ShowNameSuggestions =
            new PerLanguageOption2<bool>(nameof(CompletionOptions), nameof(ShowNameSuggestions), defaultValue: true,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowNameSuggestions"));

        //Dev16 options

        // Use tri-value so the default state can be used to turn on the feature with experimentation service.
        public static readonly PerLanguageOption2<bool?> ShowItemsFromUnimportedNamespaces =
            new PerLanguageOption2<bool?>(nameof(CompletionOptions), nameof(ShowItemsFromUnimportedNamespaces), defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.ShowItemsFromUnimportedNamespaces"));

        // Use tri-value so the default state can be used to turn on the feature with experimentation service.
        public static readonly PerLanguageOption2<bool?> TriggerInArgumentLists =
            new PerLanguageOption2<bool?>(nameof(CompletionOptions), nameof(TriggerInArgumentLists), defaultValue: null,
            storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.TriggerInArgumentLists"));

        public static IEnumerable<PerLanguageOption2<bool>> GetDev15CompletionOptions()
        {
            yield return ShowCompletionItemFilters;
            yield return HighlightMatchingPortionsOfCompletionListItems;
        }
    }

    internal static class CompletionControllerOptions
    {
        public static readonly Option2<bool> FilterOutOfScopeLocals = new Option2<bool>(nameof(CompletionControllerOptions), nameof(FilterOutOfScopeLocals), defaultValue: true);
        public static readonly Option2<bool> ShowXmlDocCommentCompletion = new Option2<bool>(nameof(CompletionControllerOptions), nameof(ShowXmlDocCommentCompletion), defaultValue: true);
    }
}
