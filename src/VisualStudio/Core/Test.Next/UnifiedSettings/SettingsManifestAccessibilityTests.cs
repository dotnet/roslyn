// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.CSharp;
using Microsoft.VisualStudio.LanguageServices.VisualBasic;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class SettingsManifestAccessibilityTests
{
    private static readonly (Type ResourceType, string Key, string Label)[] s_labels =
    [
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Automatically_format_block_on_close_brace), "Automatically format block on }"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Automatically_format_on_paste), "Automatically format on paste"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Automatically_format_statement_on_semicolon), "Automatically format statement on ;"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Enter_outlining_mode_when_files_open), "Enter outlining mode when files open"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Generate_XML_documentation_comments_for), "Generate XML documentation comments for ///"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Insert_at_the_start_of_new_lines_when_writing_comments), "Insert * at the start of new lines when writing /* */ comments"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Place_System_directives_first_when_sorting_usings), "Place 'System' directives first when sorting usings"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Suggest_usings_for_types_in_NuGet_packages), "Suggest usings for types in NuGet packages"),
        (typeof(CSharpVSResources), nameof(CSharpVSResources.Split_string_literals_on_enter), "Split string literals on enter"),
        (typeof(ServicesVSResources), nameof(ServicesVSResources.Display_inline_parameter_name_hints), "Display inline parameter name hints"),
        (typeof(ServicesVSResources), nameof(ServicesVSResources.Show_procedure_line_separators), "Show procedure line separators"),
        (typeof(ServicesVSResources), nameof(ServicesVSResources.Highlight_related_keywords_under_cursor), "Highlight related keywords under cursor"),
        (typeof(ServicesVSResources), nameof(ServicesVSResources.Highlight_references_to_symbol_under_cursor), "Highlight references to symbol under cursor"),
        (typeof(ServicesVSResources), nameof(ServicesVSResources.Show_preview_for_rename_tracking), "Show preview for rename tracking"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Automatic_insertion_of_Interface_and_MustOverride_members), "Automatic insertion of Interface and MustOverride members"),
        (typeof(BasicVSResources), nameof(BasicVSResources.A_utomatic_insertion_of_end_constructs), "Automatic insertion of end constructs"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Pretty_listing_reformatting_of_code), "Pretty listing (reformatting) of code"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Enter_outlining_mode_when_files_open), "Enter outlining mode when files open"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Generate_XML_documentation_comments_for), "Generate XML documentation comments for '''"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Navigate_to_Object_Browser_for_symbols_defined_in_metadata), "Navigate to Object Browser for symbols defined in metadata"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Suggest_imports_for_types_in_NuGet_packages), "Suggest imports for types in NuGet packages"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Suggest_imports_for_types_in_reference_assemblies), "Suggest imports for types in reference assemblies"),
        (typeof(BasicVSResources), nameof(BasicVSResources.Place_System_directives_first_when_sorting_imports), "Place 'System' directives first when sorting imports"),
    ];

    [Theory]
    [InlineData("csharpSettings.registration.json", 13)]
    [InlineData("visualBasicSettings.registration.json", 14)]
    [InlineData("roslynSettings.registration.json", 1)]
    public void ShippingManifestsReuseExistingResourceIds(string file, int expectedLabelCount)
    {
        var assembly = typeof(SettingsManifestAccessibilityTests).Assembly;
        using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(name => name.EndsWith(file, StringComparison.Ordinal)))!;
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var cleanLabels = s_labels.ToDictionary(label => label.ResourceType.FullName + ":" + label.Key, label => label.Label, StringComparer.Ordinal);
        var copiedLabels = s_labels.Select(label => label.ResourceType.FullName + ":" + label.Key + "_USX").ToHashSet(StringComparer.Ordinal);
        var affectedReferences = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in GetResourceReferences(document.RootElement))
        {
            Assert.Contains(";", reference);
            var identifier = reference.Substring(1, reference.IndexOf(';') - 1);
            Assert.False(copiedLabels.Contains(identifier), "Manifest uses a duplicate resource ID: " + reference);
            if (cleanLabels.TryGetValue(identifier, out var expectedLabel))
            {
                Assert.DoesNotContain("_USX", identifier);
                var label = TestModel.Utilities.EvalResource(reference);
                Assert.Equal(expectedLabel, label);
                Assert.DoesNotContain("_", label);
                affectedReferences.Add(identifier);
            }
        }

        Assert.Equal(expectedLabelCount, affectedReferences.Count);
    }

    private static IEnumerable<string> GetResourceReferences(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject())
            {
                foreach (var reference in GetResourceReferences(property.Value))
                    yield return reference;
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                foreach (var reference in GetResourceReferences(item))
                    yield return reference;
            }
        }
        else if (value.ValueKind == JsonValueKind.String && value.GetString() is { } text &&
            text.StartsWith("@", StringComparison.Ordinal) && text is not ("@default" or "@any" or "@missing"))
        {
            yield return text;
        }
    }
}
