// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public class UnifiedSettingsTests
{
    /// <summary>
    /// Dictionary containing the option to unified setting path for VB.
    /// </summary>
    private static readonly ImmutableDictionary<IOption2, string> s_visualBasicUnifiedSettingsStorage = ImmutableDictionary<IOption2, string>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "textEditor.basic.intellisense.triggerCompletionOnTypingLetters").
        Add(CompletionOptionsStorage.TriggerOnDeletion, "textEditor.basic.intellisense.triggerCompletionOnDeletion").
        Add(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, "textEditor.basic.intellisense.highlightMatchingPortionsOfCompletionListItems").
        Add(CompletionViewOptionsStorage.ShowCompletionItemFilters, "textEditor.basic.intellisense.showCompletionItemFilters").
        Add(CompletionOptionsStorage.SnippetsBehavior, "textEditor.basic.intellisense.snippetsBehavior").
        Add(CompletionOptionsStorage.EnterKeyBehavior, "textEditor.basic.intellisense.returnKeyCompletionBehavior").
        Add(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, "textEditor.basic.intellisense.showCompletionItemsFromUnimportedNamespaces").
        Add(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, "textEditor.basic.intellisense.enableArgumentCompletionSnippets");

    /// <summary>
    /// Dictionary containing the option to expected unified settings for VB intellisense page.
    /// </summary>
    private static readonly ImmutableArray<(IOption2, UnifiedSettingBase)> s_visualBasicIntellisenseExpectedSettings =
    [
        (CompletionOptionsStorage.TriggerOnTypingLetters, CreateBooleanOption(
                CompletionOptionsStorage.TriggerOnTypingLetters,
                title: "Show completion list after a character is typed",
                order: 0,
                languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.TriggerOnDeletion, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnDeletion,
            title: "Show completion list after a character is deleted",
            order: 1,
            customDefaultValue: true,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, CreateBooleanOption(
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            "Highlight matching portions of completion list items",
            order: 10,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.ShowCompletionItemFilters, CreateBooleanOption(
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            title: "Show completion item filters",
            order: 20,
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.SnippetsBehavior, CreateEnumOption(
            CompletionOptionsStorage.SnippetsBehavior,
            "Snippets behavior",
            order: 30,
            customDefaultValue: SnippetsRule.IncludeAfterTypingIdentifierQuestionTab,
            enumLabels: ["Never include snippets", "Always include snippets", "Include snippets when ?-Tab is typed after an identifier"],
            enumValues: [SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab],
            customMaps: [new Map { Result = "neverInclude", Match = 1}, new Map { Result = "alwaysInclude", Match = 2}, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 3}, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 0}],
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.EnterKeyBehavior, CreateEnumOption(
            CompletionOptionsStorage.EnterKeyBehavior,
            "Enter key behavior",
            order: 40,
            customDefaultValue: EnterKeyRule.Always,
            enumLabels: ["Never add new line on enter", "Only add new line on enter after end of fully typed word", "Always add new line on enter"],
            enumValues: [EnterKeyRule.Never, EnterKeyRule.AfterFullyTypedWord, EnterKeyRule.Always],
            customMaps: [new Map { Result = "never", Match = 1}, new Map { Result = "always", Match = 2}, new Map { Result = "always", Match = 0}, new Map { Result = "afterFullyTypedWord", Match = 3}],
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, CreateBooleanOption(
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            title: "Show items from unimported namespaces",
            order: 50,
            languageName: LanguageNames.VisualBasic)),
        (CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, CreateBooleanOption(
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            title: "Tab twice to insert arguments",
            customDefaultValue: false,
            order: 60,
            languageName: LanguageNames.VisualBasic)),
    ];

    [Fact]
    public async Task VisualBasicIntellisenseTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        using var pkgDefFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var expectedPrefix = "textEditor.basic.intellisense";
        var properties = jsonDocument!.Root["properties"]!.AsObject()
            .Where(jsonObject => jsonObject.Key.StartsWith(expectedPrefix))
            .SelectAsArray(jsonObject => jsonObject.Value);
        Assert.Equal(s_visualBasicIntellisenseExpectedSettings.Length, properties.Length);
        foreach (var (actualJson, (expectedOption, expectedSetting)) in properties.Zip(s_visualBasicIntellisenseExpectedSettings, (actual, expected) => (actual, expected)))
        {
            // We only have bool and enum option now.
            UnifiedSettingBase actualSettings = expectedOption.Definition.Type.IsEnum
                ? actualJson.Deserialize<UnifiedSettingsEnumOption>()!
                : actualJson.Deserialize<UnifiedSettingsOption<bool>>()!;
            Assert.Equal(expectedSetting, actualSettings);
        }
    }

    #region Helpers
    private static UnifiedSettingsOption<bool> CreateBooleanOption(
        IOption2 onboardedOption,
        string title,
        int order,
        bool? customDefaultValue = null,
        (IOption2 featureFlagOption, bool value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? languageName = null)
    {
        var migration = new Migration { Pass = new Pass { Input = new Input(onboardedOption, languageName) } };
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, like bool?, we use bool in the registration file.
        var underlyingType = Nullable.GetUnderlyingType(type);
        var nonNullableType = underlyingType ?? type;

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<bool>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value)
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"config:{s_visualBasicUnifiedSettingsStorage[enableWhenOptionAndValue.Value.enableWhenOption]}='{enableWhenOptionAndValue.Value.whenValue}'"
            : null;

        var expectedDefault = customDefaultValue ?? onboardedOption.Definition.DefaultValue;
        // If the option default value is null, it means the option is in experiment mode and is hidden by a feature flag.
        // In Unified Settings it is not allowed and should be replaced by using the alternative default.
        // Like:
        //     "textEditor.csharp.intellisense.showNewSnippetExperience": {
        //         "type": "boolean",
        //         "default": false,
        //         "alternateDefault": {
        //             "flagName": "Roslyn.SnippetCompletion",
        //             "default": true
        //         }
        //      }
        // so please specify a non-null default value.
        Assert.NotNull(expectedDefault);

        return new UnifiedSettingsOption<bool>
        {
            Title = title,
            Type = nonNullableType.Name.ToCamelCase(),
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternativeDefault = alternativeDefault,
            Default = (bool)expectedDefault
        };
    }

    private static UnifiedSettingsEnumOption CreateEnumOption<T>(
        IOption2 onboardedOption,
        string title,
        int order,
        string[] enumLabels,
        T? customDefaultValue = default,
        T[]? enumValues = null,
        Map[]? customMaps = null,
        (IOption2 featureFlagOption, T value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? languageName = null) where T : System.Enum
    {
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, we use the original type in the registration file.
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
        Assert.Equal(typeof(T), nonNullableType);

        var expectedEnumValues = enumValues ?? [.. Enum.GetValues(nonNullableType).Cast<T>()];
        var migration = new Migration
        {
            EnumIntegerToString = new EnumIntegerToString
            {
                Input = new Input(onboardedOption, languageName),
                Map = customMaps ?? [.. expectedEnumValues.Select(value => new Map { Result = value.ToString().ToCamelCase(), Match = Convert.ToInt32(value)}) ]
            }
        };

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<string>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value.ToString().ToCamelCase())
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"config:{s_visualBasicUnifiedSettingsStorage[enableWhenOptionAndValue.Value.enableWhenOption]}='{enableWhenOptionAndValue.Value.whenValue}'"
            : null;

        var expectedDefault = customDefaultValue ?? onboardedOption.Definition.DefaultValue;
        Assert.NotNull(expectedDefault);

        return new UnifiedSettingsEnumOption
        {
            Title = title,
            Type = "string",
            Enum = [.. expectedEnumValues.Select(value => value.ToString().ToCamelCase())],
            EnumItemLabels = enumLabels,
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternativeDefault = alternativeDefault,
            Default = expectedDefault.ToString().ToCamelCase(),
        };
    }

    #endregion
}
