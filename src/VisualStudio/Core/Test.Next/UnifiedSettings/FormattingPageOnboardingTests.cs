// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.PreviewPane;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.LanguageServices.VisualBasic;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class FormattingPageOnboardingTests
{
    [Theory]
    [InlineData(LanguageNames.CSharp, nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration), "CodeStylePage")]
    [InlineData(LanguageNames.VisualBasic, nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration), "CodeStylePage")]
    [InlineData(LanguageNames.CSharp, nameof(NamingStyleOptions.NamingPreferences), "NamingStylesOptionPage")]
    [InlineData(LanguageNames.VisualBasic, nameof(NamingStyleOptions.NamingPreferences), "NamingStylesOptionPage")]
    public void DiagnosticPreviewLinksTargetRegisteredLegacyPages(string language, string optionName, string expectedPageName)
    {
        var packageType = language == LanguageNames.CSharp ? typeof(CSharpPackage) : typeof(VisualBasicPackage);
        var registration = Assert.Single(packageType.GetCustomAttributesData(), attribute =>
            attribute.AttributeType == typeof(ProvideLanguageEditorOptionPageAttribute)
            && ((Type)attribute.ConstructorArguments[0].Value!).Name == expectedPageName);
        var pageType = (Type)registration.ConstructorArguments[0].Value!;
        Assert.Equal(pageType.GUID, PreviewPaneService.GetOptionPageGuidForOptionName(optionName, language));
    }

    [Theory]
    [InlineData("CSharp", 6)]
    [InlineData("Basic", 2)]
    public void RemainingPagesAreLegacy(string language, int expectedPageCount)
    {
        var packageType = language == "CSharp" ? typeof(CSharpPackage) : typeof(VisualBasicPackage);
        var registrations = packageType.GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType == typeof(ProvideLanguageEditorOptionPageAttribute))
            .ToArray();
        Assert.Equal(expectedPageCount, registrations.Length);
        Assert.All(registrations, registration =>
        {
            Assert.Equal(language, registration.ConstructorArguments[1].Value);
            var pageType = (Type)registration.ConstructorArguments[0].Value!;
            var converted = registration.NamedArguments.Any(argument =>
                argument.MemberName == nameof(ProvideLanguageEditorOptionPageAttribute.IsInUnifiedSettings) && Equals(argument.TypedValue.Value, true));
            Assert.False(converted);
            Assert.Contains(pageType.Name, new[] { "CodeStylePage", "NamingStylesOptionPage", "FormattingIndentationOptionPage", "FormattingWrappingPage", "FormattingNewLinesPage", "FormattingSpacingPage" });
            Assert.Equal(pageType.Name is "CodeStylePage" or "NamingStylesOptionPage" ? "Code Style" : @"Code Style\Formatting", registration.ConstructorArguments[2].Value);
        });

        var manifestName = language == "CSharp" ? "csharpSettings.registration.json" : "visualBasicSettings.registration.json";
        using var document = Load(manifestName);
        var root = document.RootElement;
        foreach (var section in new[] { "categories", "properties" })
        {
            Assert.All(root.GetProperty(section).EnumerateObject(), entry =>
            {
                Assert.DoesNotContain(".editorConfig", entry.Name);
                Assert.DoesNotContain(".codeStyle", entry.Name);
            });
        }

        var manifest = Assert.Single(packageType.GetCustomAttributesData(), attribute => attribute.AttributeType == typeof(ProvideSettingsManifestAttribute));
        Assert.EndsWith(manifestName, (string)Assert.Single(manifest.NamedArguments).TypedValue.Value!);
    }

    [Fact]
    public void OnTypeFormattingRetainsSettingsDefaultsAndLegacyMigrations()
    {
        using var document = Load("csharpSettings.registration.json");
        const string prefix = "languages.csharp.advanced.onTypeFormatting";
        var options = new (string Name, IOption2 Option, string StorageName)[]
        {
            ("formatOnTyping", AutoFormattingOptionsStorage.FormatOnTyping, "Auto Formatting On Typing"),
            ("formatOnSemicolon", AutoFormattingOptionsStorage.FormatOnSemicolon, "Auto Formatting On Semicolon"),
            ("formatOnCloseBrace", AutoFormattingOptionsStorage.FormatOnCloseBrace, "Auto Formatting On Close Brace"),
            ("formatOnReturn", AutoFormattingOptionsStorage.FormatOnReturn, "Auto Formatting On Return"),
            ("formatOnPaste", FormattingOptionsStorage.FormatOnPaste, "FormatOnPaste"),
        };
        var properties = document.RootElement.GetProperty("properties");
        Assert.Equal(options.Length, properties.EnumerateObject().Count(property => property.Name.StartsWith(prefix + ".", StringComparison.Ordinal)));
        foreach (var (name, option, storageName) in options)
        {
            var setting = properties.GetProperty(prefix + "." + name);
            Assert.Equal("boolean", setting.GetProperty("type").GetString());
            Assert.Equal(option.DefaultValue, setting.GetProperty("default").GetBoolean());
            Assert.False(setting.GetProperty("transientScopesSupported").GetBoolean());
            var migration = setting.GetProperty("migration").GetProperty("pass");
            Assert.Equal("full", migration.GetProperty("mode").GetString());
            Assert.Equal("SettingsManager", migration.GetProperty("input").GetProperty("store").GetString());
            Assert.Equal("TextEditor.CSharp.Specific." + storageName, migration.GetProperty("input").GetProperty("path").GetString());
            Assert.IsType<VisualStudioOptionStorage.RoamingProfileStorage>(VisualStudioOptionStorage.Storages[option.Definition.ConfigName]);
            if (name is "formatOnSemicolon" or "formatOnCloseBrace")
                Assert.Equal("${config:" + prefix + ".formatOnTyping} == 'true'", setting.GetProperty("enableWhen").GetString());
        }

        var category = document.RootElement.GetProperty("categories").GetProperty(prefix);
        Assert.False(category.TryGetProperty("legacyOptionPageId", out _));
        Assert.False(category.TryGetProperty("messages", out _));
        Assert.False(category.TryGetProperty("helpUri", out _));
    }

    private static JsonDocument Load(string manifestName)
    {
        using var stream = typeof(FormattingPageOnboardingTests).Assembly.GetManifestResourceStream(
            "Roslyn.VisualStudio.Next.UnitTests." + manifestName);
        Assert.NotNull(stream);
        return JsonDocument.Parse(stream!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
    }
}
