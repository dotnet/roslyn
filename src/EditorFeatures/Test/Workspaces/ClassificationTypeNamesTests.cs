// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    [UseExportProvider]
    public class ClassificationTypeNamesTests
    {
        public static IEnumerable<object[]> AllClassificationTypeNames
        {
            get
            {
                foreach (var field in typeof(ClassificationTypeNames).GetFields(BindingFlags.Static | BindingFlags.Public))
                {
                    yield return new object[] { field.Name, field.GetRawConstantValue() };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllClassificationTypeNames))]
        [WorkItem(25716, "https://github.com/dotnet/roslyn/issues/25716")]
        public void ClassificationTypeExported(string fieldName, object constantValue)
        {
            var classificationTypeName = Assert.IsType<string>(constantValue);
            var exportProvider = EditorTestCompositions.EditorFeatures.ExportProviderFactory.CreateExportProvider();
            var classificationTypeRegistryService = exportProvider.GetExport<IClassificationTypeRegistryService>().Value;
            var classificationType = classificationTypeRegistryService.GetClassificationType(classificationTypeName);
            Assert.True(classificationType != null, $"{nameof(ClassificationTypeNames)}.{fieldName} has value \"{classificationTypeName}\", but no matching {nameof(ClassificationTypeDefinition)} was exported.");
        }
    }
}
