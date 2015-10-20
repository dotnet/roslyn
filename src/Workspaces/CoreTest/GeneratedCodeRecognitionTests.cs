// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
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

            foreach (var fileName in fileNames)
            {
                var document = project.AddDocument(fileName, "");
                if (assertGenerated)
                {
                    Assert.True(IsGeneratedCode(document), string.Format("Expected file '{0}' to be interpreted as generated code", fileName));
                }
                else
                {
                    Assert.False(IsGeneratedCode(document), string.Format("Did not expect file '{0}' to be interpreted as generated code", fileName));
                }
            }
        }

        private static Project CreateProject(string language = LanguageNames.CSharp)
        {
            var projectName = "TestProject";
            var projectId = ProjectId.CreateNewId(projectName);
            return new AdhocWorkspace().CurrentSolution
                .AddProject(projectId, projectName, projectName, LanguageNames.CSharp)
                .GetProject(projectId);
        }

        private static bool IsGeneratedCode(Document document)
        {
            return document.Project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>().IsGeneratedCode(document);
        }
    }
}
