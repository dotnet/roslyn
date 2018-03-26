// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
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
            Assert.IsType<string>(constantValue);
            string classificationTypeName = (string)constantValue;
            var exportProvider = TestExportProvider.ExportProviderWithCSharpAndVisualBasic;
            var exports = exportProvider.GetExports<ClassificationTypeDefinition, IClassificationTypeDefinitionMetadata>();
            var export = exports.FirstOrDefault(x => x.Metadata.Name == classificationTypeName);
            Assert.True(export != null, $"{nameof(ClassificationTypeNames)}.{fieldName} has value \"{classificationTypeName}\", but no matching {nameof(ClassificationTypeDefinition)} was exported.");
        }

        public interface IClassificationTypeDefinitionMetadata
        {
            string Name
            {
                get;
            }

            [DefaultValue(null)]
            IEnumerable<string> BaseDefinition
            {
                get;
            }
        }
    }
}
