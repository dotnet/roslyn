// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.CSharp.CompleteStatement;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices;
using Roslyn.Utilities;
using Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings.TestModel;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class UnifiedSettingsTests
{
    #region CSharpTest
    /// <summary>
    /// Dictionary containing the option to unified setting path for C#.
    /// </summary>
    private static readonly ImmutableDictionary<IOption2, string> s_csharpUnifiedSettingsStorage = ImmutableDictionary<IOption2, string>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "languages.csharp.intellisense.triggerCompletionOnTypingLetters").
        Add(CompletionOptionsStorage.TriggerOnDeletion, "languages.csharp.intellisense.triggerCompletionOnDeletion").
        Add(CompletionOptionsStorage.TriggerInArgumentLists, "languages.csharp.intellisense.triggerCompletionInArgumentLists").
        Add(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, "languages.csharp.intellisense.highlightMatchingPortionsOfCompletionListItems").
        Add(CompletionViewOptionsStorage.ShowCompletionItemFilters, "languages.csharp.intellisense.showCompletionItemFilters").
        Add(CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, "languages.csharp.intellisense.completeStatementOnSemicolon").
        Add(CompletionOptionsStorage.SnippetsBehavior, "languages.csharp.intellisense.snippetsBehavior").
        Add(CompletionOptionsStorage.EnterKeyBehavior, "languages.csharp.intellisense.returnKeyCompletionBehavior").
        Add(CompletionOptionsStorage.ShowNameSuggestions, "languages.csharp.intellisense.showNameCompletionSuggestions").
        Add(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, "languages.csharp.intellisense.showCompletionItemsFromUnimportedNamespaces").
        Add(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, "languages.csharp.intellisense.enableArgumentCompletionSnippets").
        Add(CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, "languages.csharp.intellisense.showNewSnippetExperience");

    /// <summary>
    /// Array containing the option to expected unified settings for C# intellisense page.
    /// </summary>
    private static readonly ImmutableArray<(IOption2, UnifiedSettingBase)> s_csharpIntellisenseExpectedSettings =
    [
        (CompletionOptionsStorage.TriggerOnTypingLetters, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnTypingLetters,
            title: "Show completion list after a character is typed",
            order: 0,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.TriggerOnDeletion, CreateBooleanOption(
            CompletionOptionsStorage.TriggerOnDeletion,
            title: "Show completion list after a character is deleted",
            order: 1,
            customDefaultValue: false,
            enableWhenOptionAndValue: (enableWhenOption: CompletionOptionsStorage.TriggerOnTypingLetters, whenValue: true),
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.TriggerInArgumentLists, CreateBooleanOption(
            CompletionOptionsStorage.TriggerInArgumentLists,
            title: "Automatically show completion list in argument lists",
            order: 10,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, CreateBooleanOption(
            CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems,
            "Highlight matching portions of completion list items",
            order: 20,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.ShowCompletionItemFilters, CreateBooleanOption(
            CompletionViewOptionsStorage.ShowCompletionItemFilters,
            title: "Show completion item filters",
            order: 30,
            languageName: LanguageNames.CSharp)),
        (CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon, CreateBooleanOption(
            CompleteStatementOptionsStorage.AutomaticallyCompleteStatementOnSemicolon,
            title: "Automatically complete statement on semicolon",
            order: 40,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.SnippetsBehavior, CreateEnumOption(
            CompletionOptionsStorage.SnippetsBehavior,
            "Snippets behavior",
            order: 50,
            customDefaultValue: SnippetsRule.AlwaysInclude,
            enumLabels: ["Never include snippets", "Always include snippets", "Include snippets when ?-Tab is typed after an identifier"],
            enumValues: [SnippetsRule.NeverInclude, SnippetsRule.AlwaysInclude, SnippetsRule.IncludeAfterTypingIdentifierQuestionTab],
            customMaps: [new Map { Result = "neverInclude", Match = 1 }, new Map { Result = "alwaysInclude", Match = 2 }, new Map { Result = "alwaysInclude", Match = 0 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 3 }],
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.EnterKeyBehavior, CreateEnumOption(
            CompletionOptionsStorage.EnterKeyBehavior,
            "Enter key behavior",
            order: 60,
            customDefaultValue: EnterKeyRule.Never,
            enumLabels: ["Never add new line on enter", "Only add new line on enter after end of fully typed word", "Always add new line on enter"],
            enumValues: [EnterKeyRule.Never, EnterKeyRule.AfterFullyTypedWord, EnterKeyRule.Always],
            customMaps: [new Map { Result = "never", Match = 1 }, new Map { Result = "never", Match = 0 }, new Map { Result = "always", Match = 2}, new Map { Result = "afterFullyTypedWord", Match = 3 }],
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.ShowNameSuggestions, CreateBooleanOption(
            CompletionOptionsStorage.ShowNameSuggestions,
            title: "Show name suggestions",
            order: 70,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, CreateBooleanOption(
            CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces,
            title: "Show items from unimported namespaces",
            order: 80,
            languageName: LanguageNames.CSharp)),
        (CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, CreateBooleanOption(
            CompletionViewOptionsStorage.EnableArgumentCompletionSnippets,
            title: "Tab twice to insert arguments",
            customDefaultValue: false,
            order: 90,
            languageName: LanguageNames.CSharp)),
        (CompletionOptionsStorage.ShowNewSnippetExperienceUserOption, CreateBooleanOption(
            CompletionOptionsStorage.ShowNewSnippetExperienceUserOption,
            title: "Show new snippet experience",
            customDefaultValue: false,
            order: 100,
            languageName: LanguageNames.CSharp,
            featureFlagAndExperimentValue: (CompletionOptionsStorage.ShowNewSnippetExperienceFeatureFlag, true))),
    ];

    [Fact]
    public async Task CSharpCategoriesTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.csharpSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var categories = jsonDocument!.Root["categories"]!.AsObject();
        var propertyToCategory = categories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Deserialize<Category>());
        Assert.Equal(2, propertyToCategory.Count);
        Assert.Equal("C#", propertyToCategory["languages.csharp"]!.Title);
        Assert.Equal("IntelliSense", propertyToCategory["languages.csharp.intellisense"]!.Title);
        await VerifyTagAsync(jsonDocument.ToString(), "Roslyn.VisualStudio.Next.UnitTests.csharpPackageRegistration.pkgdef");
    }

    [Fact]
    public async Task CSharpIntellisenseTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.csharpSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        foreach (var (option, _) in s_csharpIntellisenseExpectedSettings)
        {
            Assert.True(s_csharpUnifiedSettingsStorage.ContainsKey(option));
        }

        VerifyProperties(jsonDocument!, "languages.csharp.intellisense", s_csharpIntellisenseExpectedSettings);
        await VerifyTagAsync(jsonDocument!.ToString(), "Roslyn.VisualStudio.Next.UnitTests.csharpPackageRegistration.pkgdef");
    }

    #endregion

    #region VisualBasicTest
    /// <summary>
    /// Dictionary containing the option to unified setting path for VB.
    /// </summary>
    private static readonly ImmutableDictionary<IOption2, string> s_visualBasicUnifiedSettingsStorage = ImmutableDictionary<IOption2, string>.Empty.
        Add(CompletionOptionsStorage.TriggerOnTypingLetters, "languages.basic.intellisense.triggerCompletionOnTypingLetters").
        Add(CompletionOptionsStorage.TriggerOnDeletion, "languages.basic.intellisense.triggerCompletionOnDeletion").
        Add(CompletionViewOptionsStorage.HighlightMatchingPortionsOfCompletionListItems, "languages.basic.intellisense.highlightMatchingPortionsOfCompletionListItems").
        Add(CompletionViewOptionsStorage.ShowCompletionItemFilters, "languages.basic.intellisense.showCompletionItemFilters").
        Add(CompletionOptionsStorage.SnippetsBehavior, "languages.basic.intellisense.snippetsBehavior").
        Add(CompletionOptionsStorage.EnterKeyBehavior, "languages.basic.intellisense.returnKeyCompletionBehavior").
        Add(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, "languages.basic.intellisense.showCompletionItemsFromUnimportedNamespaces").
        Add(CompletionViewOptionsStorage.EnableArgumentCompletionSnippets, "languages.basic.intellisense.enableArgumentCompletionSnippets");

    /// <summary>
    /// Array containing the option to expected unified settings for VB intellisense page.
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
            customMaps: [new Map { Result = "neverInclude", Match = 1 }, new Map { Result = "alwaysInclude", Match = 2 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 3 }, new Map { Result = "includeAfterTypingIdentifierQuestionTab", Match = 0 }],
            languageName: LanguageNames.VisualBasic)),
        (CompletionOptionsStorage.EnterKeyBehavior, CreateEnumOption(
            CompletionOptionsStorage.EnterKeyBehavior,
            "Enter key behavior",
            order: 40,
            customDefaultValue: EnterKeyRule.Always,
            enumLabels: ["Never add new line on enter", "Only add new line on enter after end of fully typed word", "Always add new line on enter"],
            enumValues: [EnterKeyRule.Never, EnterKeyRule.AfterFullyTypedWord, EnterKeyRule.Always],
            customMaps: [new Map { Result = "never", Match = 1}, new Map { Result = "always", Match = 2}, new Map { Result = "always", Match = 0}, new Map { Result = "afterFullyTypedWord", Match = 3 }],
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
    public async Task VisualBasicCategoriesTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var categories = jsonDocument!.Root["categories"]!.AsObject();
        var propertyToCategory = categories.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Deserialize<Category>());
        Assert.Equal(2, propertyToCategory.Count);
        Assert.Equal("Visual Basic", propertyToCategory["languages.basic"]!.Title);
        Assert.Equal("IntelliSense", propertyToCategory["languages.basic.intellisense"]!.Title);
        await VerifyTagAsync(jsonDocument.ToString(), "Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
    }

    [Fact]
    public async Task VisualBasicIntellisenseTest()
    {
        using var registrationFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream("Roslyn.VisualStudio.Next.UnitTests.visualBasicSettings.registration.json");
        var jsonDocument = await JsonNode.ParseAsync(registrationFileStream!, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        foreach (var (option, _) in s_visualBasicIntellisenseExpectedSettings)
        {
            Assert.True(s_visualBasicUnifiedSettingsStorage.ContainsKey(option));
        }

        VerifyProperties(jsonDocument!, "languages.basic.intellisense", s_visualBasicIntellisenseExpectedSettings);
        await VerifyTagAsync(jsonDocument!.ToString(), "Roslyn.VisualStudio.Next.UnitTests.visualBasicPackageRegistration.pkgdef");
    }

    private static void VerifyProperties(JsonNode jsonDocument, string prefix, ImmutableArray<(IOption2, UnifiedSettingBase)> expectedOptionToSettings)
    {
        var properties = jsonDocument!.Root["properties"]!.AsObject().SelectAsArray(
            predicate: jsonObject => jsonObject.Key.StartsWith(prefix),
            selector: jsonObject => jsonObject.Value);
        Assert.Equal(expectedOptionToSettings.Length, properties.Length);
        foreach (var (actualJson, (expectedOption, expectedSetting)) in properties.Zip(expectedOptionToSettings, (actual, expected) => (actual, expected)))
        {
            // We only have bool and enum option now.
            UnifiedSettingBase actualSetting = expectedOption.Definition.Type.IsEnum
                ? actualJson.Deserialize<UnifiedSettingsEnumOption>()!
                : actualJson.Deserialize<UnifiedSettingsOption<bool>>()!;
            Assert.Equal(expectedSetting, actualSetting);
        }
    }

    #endregion

    #region Helpers

    private static async Task VerifyTagAsync(string registrationFile, string pkgdefFileName)
    {
        using var pkgDefFileStream = typeof(UnifiedSettingsTests).GetTypeInfo().Assembly.GetManifestResourceStream(pkgdefFileName);
        using var streamReader = new StreamReader(pkgDefFileStream);
        var pkgdefFile = await streamReader.ReadToEndAsync();

        var fileBytes = Encoding.ASCII.GetBytes(registrationFile);
        var expectedTags = BitConverter.ToInt64([.. XxHash128.Hash(fileBytes).Take(8)], 0).ToString("X16");
        var regex = new Regex("""
                              "CacheTag"=qword:\w{16}
                              """);
        var match = regex.Match(pkgdefFile, 0).Value;
        var actualTag = match[^16..];
        // Please change the CacheTag value in pkddefFile when you modify the registration file.
        Assert.Equal(expectedTags, actualTag);
    }

    private static UnifiedSettingsOption<bool> CreateBooleanOption(
        IOption2 onboardedOption,
        string title,
        int order,
        string languageName,
        bool? customDefaultValue = null,
        (IOption2 featureFlagOption, bool value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null,
        string? message = null)
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
            ? $"${{config:{GetUnifiedSettingsOptionValue(enableWhenOptionAndValue.Value.enableWhenOption, languageName)}}}=='{enableWhenOptionAndValue.Value.whenValue.ToString().ToCamelCase()}'"
            : null;

        var expectedDefault = customDefaultValue ?? onboardedOption.Definition.DefaultValue;
        // If the option default value is null, it means the option is in experiment mode and is hidden by a feature flag.
        // In Unified Settings it is not allowed and should be replaced by using the alternative default.
        // Like:
        //     "languages.csharp.intellisense.showNewSnippetExperience": {
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
            AlternateDefault = alternativeDefault,
            Default = (bool)expectedDefault,
            Messages = message is null ? null : [new Message { Text = message }],
        };
    }

    private static UnifiedSettingsEnumOption CreateEnumOption<T>(
        IOption2 onboardedOption,
        string title,
        int order,
        string[] enumLabels,
        string languageName,
        T? customDefaultValue = default,
        T[]? enumValues = null,
        Map[]? customMaps = null,
        (IOption2 featureFlagOption, T value)? featureFlagAndExperimentValue = null,
        (IOption2 enableWhenOption, object whenValue)? enableWhenOptionAndValue = null) where T : Enum
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
                Map = customMaps ?? [.. expectedEnumValues.Select(value => new Map { Result = value.ToString().ToCamelCase(), Match = Convert.ToInt32(value) })]
            }
        };

        var alternativeDefault = featureFlagAndExperimentValue is not null
            ? new AlternativeDefault<string>(featureFlagAndExperimentValue.Value.featureFlagOption, featureFlagAndExperimentValue.Value.value.ToString().ToCamelCase())
            : null;

        var enableWhen = enableWhenOptionAndValue is not null
            ? $"${{config:{GetUnifiedSettingsOptionValue(enableWhenOptionAndValue.Value.enableWhenOption, languageName)}}}=='{enableWhenOptionAndValue.Value.whenValue.ToString().ToCamelCase()}'"
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
            AlternateDefault = alternativeDefault,
            Default = expectedDefault.ToString().ToCamelCase(),
        };
    }

    private static string GetUnifiedSettingsOptionValue(IOption2 option, string languageName)
    {
        return languageName switch
        {
            LanguageNames.CSharp => s_csharpUnifiedSettingsStorage[option],
            LanguageNames.VisualBasic => s_visualBasicUnifiedSettingsStorage[option],
            _ => throw ExceptionUtilities.UnexpectedValue(languageName)
        };
    }

    #endregion
}
