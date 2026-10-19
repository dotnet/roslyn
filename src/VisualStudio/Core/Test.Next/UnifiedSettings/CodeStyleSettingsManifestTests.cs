// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.CodeStyle;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.CSharp.UnifiedSettings;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.VisualBasic.UnifiedSettings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class CodeStyleSettingsManifestTests
{
    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle.predefinedTypes")]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general.predefinedTypes")]
    public void PredefinedTypeHeadingUsesSentenceCase(string file, string categoryMoniker)
    {
        using var document = Load(file);
        var title = document.RootElement.GetProperty("categories").GetProperty(categoryMoniker).GetProperty("title").GetString()!;
        Assert.Equal("Predefined type preferences:", TestModel.Utilities.EvalResource(title));
    }

    [WpfTheory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle", LanguageNames.CSharp, 13)]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general", LanguageNames.VisualBasic, 9)]
    [UseExportProvider]
    public void EveryConvertedPreferencePreservesItsLegacyHeadingAndGroupOrder(string file, string categoryMoniker, string language, int groupCount)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");
        var (store, serviceProvider) = CreateLegacyDependencies(workspace);
        using AbstractOptionPreviewViewModel legacy = language == LanguageNames.CSharp
            ? new Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting.StyleViewModel(store, serviceProvider)
            : new Microsoft.VisualStudio.LanguageServices.VisualBasic.Options.StyleViewModel(store, serviceProvider);

        using var document = Load(file);
        var categories = document.RootElement.GetProperty("categories").EnumerateObject()
            .Where(category => category.Name[..category.Name.LastIndexOf('.')] == categoryMoniker)
            .OrderBy(category => category.Value.GetProperty("order").GetInt32()).ToArray();
        var properties = document.RootElement.GetProperty("properties");
        var legacyGroups = legacy.CodeStyleItems.GroupBy(item => item.GroupName).ToArray();
        Assert.Equal(groupCount, categories.Length);
        Assert.Equal(legacyGroups.Select(group => group.Key), categories.Select(category => TestModel.Utilities.EvalResource(category.Value.GetProperty("title").GetString()!)));
        for (var index = 0; index < categories.Length; index++)
        {
            var category = categories[index];
            var placedPreferences = properties.EnumerateObject().Where(property => property.Name.EndsWith(".preference", StringComparison.Ordinal))
                .Where(property => Assert.Single(property.Value.GetProperty("placements").EnumerateArray()).GetProperty("category").GetString()!.StartsWith(category.Name + ".", StringComparison.Ordinal))
                .OrderBy(property => document.RootElement.GetProperty("categories").GetProperty(property.Name[..property.Name.LastIndexOf('.')]).GetProperty("order").GetInt32())
                .Select(property => property.Name).ToArray();
            Assert.Equal(legacyGroups[index].Select(item => NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, item.Option.Definition.ConfigName) + ".preference"), placedPreferences);
            Assert.False(category.Value.TryGetProperty("legacyOptionPageId", out _));
            Assert.False(category.Value.TryGetProperty("previewUIFactory", out _));
        }

        foreach (var item in legacy.CodeStyleItems)
        {
            var row = document.RootElement.GetProperty("categories").GetProperty(NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, item.Option.Definition.ConfigName));
            Assert.Equal(item.Description, TestModel.Utilities.EvalResource(row.GetProperty("title").GetString()!));
        }

        foreach (var item in legacy.CodeStyleItems.OfType<BooleanCodeStyleOptionViewModel>())
        {
            var preference = properties.GetProperty(NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, item.Option.Definition.ConfigName) + ".preference");
            var expectedLabels = preference.GetProperty("enum").EnumerateArray()
                .Select(value => item.Preferences.Single(choice => choice.IsChecked == bool.Parse(value.GetString()!)).Name);
            var actualLabels = preference.GetProperty("enumItemLabels").EnumerateArray()
                .Select(label => TestModel.Utilities.EvalResource(label.GetString()!));
            Assert.Equal(expectedLabels, actualLabels);
        }
    }

    [WpfTheory]
    [InlineData("newLines", 3)]
    [InlineData("spacing", 6)]
    [UseExportProvider]
    public void FormattingGroupsPreserveLegacyHeadingsAndChoicesWithOneParentPreview(string page, int groupCount)
    {
        using var workspace = EditorTestWorkspace.CreateCSharp("");
        var (store, serviceProvider) = CreateLegacyDependencies(workspace);
        using AbstractOptionPreviewViewModel legacy = page == "newLines"
            ? new Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting.NewLinesViewModel(store, serviceProvider)
            : new Microsoft.VisualStudio.LanguageServices.CSharp.Options.Formatting.SpacingViewModel(store, serviceProvider);
        var legacyGroups = new List<(string Heading, List<object> Items)>();
        foreach (var item in legacy.Items)
        {
            if (item is HeaderItemViewModel header)
                legacyGroups.Add((header.Header, []));
            else
                legacyGroups[^1].Items.Add(item);
        }

        using var document = Load("csharpFormattingSettings.registration.json");
        var categoryMoniker = page == "general" ? "languages.csharp.advanced.onTypeFormatting" : "languages.csharp.editorConfig." + page;
        var categories = document.RootElement.GetProperty("categories");
        var groups = categories.EnumerateObject().Where(category => category.Name.StartsWith(categoryMoniker + ".", StringComparison.Ordinal))
            .OrderBy(category => category.Value.GetProperty("order").GetInt32()).ToArray();
        var properties = document.RootElement.GetProperty("properties").EnumerateObject()
            .Where(property => property.Name.StartsWith(categoryMoniker + ".", StringComparison.Ordinal)).ToArray();
        Assert.Equal(groupCount, groups.Length);
        Assert.Equal(legacyGroups.Select(group => group.Heading), groups.Select(group => TestModel.Utilities.EvalResource(group.Value.GetProperty("title").GetString()!)));
        Assert.True(categories.GetProperty(categoryMoniker).GetProperty("keepHeaderVisible").GetBoolean());
        Assert.True(categories.GetProperty(categoryMoniker).TryGetProperty("previewUIFactory", out _));
        for (var index = 0; index < groups.Length; index++)
        {
            var group = groups[index];
            Assert.True(group.Value.GetProperty("hideFromNavigation").GetBoolean());
            Assert.False(group.Value.TryGetProperty("legacyOptionPageId", out _));
            Assert.False(group.Value.TryGetProperty("previewUIFactory", out _));
            var placed = properties.Where(property => Assert.Single(property.Value.GetProperty("placements").EnumerateArray()).GetProperty("category").GetString() == group.Name)
                .OrderBy(property => property.Value.GetProperty("order").GetInt32()).ToArray();
            var checkboxes = legacyGroups[index].Items.OfType<AbstractCheckBoxViewModel>().ToArray();
            var radios = legacyGroups[index].Items.OfType<AbstractRadioButtonViewModel>().ToArray();
            Assert.Equal(checkboxes.Length + radios.Length, legacyGroups[index].Items.Count);
            Assert.Equal(checkboxes.Length + (radios.Length > 0 ? 1 : 0), placed.Length);
            Assert.Equal(checkboxes.Select(item => item.Description), placed.Where(property => property.Value.GetProperty("type").GetString() == "boolean")
                .Select(property => TestModel.Utilities.EvalResource(property.Value.GetProperty("title").GetString()!)));
            if (radios.Length > 0)
            {
                var preference = Assert.Single(placed, property => property.Value.GetProperty("type").GetString() == "string");
                Assert.Equal(radios.Select(item => item.Description), preference.Value.GetProperty("enumItemLabels").EnumerateArray()
                    .Select(label => TestModel.Utilities.EvalResource(label.GetString()!)));
            }
        }

        Assert.All(properties, property => Assert.Contains(Assert.Single(property.Value.GetProperty("placements").EnumerateArray()).GetProperty("category").GetString(), groups.Select(group => group.Name)));
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle")]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general")]
    [InlineData("csharpFormattingSettings.registration.json", "languages.csharp.advanced.onTypeFormatting")]
    [InlineData("csharpFormattingSettings.registration.json", "languages.csharp.editorConfig.indentation")]
    [InlineData("csharpFormattingSettings.registration.json", "languages.csharp.editorConfig.newLines")]
    [InlineData("csharpFormattingSettings.registration.json", "languages.csharp.editorConfig.spacing")]
    [InlineData("csharpFormattingSettings.registration.json", "languages.csharp.editorConfig.wrapping")]
    public void NativePagesExplainEditorConfigOverridesAndProvideTheLearnMoreLink(string file, string categoryMoniker)
    {
        using var document = Load(file);
        var category = document.RootElement.GetProperty("categories").GetProperty(categoryMoniker);
        var message = Assert.Single(category.GetProperty("messages").EnumerateArray());
        var text = TestModel.Utilities.EvalResource(message.GetProperty("text").GetString()!);
        var link = "[More info](" + CodeStyleNoticeTextBlock.CodeStylePageHeaderLearnMoreUri.AbsoluteUri + ")";
        Assert.Equal(CodeStyleNoticeTextBlock.CodeStylePageHeader.Replace("More info", link), text);
        Assert.False(message.TryGetProperty("visibleWhen", out _));
        Assert.Equal(CodeStyleNoticeTextBlock.CodeStylePageHeaderLearnMoreUri.AbsoluteUri, category.GetProperty("helpUri").GetString());
        if (category.TryGetProperty("commands", out var commands))
        {
            var help = Assert.Single(commands.EnumerateArray(), command => command.GetProperty("vsct").GetProperty("id").GetInt32() == CodeStyleSettingsCommands.HelpCommandId);
            Assert.Equal(CodeStyleNoticeTextBlock.CodeStylePageHeaderLearnMoreText, TestModel.Utilities.EvalResource(help.GetProperty("vsct").GetProperty("text").GetString()!));
        }
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle", 13)]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general", 9)]
    public void GeneralGroupsAreInlineOnlyAndTheParentRetainsItsPreview(string file, string general, int count)
    {
        using var document = Load(file);
        var categories = document.RootElement.GetProperty("categories");
        var groups = categories.EnumerateObject().Where(category => category.Name[..category.Name.LastIndexOf('.')] == general).ToArray();
        Assert.Equal(count, groups.Length);
        Assert.All(groups, group =>
        {
            Assert.True(group.Value.GetProperty("hideFromNavigation").GetBoolean());
            Assert.False(group.Value.TryGetProperty("previewUIFactory", out _));
        });
        Assert.True(categories.GetProperty(general).GetProperty("keepHeaderVisible").GetBoolean());
        Assert.True(categories.GetProperty(general).TryGetProperty("previewUIFactory", out _));
        Assert.False(categories.GetProperty(general).TryGetProperty("hideFromNavigation", out _));
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", CSharpCodeStyleSettingsPackage.PackageId, 1)]
    [InlineData("visualBasicCodeStyleSettings.registration.json", VisualBasicCodeStyleSettingsPackage.PackageId, 1)]
    [InlineData("csharpFormattingSettings.registration.json", CSharpCodeStyleSettingsPackage.PackageId, 4)]
    public void EveryPreviewCategoryRegistersBothLocalizedPackageCommands(string file, string commandSet, int categoryCount)
    {
        using var document = Load(file);
        var categories = document.RootElement.GetProperty("categories").EnumerateObject()
            .Where(category => category.Value.TryGetProperty("previewUIFactory", out _)).ToArray();
        Assert.Equal(categoryCount, categories.Length);
        foreach (var category in categories)
        {
            var commands = category.Value.GetProperty("commands").EnumerateArray().Select(command => command.GetProperty("vsct")).ToArray();
            Assert.Equal(new[] { CodeStyleSettingsCommands.GenerateEditorConfigCommandId, CodeStyleSettingsCommands.HelpCommandId }, commands.Select(command => command.GetProperty("id").GetInt32()));
            Assert.All(commands, command => Assert.Equal(new Guid(commandSet), new Guid(command.GetProperty("set").GetString()!)));
            Assert.Equal("@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:Generate_dot_editorconfig_file_from_settings;{6cf2e545-6109-4730-8883-cf43d7aec3e1}", commands[0].GetProperty("text").GetString());
            Assert.Equal("@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:Learn_more;{6cf2e545-6109-4730-8883-cf43d7aec3e1}", commands[1].GetProperty("text").GetString());
        }
    }

    [Theory]
    [InlineData(typeof(CSharpCodeStyleSettingsPackage), "CSharpCodeStyleSettingsCommands.vsct")]
    [InlineData(typeof(VisualBasicCodeStyleSettingsPackage), "VisualBasicCodeStyleSettingsCommands.vsct")]
    public void CategoryCommandsHaveColdPackageRoutingWithoutMenuPlacements(Type package, string resource)
    {
        Assert.Single(package.GetCustomAttributes<ProvideMenuResourceAttribute>());
        using var stream = typeof(CodeStyleSettingsManifestTests).Assembly.GetManifestResourceStream(resource)!;
        var table = XDocument.Load(stream);
        var ns = table.Root!.Name.Namespace;
        var symbol = Assert.Single(table.Descendants(ns + "GuidSymbol"));
        Assert.Equal(package.GUID, new Guid((string)symbol.Attribute("value")!));
        Assert.Equal(new[] { "0x0100", "0x0101" }, symbol.Elements(ns + "IDSymbol").Select(id => (string?)id.Attribute("value")));
        Assert.Equal((string?)symbol.Attribute("name"), (string?)table.Root.Element(ns + "Commands")!.Attribute("package"));
        Assert.Equal(2, table.Descendants(ns + "Button").Count());
        Assert.Equal(symbol.Elements(ns + "IDSymbol").Select(id => (string?)id.Attribute("name")), table.Descendants(ns + "Button").Select(button => (string?)button.Attribute("id")));
        Assert.Empty(table.Descendants(ns + "Parent"));
        Assert.All(table.Descendants(ns + "Button"), button =>
        {
            Assert.Equal((string?)symbol.Attribute("name"), (string?)button.Attribute("guid"));
            Assert.Contains("CommandWellOnly", button.Elements(ns + "CommandFlag").Select(flag => flag.Value));
            Assert.Contains("NoButtonCustomize", button.Elements(ns + "CommandFlag").Select(flag => flag.Value));
            Assert.Contains("NoKeyCustomize", button.Elements(ns + "CommandFlag").Select(flag => flag.Value));
        });
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle", 71, "EAE577A7-ACB9-40F5-A7B1-D2878C3C7D6F")]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general", 32, "10C168E1-3470-448A-A1AC-73D6BC070750")]
    public void EveryPreferenceHasDependentSeverityControlAndNativePreview(string file, string categoryMoniker, int count, string legacyPageId)
    {
        using var document = Load(file);
        var language = categoryMoniker.StartsWith("languages.csharp.", StringComparison.Ordinal) ? LanguageNames.CSharp : LanguageNames.VisualBasic;
        var properties = GetGeneralProperties(document, categoryMoniker);
        var preferences = properties.EnumerateObject().Where(property => !property.Name.EndsWith("_severity", StringComparison.Ordinal)).ToArray();
        Assert.Equal(count, preferences.Length);
        Assert.Equal(count * 2, properties.EnumerateObject().Count());
        foreach (var preference in preferences)
        {
            Assert.Equal("string", preference.Value.GetProperty("type").GetString());
            Assert.True(preference.Value.TryGetProperty("default", out _));
            Assert.False(preference.Value.TryGetProperty("migration", out _));
            var severity = properties.GetProperty(preference.Name + "_severity");
            Assert.NotEmpty(preference.Value.GetProperty("enum").EnumerateArray());
            Assert.Equal(preference.Value.GetProperty("enum").GetArrayLength(), preference.Value.GetProperty("enumItemLabels").GetArrayLength());
            Assert.Equal(new[] { "none", "silent", "suggestion", "warning", "error" }, severity.GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
            Assert.Equal(5, severity.GetProperty("enumItemLabels").GetArrayLength());
            Assert.Equal("@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:Severity;{6cf2e545-6109-4730-8883-cf43d7aec3e1}", severity.GetProperty("title").GetString());
            Assert.True(severity.GetProperty("hideFromSearch").GetBoolean());
            Assert.Equal(preference.Value.GetProperty("order").GetInt32() + 1, severity.GetProperty("order").GetInt32());
            var rowMoniker = NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, preference.Name);
            var parentMoniker = rowMoniker + ".preference";
            Assert.Equal("${config:" + parentMoniker + "} != ''", severity.GetProperty("enableWhen").GetString());
            Assert.All(preference.Value.GetProperty("enum").EnumerateArray(), value => Assert.False(string.IsNullOrEmpty(value.GetString())));
            Assert.False(preference.Value.TryGetProperty("enableWhen", out _));
            Assert.Equal(rowMoniker, Assert.Single(preference.Value.GetProperty("placements").EnumerateArray()).GetProperty("category").GetString());
            Assert.False(severity.TryGetProperty("placements", out _));
            Assert.Equal("Preference", TestModel.Utilities.EvalResource(preference.Value.GetProperty("title").GetString()!));
            Assert.False(preference.Value.GetProperty("transientScopesSupported").GetBoolean());
            Assert.False(severity.GetProperty("transientScopesSupported").GetBoolean());
            Assert.False(severity.TryGetProperty("description", out _));
        }
        var category = document.RootElement.GetProperty("categories").GetProperty(categoryMoniker);
        Assert.Equal(new Guid(legacyPageId), new Guid(category.GetProperty("legacyOptionPageId").GetString()!));
        var preview = category.GetProperty("previewUIFactory").GetProperty("gel");
        var factoryId = categoryMoniker.StartsWith("languages.csharp.", StringComparison.Ordinal)
            ? CSharpCodeStyleSettingsPackage.PreviewFactoryId
            : VisualBasicCodeStyleSettingsPackage.PreviewFactoryId;
        Assert.Equal(new Guid(factoryId), new Guid(preview.GetProperty("factoryGuid").GetString()!));
        Assert.Equal(0, preview.GetProperty("elementId").GetInt32());
        Assert.Equal("https://go.microsoft.com/fwlink/?linkid=866541", category.GetProperty("helpUri").GetString());
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json")]
    [InlineData("visualBasicCodeStyleSettings.registration.json")]
    [InlineData("csharpFormattingSettings.registration.json")]
    [InlineData("csharpSettings.registration.json")]
    [InlineData("visualBasicSettings.registration.json")]
    [InlineData("roslynSettings.registration.json")]
    public void NativeSettingsWithDependenciesDoNotDefineExplicitPlacements(string file)
    {
        using var document = Load(file);
        foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject())
        {
            foreach (var condition in new[] { "enableWhen", "visibleWhen" })
            {
                if (property.Value.TryGetProperty(condition, out var expression) &&
                    expression.GetString() is { } text && text.Contains("${config:", StringComparison.Ordinal))
                {
                    // Native registration inherits placement from the dependency and rejects explicit placements.
                    Assert.False(property.Value.TryGetProperty("placements", out _), property.Name);
                }
            }
        }
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle", LanguageNames.CSharp, "CSharp", 71)]
    [InlineData("visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general", LanguageNames.VisualBasic, "VisualBasic", 32)]
    public async Task EveryGeneralPreferenceHasCompoundLegacyRoamingStorage(string file, string regionMoniker, string language, string legacyLanguage, int count)
    {
        using var document = Load(file);
        var preferences = GetGeneralProperties(document, regionMoniker)
            .EnumerateObject().Where(property => !property.Name.EndsWith("_severity", StringComparison.Ordinal)).ToArray();
        var options = new[] { typeof(CodeStyleOptions2), typeof(CSharpCodeStyleOptions), typeof(VisualBasicCodeStyleOptions) }
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(field => field.GetValue(null)).OfType<IOption2>()
            .ToDictionary(option => option.Definition.ConfigName);
        var manager = new Mock<Microsoft.VisualStudio.Settings.ISettingsManager>(MockBehavior.Strict);
        manager.Setup(service => service.GetSubset("*")).Returns(new Mock<Microsoft.VisualStudio.Settings.ISettingsSubset>(MockBehavior.Strict).Object);
        manager.Setup(service => service.SetValueAsync(It.IsAny<string>(), It.IsAny<object?>(), false)).Returns(Task.CompletedTask);
        var persister = new VisualStudioSettingsOptionPersister((_, _) => { },
            ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>>.Empty, manager.Object);
        Assert.Equal(count, preferences.Length);
        Assert.Equal(preferences.Select(preference => preference.Name).OrderBy(name => name), NativeCodeStyleSettingsDefinitions.GetConfigNames(language).OrderBy(name => name));
        foreach (var preference in preferences)
        {
            var option = options[preference.Name];
            Assert.IsAssignableFrom<ICodeStyleOption2>(option.DefaultValue);
            Assert.True(typeof(ICodeStyleOption2).IsAssignableFrom(option.Type));
            var storage = Assert.IsType<VisualStudioOptionStorage.RoamingProfileStorage>(VisualStudioOptionStorage.Storages[preference.Name]);
            var defaultStyle = (ICodeStyleOption2)option.DefaultValue!;
            Assert.Equal(option.Definition.Serializer.Serialize(defaultStyle).Split(':')[0], preference.Value.GetProperty("default").GetString());
            Assert.Equal(CodeStyleSetting.SerializeSeverity(defaultStyle.Notification), GetGeneralProperties(document, regionMoniker).GetProperty(preference.Name + "_severity").GetProperty("default").GetString());
            var expectedKey = language == LanguageNames.VisualBasic && storage.VisualBasicKey is { } visualBasicKey
                ? visualBasicKey : storage.Key.Replace("%LANGUAGE%", legacyLanguage);
            Assert.StartsWith("TextEditor." + legacyLanguage + ".Specific.", expectedKey);
            manager.Invocations.Clear();
            await storage.PersistAsync(persister, new OptionKey2(option, option.IsPerLanguage ? language : null), option.DefaultValue);
            manager.Verify(service => service.SetValueAsync(expectedKey, It.IsAny<object?>(), false), Times.Once);
        }
    }

    [Fact]
    public void VisualBasicIncludesParenthesesAndUnusedParameterPreferencesWithNativeChoices()
    {
        using var document = Load("visualBasicCodeStyleSettings.registration.json");
        var properties = GetGeneralProperties(document, "languages.basic.codeStyle.general");
        foreach (var kind in new[] { "arithmetic_binary", "other_binary", "relational_binary", "other" })
            Assert.Equal(new[] { "always_for_clarity", "never_if_unnecessary" }, properties.GetProperty("dotnet_style_parentheses_in_" + kind + "_operators").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        Assert.Equal(new[] { "non_public", "all" }, properties.GetProperty("dotnet_code_quality_unused_parameters").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        foreach (var kind in new[] { "assignment", "expression_statement" })
            Assert.Equal("unused_local_variable", Assert.Single(properties.GetProperty("visual_basic_style_unused_value_" + kind + "_preference").GetProperty("enum").EnumerateArray()).GetString());
    }

    [Fact]
    public void AllFiveFormattingPagesHaveTheirCompleteNativeInventoryAndTheFourNativePreviews()
    {
        using var document = Load("csharpFormattingSettings.registration.json");
        var pages = new[] { ("general", 5), ("indentation", 6), ("newLines", 15), ("spacing", 24), ("wrapping", 2) };
        var properties = document.RootElement.GetProperty("properties");
        Assert.Equal(52, properties.EnumerateObject().Count());
        Assert.DoesNotContain(properties.EnumerateObject(), property => property.Value.GetProperty("type").GetString() == "external");
        for (var index = 0; index < pages.Length; index++)
        {
            var (page, count) = pages[index];
            var propertyMoniker = page == "general" ? "languages.csharp.advanced.onTypeFormatting" : "languages.csharp.editorConfig." + page;
            var categoryMoniker = page == "general" ? "languages.csharp.advanced.onTypeFormatting" : "languages.csharp.editorConfig." + page;
            var normalCount = properties.EnumerateObject().Count(property => property.Name.StartsWith(propertyMoniker + ".", StringComparison.Ordinal) && property.Value.GetProperty("type").GetString() != "external");
            Assert.Equal(count, normalCount);
            var category = document.RootElement.GetProperty("categories").GetProperty(categoryMoniker);
            Assert.True(Guid.TryParse(category.GetProperty("legacyOptionPageId").GetString(), out _));
            if (index == 0)
            {
                Assert.False(category.TryGetProperty("previewUIFactory", out _));
            }
            else
            {
                var preview = category.GetProperty("previewUIFactory").GetProperty("gel");
                Assert.Equal(new Guid(CSharpCodeStyleSettingsPackage.PreviewFactoryId), new Guid(preview.GetProperty("factoryGuid").GetString()!));
                Assert.Equal(index, preview.GetProperty("elementId").GetInt32());
            }
        }
    }

    [Fact]
    public void FormattingIncludesEveryBraceAndParenthesisFlagAndTypingDependencies()
    {
        using var document = Load("csharpFormattingSettings.registration.json");
        var regions = document.RootElement.GetProperty("properties");
        foreach (var field in new[] { "formatOnSemicolon", "formatOnCloseBrace" })
            Assert.Equal("${config:languages.csharp.advanced.onTypeFormatting.formatOnTyping} == 'true'", regions.GetProperty("languages.csharp.advanced.onTypeFormatting." + field).GetProperty("enableWhen").GetString());
        var newLines = regions.EnumerateObject().Where(property => property.Name.StartsWith("languages.csharp.editorConfig.newLines.csharpNewLineBeforeOpenBrace", StringComparison.Ordinal)).ToArray();
        Assert.Equal(9, newLines.Length);
        var spacing = regions.EnumerateObject().Where(property => property.Name.StartsWith("languages.csharp.editorConfig.spacing.csharpSpaceBetweenParentheses", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, spacing.Length);
        Assert.Equal(new[] { "Ignore", "Remove", "Single" }, regions.GetProperty("languages.csharp.editorConfig.spacing.binaryOperatorSpacing").GetProperty("enum").EnumerateArray().Select(value => value.GetString()));
        foreach (var flag in newLines.Concat(spacing))
        {
            Assert.Equal("boolean", flag.Value.GetProperty("type").GetString());
            var migration = flag.Value.GetProperty("migration").GetProperty("custom");
            var inputs = migration.GetProperty("inputs").EnumerateArray().ToArray();
            Assert.Equal(2, inputs.Length);
            Assert.True(inputs[0].GetProperty("bitmask").GetInt32() > 0);
            Assert.False(inputs[1].TryGetProperty("bitmask", out _));
            var braces = flag.Name.Contains("csharpNewLineBeforeOpenBrace");
            var suffix = flag.Name.Substring(flag.Name.LastIndexOf(braces ? "csharpNewLineBeforeOpenBrace" : "csharpSpaceBetweenParentheses", StringComparison.Ordinal)
                + (braces ? "csharpNewLineBeforeOpenBrace" : "csharpSpaceBetweenParentheses").Length);
            var expectedMask = braces
                ? (int)(NewLineBeforeOpenBracePlacement)Enum.Parse(typeof(NewLineBeforeOpenBracePlacement), suffix)
                : (int)(SpacePlacementWithinParentheses)Enum.Parse(typeof(SpacePlacementWithinParentheses), suffix);
            Assert.Equal(expectedMask, inputs[0].GetProperty("bitmask").GetInt32());
            Assert.Equal(braces, flag.Value.GetProperty("default").GetBoolean());
            Assert.Equal("SettingsManager", inputs[0].GetProperty("store").GetString());
            Assert.Equal("SettingsManager", inputs[1].GetProperty("store").GetString());
            Assert.Equal("TextEditor.CSharp.Specific." + (braces ? "csharp_new_line_before_open_brace" : "csharp_space_between_parentheses"), inputs[0].GetProperty("path").GetString());
            var fallback = braces ? "NewLinesForBracesIn" + suffix : suffix switch
            {
                "Expressions" => "SpaceWithinExpressionParentheses",
                "TypeCasts" => "SpaceWithinCastParentheses",
                "ControlFlowStatements" => "SpaceWithinOtherParentheses",
                _ => throw new InvalidOperationException(suffix),
            };
            Assert.Equal("TextEditor.CSharp.Specific." + fallback, inputs[1].GetProperty("path").GetString());
            Assert.Equal(new[] { 1, 0 }, migration.GetProperty("map").EnumerateArray().Take(2).Select(mapping => mapping.GetProperty("matches")[0].GetInt32()));
            Assert.Equal(new[] { "@missing", "@missing", "@missing" }, migration.GetProperty("map").EnumerateArray().Skip(2).Select(mapping => mapping.GetProperty("matches")[0].GetString()));
        }
    }

    [Fact]
    public void IndependentFormattingScalarsUseExactLegacyLocationsAndActualDefaultsWithoutMaterializingDefaults()
    {
        using var document = Load("csharpFormattingSettings.registration.json");
        var options = new[] { typeof(CSharpFormattingOptions2), typeof(AutoFormattingOptionsStorage), typeof(FormattingOptionsStorage) }
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Select(field => field.GetValue(null)).OfType<IOption2>()
            .ToDictionary(option => CodeStyleSetting.ToUnifiedScalarMoniker(option.Definition.ConfigName));
        var boolCount = 0;
        var enumCount = 0;
        foreach (var property in document.RootElement.GetProperty("properties").EnumerateObject())
        {
            if (property.Value.GetProperty("migration").GetProperty(property.Value.GetProperty("migration").EnumerateObject().First().Name).TryGetProperty("inputs", out var inputs) &&
                inputs.EnumerateArray().Any(input => input.TryGetProperty("bitmask", out _)))
            {
                continue;
            }
            Assert.Matches(@"^([a-z0-9][A-Za-z0-9]+\.)*[a-z0-9][A-Za-z0-9]+$", property.Name);
            var leaf = property.Name.Substring(property.Name.LastIndexOf('.') + 1);
            var configName = leaf switch
            {
                "formatOnTyping" => "csharp_format_on_typing",
                "formatOnSemicolon" => "csharp_format_on_semicolon",
                "formatOnCloseBrace" => "csharp_format_on_close_brace",
                "formatOnReturn" => "csharp_format_on_return",
                "formatOnPaste" => "dotnet_format_on_paste",
                "labelPositioning" => "csharp_indent_labels",
                "binaryOperatorSpacing" => "csharp_space_around_binary_operators",
                _ => leaf,
            };
            var option = options[CodeStyleSetting.ToUnifiedScalarMoniker(configName)];
            var storage = Assert.IsType<VisualStudioOptionStorage.RoamingProfileStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
            Assert.False(property.Value.GetProperty("transientScopesSupported").GetBoolean());
            var migration = Assert.Single(property.Value.GetProperty("migration").EnumerateObject());
            Assert.Equal("oneTimeFromLegacy", migration.Value.GetProperty("mode").GetString());
            var input = migration.Name == "pass" ? migration.Value.GetProperty("input") : Assert.Single(migration.Value.GetProperty("inputs").EnumerateArray());
            Assert.Equal("SettingsManager", input.GetProperty("store").GetString());
            Assert.Equal(storage.Key.Replace("%LANGUAGE%", "CSharp"), input.GetProperty("path").GetString());
            if (option.DefaultValue is bool defaultValue)
            {
                boolCount++;
                Assert.Equal("pass", migration.Name);
                Assert.Equal(defaultValue, property.Value.GetProperty("default").GetBoolean());
            }
            else
            {
                enumCount++;
                Assert.Equal("custom", migration.Name);
                Assert.Equal(option.DefaultValue!.ToString(), property.Value.GetProperty("default").GetString());
                var values = property.Value.GetProperty("enum").EnumerateArray().Select(value => value.GetString()!).ToArray();
                Assert.Equal(values.Length, property.Value.GetProperty("enumItemLabels").GetArrayLength());
                var mappings = migration.Value.GetProperty("map").EnumerateArray().ToArray();
                Assert.Equal(values.Length + 2, mappings.Length);
                // Reset must select the canonical integer before the final unknown-value fallback.
                Assert.Equal("@default", mappings[0].GetProperty("result").GetString());
                Assert.Equal(Convert.ToInt32(option.DefaultValue), Assert.Single(mappings[0].GetProperty("matches").EnumerateArray()).GetInt32());
                Assert.Equal(option.DefaultValue!.ToString(), mappings[1].GetProperty("result").GetString());
                Assert.Equal("@default", mappings[mappings.Length - 1].GetProperty("result").GetString());
                Assert.Equal("@any", Assert.Single(mappings[mappings.Length - 1].GetProperty("matches").EnumerateArray()).GetString());
                foreach (var value in values)
                {
                    var mapping = Assert.Single(mappings, mapping => mapping.GetProperty("result").GetString() == value);
                    var legacy = Assert.Single(mapping.GetProperty("matches").EnumerateArray()).GetInt32();
                    Assert.Equal(Convert.ToInt32(Enum.Parse(option.DefaultValue!.GetType(), value)), legacy);
                }
            }
        }
        Assert.Equal(38, boolCount);
        Assert.Equal(2, enumCount);
    }

    [Theory]
    [InlineData("csharpCodeStyleSettings.registration.json")]
    [InlineData("visualBasicCodeStyleSettings.registration.json")]
    [InlineData("csharpFormattingSettings.registration.json")]
    [InlineData("csharpSettings.registration.json")]
    [InlineData("visualBasicSettings.registration.json")]
    [InlineData("roslynSettings.registration.json")]
    public void RegistrationHasNoDuplicateKeysAndAllResourcesResolveWithoutKeyboardMnemonics(string file)
    {
        using var document = Load(file);
        var requireQualifiedResourceNames = file is "csharpCodeStyleSettings.registration.json" or
            "visualBasicCodeStyleSettings.registration.json" or "csharpFormattingSettings.registration.json";
        Check(document.RootElement, requireQualifiedResourceNames);
        static void Check(JsonElement element, bool requireQualifiedResourceNames)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    Assert.True(names.Add(property.Name), "Duplicate key: " + property.Name);
                    Check(property.Value, requireQualifiedResourceNames);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var value in element.EnumerateArray())
                    Check(value, requireQualifiedResourceNames);
            }
            else if (element.ValueKind == JsonValueKind.String && element.GetString() is { } text &&
                (text.StartsWith("@", StringComparison.Ordinal) || text.StartsWith("#", StringComparison.Ordinal)) &&
                text is not ("@default" or "@any" or "@missing"))
            {
                Assert.Contains(";", text);
                if (text.StartsWith("@", StringComparison.Ordinal))
                {
                    if (requireQualifiedResourceNames)
                        Assert.Contains(":", text[..text.IndexOf(';')]);
                    var owner = text[(text.IndexOf(';') + 1)..];
                    Assert.True(Guid.TryParse(owner, out _) || owner.EndsWith(".dll", StringComparison.Ordinal), "Native resources require a package or assembly owner: " + text);
                }

                var resolved = TestModel.Utilities.EvalResource(text);
                Assert.False(string.IsNullOrEmpty(resolved), "Unresolved resource: " + text);
                Assert.DoesNotContain("_", resolved);
            }
        }
    }

    [Fact]
    public void NamedResourcesUseTheExactNativeTableAndAssemblyOrPackageOwner()
    {
        const string identifier = "@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:Severity;";
        var byAssembly = TestModel.Utilities.EvalResource(identifier + @"..\Microsoft.VisualStudio.LanguageServices.dll");
        var byPackage = TestModel.Utilities.EvalResource(identifier + "{6cf2e545-6109-4730-8883-cf43d7aec3e1}");
        Assert.False(string.IsNullOrEmpty(byAssembly));
        Assert.Equal(byAssembly, byPackage);
        Assert.Throws<InvalidOperationException>(() => TestModel.Utilities.EvalResource(identifier + @"..\Microsoft.VisualStudio.LanguageServices.CSharp.dll"));
        Assert.Throws<InvalidOperationException>(() => TestModel.Utilities.EvalResource(identifier + "{13c3bbb4-f18f-4111-9f54-a0fb010d9194}"));
        Assert.Throws<InvalidOperationException>(() => TestModel.Utilities.EvalResource("@WrongNamespace:Severity;" + @"..\Microsoft.VisualStudio.LanguageServices.dll"));
    }

    [Fact]
    public void SeparatePackagesRegisterEachNewManifestAndTheTwoNativePreviewFactories()
    {
        using var setupStream = typeof(CodeStyleSettingsManifestTests).Assembly.GetManifestResourceStream("CodeStyleSettingsSetup.vsixmanifest")!;
        var setup = XDocument.Load(setupStream);
        var assets = setup.Descendants(setup.Root!.Name.Namespace + "Asset");
        var packages = new[]
        {
            (typeof(CSharpCodeStyleSettingsPackage), @"UnifiedSettings\csharpCodeStyleSettings.registration.json"),
            (typeof(CSharpFormattingSettingsPackage), @"UnifiedSettings\csharpFormattingSettings.registration.json"),
            (typeof(VisualBasicCodeStyleSettingsPackage), @"UnifiedSettings\visualBasicCodeStyleSettings.registration.json"),
        };
        foreach (var (package, path) in packages)
            Assert.Equal(path, Assert.Single(package.GetCustomAttributes<ProvideSettingsManifestAttribute>()).PackageRelativeManifestFile);
        foreach (var (file, factory, package) in new[]
        {
            ("CSharpCodeStyleSettingsPreview.pkgdef", CSharpCodeStyleSettingsPackage.PreviewFactoryId, CSharpCodeStyleSettingsPackage.PackageId),
            ("VisualBasicCodeStyleSettingsPreview.pkgdef", VisualBasicCodeStyleSettingsPackage.PreviewFactoryId, VisualBasicCodeStyleSettingsPackage.PackageId),
        })
        {
            var assembly = typeof(CodeStyleSettingsManifestTests).Assembly;
            var resource = assembly.GetManifestResourceNames().Single(name => name.EndsWith(file, StringComparison.Ordinal));
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var registration = reader.ReadToEnd();
            Assert.Contains(@"[$RootKey$\UIProviders\{" + factory + "}]", registration);
            Assert.Contains("\"Package\"=\"{" + package + "}\"", registration);
            Assert.Single(assets, asset => (string?)asset.Attribute("Type") == "Microsoft.VisualStudio.VsPackage" && (string?)asset.Attribute("Path") == @"UnifiedSettings\" + file);
        }
    }

    private static (OptionStore Store, IServiceProvider ServiceProvider) CreateLegacyDependencies(EditorTestWorkspace workspace)
    {
        var componentModel = new Mock<IComponentModel>(MockBehavior.Loose);
        componentModel.Setup(model => model.GetService<IContentTypeRegistryService>()).Returns(workspace.ExportProvider.GetExportedValue<IContentTypeRegistryService>());
        var serviceProvider = new Mock<IServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(provider => provider.GetService(typeof(SComponentModel))).Returns(componentModel.Object);
        var options = new Mock<IGlobalOptionService>(MockBehavior.Strict);
        options.Setup(options => options.GetOption<object?>(It.IsAny<OptionKey2>())).Returns((OptionKey2 key) => key.Option.DefaultValue);
        return (new OptionStore(options.Object), serviceProvider.Object);
    }

    private static JsonElement GetGeneralProperties(JsonDocument document, string category)
    {
        var language = category.StartsWith("languages.csharp.", StringComparison.Ordinal) ? LanguageNames.CSharp : LanguageNames.VisualBasic;
        var properties = document.RootElement.GetProperty("properties");
        Assert.Equal(NativeCodeStyleSettingsDefinitions.GetConfigNames(language).Count() * 2 + 1, properties.EnumerateObject().Count());
        Assert.DoesNotContain(properties.EnumerateObject(), property => property.Value.GetProperty("type").GetString() == "external");
        var table = properties.GetProperty(category + ".options");
        Assert.Equal("mappedTable", table.GetProperty("type").GetString());
        Assert.Equal(category, table.GetProperty("monikerPrefix").GetString());
        Assert.Equal(1, table.GetProperty("groupingMonikerSegments").GetInt32());
        Assert.False(table.TryGetProperty("visibleWhen", out _));
        Assert.Equal("Description", TestModel.Utilities.EvalResource(table.GetProperty("pivotColumnTitle").GetString()!));
        Assert.False(table.TryGetProperty("default", out _));
        Assert.False(table.TryGetProperty("migration", out _));
        var mapped = new Dictionary<string, JsonElement>();
        foreach (var configName in NativeCodeStyleSettingsDefinitions.GetConfigNames(language))
        {
            var moniker = NativeCodeStyleSettingsDefinitions.GetRowMoniker(language, configName);
            mapped[configName] = properties.GetProperty(moniker + ".preference");
            mapped[configName + "_severity"] = properties.GetProperty(moniker + ".severity");
            var row = document.RootElement.GetProperty("categories").GetProperty(moniker);
            Assert.True(row.GetProperty("hideFromNavigation").GetBoolean());
        }
        using var result = JsonDocument.Parse(JsonSerializer.Serialize(mapped));
        return result.RootElement.Clone();
    }

    private static JsonDocument Load(string file)
    {
        var assembly = typeof(CodeStyleSettingsManifestTests).Assembly;
        var name = assembly.GetManifestResourceNames().Single(name => name.EndsWith(file, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
    }
}
