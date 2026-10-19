// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.LanguageServices.CSharp.LanguageService;
using Microsoft.VisualStudio.LanguageServices.VisualBasic;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class FormattingPageOnboardingTests
{
    [Theory]
    [InlineData("FormattingOptionPage", "general")]
    [InlineData("FormattingIndentationOptionPage", "indentation")]
    [InlineData("FormattingWrappingPage", "wrapping")]
    [InlineData("FormattingNewLinesPage", "newLines")]
    [InlineData("FormattingSpacingPage", "spacing")]
    public void NativeFormattingPagesAreOnboardedWithoutRemovingLegacyRegistration(string pageName, string categoryName)
        => VerifyPage(typeof(CSharpPackage), pageName, "CSharp", @"Code Style\Formatting",
            "csharpFormattingSettings.registration.json", categoryName switch
            {
                "general" => "languages.csharp.advanced.onTypeFormatting",
                _ => "languages.csharp.editorConfig." + categoryName,
            });

    [Theory]
    [InlineData("CSharp", "csharpCodeStyleSettings.registration.json", "languages.csharp.editorConfig.codeStyle")]
    [InlineData("Basic", "visualBasicCodeStyleSettings.registration.json", "languages.basic.codeStyle.general")]
    public void NativeCodeStylePagesAreOnboardedWithoutRemovingLegacyRegistration(string language, string manifestName, string category)
        => VerifyPage(language == "CSharp" ? typeof(CSharpPackage) : typeof(VisualBasicPackage), "CodeStylePage",
            language, "Code Style", manifestName, category);

    [Theory]
    [InlineData("CSharp", 8)]
    [InlineData("Basic", 3)]
    public void AllLegacyPagesAreHiddenWithoutRemovingImportRegistrations(string language, int expectedPageCount)
    {
        var packageType = language == "CSharp" ? typeof(CSharpPackage) : typeof(VisualBasicPackage);
        var registrations = packageType.GetCustomAttributesData()
            .Where(attribute => attribute.AttributeType == typeof(ProvideLanguageEditorOptionPageAttribute))
            .ToArray();
        Assert.Equal(expectedPageCount, registrations.Length);
        Assert.All(registrations, registration =>
        {
            Assert.Equal(language, registration.ConstructorArguments[1].Value);
            var onboarding = Assert.Single(registration.NamedArguments, argument =>
                argument.MemberName == nameof(ProvideLanguageEditorOptionPageAttribute.IsInUnifiedSettings));
            Assert.Equal(true, onboarding.TypedValue.Value);
        });
    }

    [Theory]
    [InlineData("Display_inline_parameter_name_hints_USX", "Display inline parameter name hints")]
    [InlineData("Show_procedure_line_separators_USX", "Show procedure line separators")]
    [InlineData("Highlight_related_keywords_under_cursor_USX", "Highlight related keywords under cursor")]
    [InlineData("Highlight_references_to_symbol_under_cursor_USX", "Highlight references to symbol under cursor")]
    [InlineData("Show_preview_for_rename_tracking_USX", "Show preview for rename tracking")]
    public void AdvancedLabelsDoNotContainLegacyKeyboardMnemonics(string resourceName, string expectedTitle)
        => Assert.Equal(expectedTitle, TestModel.Utilities.EvalResource(
            "@Microsoft.VisualStudio.LanguageServices.ServicesVSResources:" + resourceName + ";{6cf2e545-6109-4730-8883-cf43d7aec3e1}"));

    private static void VerifyPage(Type packageType, string pageName, string language, string legacyCategory, string manifestName, string category)
    {
        var registration = Assert.Single(packageType.GetCustomAttributesData(), attribute =>
            attribute.AttributeType == typeof(ProvideLanguageEditorOptionPageAttribute)
            && attribute.ConstructorArguments[0].Value is Type pageType
            && pageType.Name == pageName);
        Assert.Equal(language, registration.ConstructorArguments[1].Value);
        Assert.Equal(legacyCategory, registration.ConstructorArguments[2].Value);
        var onboarding = Assert.Single(registration.NamedArguments, argument =>
            argument.MemberName == nameof(ProvideLanguageEditorOptionPageAttribute.IsInUnifiedSettings));
        Assert.Equal(true, onboarding.TypedValue.Value);

        using var stream = typeof(FormattingPageOnboardingTests).Assembly.GetManifestResourceStream(
            "Roslyn.VisualStudio.Next.UnitTests." + manifestName);
        Assert.NotNull(stream);
        using var manifest = JsonDocument.Parse(stream!, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        Assert.True(manifest.RootElement.GetProperty("categories").TryGetProperty(category, out _));
        var settings = manifest.RootElement.GetProperty("properties").EnumerateObject()
            .Where(property => property.Name.StartsWith(category + ".", StringComparison.Ordinal)
                || property.Value.TryGetProperty("placements", out var placements)
                    && placements.EnumerateArray().Any(placement => placement.GetProperty("category").GetString() == category))
            .ToArray();
        Assert.NotEmpty(settings);
        Assert.All(settings, setting => Assert.Contains(setting.Value.GetProperty("type").GetString(), new[] { "boolean", "string", "mappedTable" }));
    }
}
