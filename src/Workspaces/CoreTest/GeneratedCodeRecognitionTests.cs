// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class GeneratedCodeRecognitionTests
    {
        [Fact]
        public void TestFileNamesNotGenerated()
        {
            TestFileNames(false,
                "",
                "Test",
                "Test.cs",
                "Test.vb",
                "AssemblyInfo.cs",
                "AssemblyInfo.vb",
                ".NETFramework,Version=v4.5.AssemblyAttributes.cs",
                ".NETFramework,Version=v4.5.AssemblyAttributes.vb",
                "Test.notgenerated.cs",
                "Test.notgenerated.vb",
                "Test.generated",
                "Test.designer");
        }

        [Fact]
        public void TestFileNamesGenerated()
        {
            TestFileNames(true,
                "TemporaryGeneratedFile_036C0B5B-1481-4323-8D20-8F5ADCB23D92",
                "TemporaryGeneratedFile_036C0B5B-1481-4323-8D20-8F5ADCB23D92.cs",
                "TemporaryGeneratedFile_036C0B5B-1481-4323-8D20-8F5ADCB23D92.vb",
                "Test.designer.cs",
                "Test.designer.vb",
                "Test.Designer.cs",
                "Test.Designer.vb",
                "Test.generated.cs",
                "Test.generated.vb",
                "Test.g.cs",
                "Test.g.vb",
                "Test.g.i.cs",
                "Test.g.i.vb");
        }

        private static void TestFileNames(bool assertGenerated, params string[] fileNames)
        {
            var project = CreateProject();

            var projectWithUserConfiguredGeneratedCodeTrue = project.AddAnalyzerConfigDocument(".editorconfig",
                SourceText.From(@"
[*.{cs,vb}]
generated_code = true
"), filePath: @"z:\.editorconfig").Project;

            var projectWithUserConfiguredGeneratedCodeFalse = project.AddAnalyzerConfigDocument(".editorconfig",
                SourceText.From(@"
[*.{cs,vb}]
generated_code = false
"), filePath: @"z:\.editorconfig").Project;

            foreach (var fileName in fileNames)
            {
                TestCore(fileName, project, assertGenerated);

                // Verify user configuration always overrides generated code heuristic.
                if (fileName.EndsWith(".cs") || fileName.EndsWith(".vb"))
                {
                    TestCore(fileName, projectWithUserConfiguredGeneratedCodeTrue, assertGenerated: true);
                    TestCore(fileName, projectWithUserConfiguredGeneratedCodeFalse, assertGenerated: false);
                }
            }

            static void TestCore(string fileName, Project project, bool assertGenerated)
            {
                var document = project.AddDocument(fileName, "", filePath: $"z:\\{fileName}");
                if (assertGenerated)
                {
                    Assert.True(document.IsGeneratedCode(CancellationToken.None), string.Format("Expected file '{0}' to be interpreted as generated code", fileName));
                }
                else
                {
                    Assert.False(document.IsGeneratedCode(CancellationToken.None), string.Format("Did not expect file '{0}' to be interpreted as generated code", fileName));
                }
            }
        }

        private static Project CreateProject()
        {
            var projectName = "TestProject";
            var projectId = ProjectId.CreateNewId(projectName);
            return new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                .GetProject(projectId);
        }
    }
}
