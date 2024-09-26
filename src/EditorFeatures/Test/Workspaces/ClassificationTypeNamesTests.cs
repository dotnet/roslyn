// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy.Internal;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;

[UseExportProvider]
public class ClassificationTypeNamesTests
{
    public static IEnumerable<object[]> AllPublicClassificationTypeNames
        => typeof(ClassificationTypeNames)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(f => new[] { f.Name, f.GetRawConstantValue() });

    public static IEnumerable<object[]> AllClassificationTypeNames => typeof(ClassificationTypeNames).GetAllFields().Where(
        f => f.GetValue(null) is string value).Select(f => new[] { f.GetValue(null) });

    [Theory]
    [MemberData(nameof(AllPublicClassificationTypeNames))]
    [WorkItem("https://github.com/dotnet/roslyn/issues/25716")]
    public void ClassificationTypeExported(string fieldName, object constantValue)
    {
        var classificationTypeName = Assert.IsType<string>(constantValue);
        var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
        var classificationTypeRegistryService = exportProvider.GetExport<IClassificationTypeRegistryService>().Value;
        var classificationType = classificationTypeRegistryService.GetClassificationType(classificationTypeName);
        Assert.True(classificationType != null, $"{nameof(ClassificationTypeNames)}.{fieldName} has value \"{classificationTypeName}\", but no matching {nameof(ClassificationTypeDefinition)} was exported.");
    }

    [Theory, MemberData(nameof(AllClassificationTypeNames))]
    public void AllTypeNamesContainsAllClassifications(string fieldName)
        => Assert.True(ClassificationTypeNames.AllTypeNames.Contains(fieldName), $"Missing token type {fieldName}.");

    [Fact]
    public void AllTypeNamesContainsNoDuplicates()
        => Assert.Equal(ClassificationTypeNames.AllTypeNames.Distinct(), ClassificationTypeNames.AllTypeNames);
}
