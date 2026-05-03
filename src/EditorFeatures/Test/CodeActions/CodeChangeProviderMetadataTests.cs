// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Composition;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions;

[UseExportProvider]
public sealed class CodeChangeProviderMetadataTests
{
    [Theory]
    [InlineData(typeof(CodeFixProvider))]
    [InlineData(typeof(CodeRefactoringProvider))]
    [InlineData(typeof(IConfigurationFixProvider))]
    public void TestNameMetadataIsPresent(Type providerType)
    {
        var configuration = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var exportedProviders = FindComposedPartsWithExport(configuration, providerType.FullName).ToArray();

        var failureMessage = new StringBuilder();
        failureMessage.AppendLine($"The following {providerType.Name}s exported without Name metadata:");
        var passLength = failureMessage.Length;

        foreach (var (providerPart, providerExport) in exportedProviders)
        {
            if (!TryGetExportName(providerExport, out var _))
            {
                failureMessage.AppendLine(providerPart.Definition.Type.FullName);
            }
        }

        Assert.True(failureMessage.Length == passLength, failureMessage.ToString());
    }

    [Theory]
    [InlineData(typeof(CodeFixProvider), LanguageNames.CSharp)]
    [InlineData(typeof(CodeFixProvider), LanguageNames.VisualBasic)]
    [InlineData(typeof(CodeRefactoringProvider), LanguageNames.CSharp)]
    [InlineData(typeof(CodeRefactoringProvider), LanguageNames.VisualBasic)]
    [InlineData(typeof(IConfigurationFixProvider), LanguageNames.CSharp)]
    [InlineData(typeof(IConfigurationFixProvider), LanguageNames.VisualBasic)]
    public void TestNameMetadataIsUniqueAmongProviders(Type providerType, string language)
    {
        var configuration = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var exportedProviders = FindComposedPartsWithExportForLanguage(configuration, providerType.FullName, language);

        var failureMessage = new StringBuilder();
        failureMessage.AppendLine($"The following {providerType.Name}s are exported for {language} without unique Name metadata:");
        var passLength = failureMessage.Length;

        var exportedProvidersByName = exportedProviders.GroupBy(
            exportedProvider => TryGetExportName(exportedProvider.Export, out var name) ? name : string.Empty);

        foreach (var namedGroup in exportedProvidersByName)
        {
            if (string.IsNullOrEmpty(namedGroup.Key))
            {
                continue;
            }

            if (namedGroup.Count() == 1)
            {
                continue;
            }

            var providerNames = string.Join(", ", namedGroup.Select(exportedProvider => exportedProvider.Part.Definition.Type.FullName));
            failureMessage.AppendLine($"'{namedGroup.Key}' is used by the following providers: {providerNames}");
        }

        Assert.True(failureMessage.Length == passLength, failureMessage.ToString());
    }

    [Theory]
    [InlineData(typeof(CodeFixProvider), typeof(PredefinedCodeFixProviderNames))]
    [InlineData(typeof(CodeRefactoringProvider), typeof(PredefinedCodeRefactoringProviderNames))]
    [InlineData(typeof(IConfigurationFixProvider), typeof(PredefinedConfigurationFixProviderNames))]
    public void TestNameMetadataIsInPredefinedNames(Type providerType, Type predefinedNamesType)
    {
        var predefinedNames = GetPredefinedNamesFromType(predefinedNamesType);

        var configuration = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var exportedProviders = FindComposedPartsWithExport(configuration, providerType.FullName).ToArray();

        var failureMessage = new StringBuilder();
        failureMessage.AppendLine($"The following providers were exported with a Name not present in Predefined{providerType.Name}Names:");
        var passLength = failureMessage.Length;

        foreach (var (providerPart, providerExport) in exportedProviders)
        {
            if (TryGetExportName(providerExport, out var name)
                && !predefinedNames.Contains(name))
            {
                failureMessage.AppendLine(providerPart.Definition.Type.FullName);
            }
        }

        Assert.True(failureMessage.Length == passLength, failureMessage.ToString());
    }

    [Theory]
    [InlineData(typeof(CodeFixProvider), typeof(PredefinedCodeFixProviderNames))]
    [InlineData(typeof(CodeRefactoringProvider), typeof(PredefinedCodeRefactoringProviderNames))]
    [InlineData(typeof(IConfigurationFixProvider), typeof(PredefinedConfigurationFixProviderNames))]
    public void TestAllPredefinedNamesUsedAsNameMetadata(Type providerType, Type predefinedNamesType)
    {
        var predefinedNames = GetPredefinedNamesFromType(predefinedNamesType);

        var configuration = EditorTestCompositions.EditorFeatures.GetCompositionConfiguration();
        var exportedProviders = FindComposedPartsWithExport(configuration, providerType.FullName);
        var providerNames = exportedProviders
            .Select(exportedProvider => TryGetExportName(exportedProvider.Export, out var name) ? name : string.Empty)
            .ToImmutableHashSet();

        var failureMessage = new StringBuilder();
        failureMessage.AppendLine($"The following Predefined{providerType.Name}Names are not used as Name metadata:");
        var passLength = failureMessage.Length;

        var unusedPredefinedNames = predefinedNames.Except(providerNames);
        foreach (var name in unusedPredefinedNames)
        {
            failureMessage.AppendLine(name);
        }

        Assert.True(failureMessage.Length == passLength, failureMessage.ToString());
    }

    private static bool TryGetExportName(ExportDefinition export, [NotNullWhen(returnValue: true)] out string? name)
    {
        if (!export.Metadata.TryGetValue("Name", out var nameObj)
            || nameObj is not string { Length: > 0 })
        {
            name = null;
            return false;
        }

        name = (string)nameObj;
        return true;
    }

    private static ImmutableHashSet<string> GetPredefinedNamesFromType(Type namesType)
    {
        return [.. namesType.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public)
            .Where(field => field.FieldType == typeof(string))
            .Select(field => (string)field.GetValue(null))];
    }

    private static IEnumerable<(ComposedPart Part, ExportDefinition Export)> FindComposedPartsWithExport(
        CompositionConfiguration configuration,
        string exportedTypeName)
    {
        foreach (var part in configuration.Parts)
        {
            var export = part.Definition.ExportedTypes
                .FirstOrDefault(exportedType => exportedTypeName.Equals(exportedType.ContractName));

            if (export != null)
            {
                yield return (part, export);
            }
        }
    }

    private static IEnumerable<(ComposedPart Part, ExportDefinition Export)> FindComposedPartsWithExportForLanguage(
        CompositionConfiguration configuration,
        string exportedTypeName,
        string language)
    {
        return FindComposedPartsWithExport(configuration, exportedTypeName)
            .Where(part => ((string[])part.Export.Metadata["Languages"]!).Contains(language));
    }
}
