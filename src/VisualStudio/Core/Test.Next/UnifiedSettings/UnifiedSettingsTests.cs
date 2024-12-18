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
    private static readonly ImmutableDictionary<IOption2, UnifiedSettingBase> s_visualBasicIntellisenseExpectedSettings = ImmutableDictionary<IOption2, UnifiedSettingBase>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, CreateOption(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            title: "Show completion list after a character is typed",
            order: 0,
            defaultValue: true,
            featureFlagAndExperimentValue: null,
            enableWhenOptionAndValue: null,
            languageName: LanguageNames.VisualBasic));

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
        // Assert.Equal(s_optionToExpectedUnifiedSettings.Count, properties.Length);
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
    private static UnifiedSettingsOption<T> CreateOption<T>(
        IOption2 onboardedOption,
        string title,
        int order,
        T? defaultValue = default,
        (IOption2 featureFlagOption, T value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? languageName = null) where T : notnull
    {
        var migration = new Migration { Pass = new Pass { Input = new Input(onboardedOption, languageName) } };
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, like bool?, we use bool in the registration file.
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType is not null)
        {
            Assert.True(featureFlagAndExperimentValue is not null);
        }
        var nonNullableType = underlyingType ?? type;

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<T>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value)
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"config:{s_visualBasicUnifiedSettingsStorage[enableWhenOptionAndValue.Value.enableWhenOption]}='{enableWhenOptionAndValue.Value.whenValue}'"
            : null;

        var expectedDefault = defaultValue ?? onboardedOption.Definition.DefaultValue;
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

        return new UnifiedSettingsOption<T>
        {
            Title = title,
            Type = nonNullableType.Name.ToCamelCase(),
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternativeDefault = alternativeDefault,
            Default = (T)expectedDefault
        };
    }

    private static UnifiedSettingsEnumOption CreateEnumOption<T>(
        IOption2 onboardedOption,
        string title,
        int order,
        T? defaultValue,
        string[] enumLabels,
        T[]? enumValues = null,
        (IOption2 featureFlagOption, T value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? languageName = null) where T : Enum
    {
        var type = onboardedOption.Definition.Type;
        // If the option's type is nullable type, we use the original type in the registration file.
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
        Assert.Equal(typeof(T), nonNullableType);

        var expectedEnumValues = enumValues ?? [.. Enum.GetValues(nonNullableType).Cast<T>()];
        var migration = new Migration
        {
            EnumToInteger = new EnumToInteger
            {
                Input = new Input(onboardedOption, languageName),
                Map = new Map()
                {
                    EnumValueMatches = [.. expectedEnumValues.Select(value => new Map.EnumToValuePair { Result = value.ToString().ToCamelCase(), Match = Convert.ToInt32(value) })]
                }
            }
        };

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<string>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value.ToString().ToCamelCase())
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"config:{UnifiedSettingsTests.s_visualBasicUnifiedSettingsStorage[enableWhenOptionAndValue.Value.enableWhenOption]}='{enableWhenOptionAndValue.Value.whenValue}'"
            : null;

        var expectedDefault = defaultValue ?? onboardedOption.Definition.DefaultValue;
        Assert.NotNull(expectedDefault);

        return new UnifiedSettingsEnumOption
        {
            Title = title,
            Type = "string",
            Enum = [.. expectedEnumValues.Select(value => value.ToString())],
            EnumLabel = enumLabels,
            Order = order,
            EnableWhen = enableWhen,
            Migration = migration,
            AlternativeDefault = alternativeDefault,
            Default = expectedDefault.ToString().ToCamelCase(),
        };
    }

    #endregion
}
