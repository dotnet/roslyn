// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public partial class CustomWorkspaceTests : WorkspaceTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_ProjectInfo()
        {
            var info = ProjectInfo.Create(
                ProjectId.CreateNewId(), 
                version: VersionStamp.Default,
                name: "TestProject",
                assemblyName: "TestProject.dll",
                language: LanguageNames.CSharp);

            using (var ws = new CustomWorkspace())
            {
                var project = ws.AddProject(info);
                Assert.Equal(project, ws.CurrentSolution.Projects.FirstOrDefault());
                Assert.Equal(info.Name, project.Name);
                Assert.Equal(info.Id, project.Id);
                Assert.Equal(info.AssemblyName, project.AssemblyName);
                Assert.Equal(info.Language, project.Language);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_NameAndLanguage()
        {
            using (var ws = new CustomWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                Assert.Same(project, ws.CurrentSolution.Projects.FirstOrDefault());
                Assert.Equal("TestProject", project.Name);
                Assert.Equal(LanguageNames.CSharp, project.Language);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddDocument_DocumentInfo()
        {
            using (var ws = new CustomWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                var info = DocumentInfo.Create(DocumentId.CreateNewId(project.Id), "code.cs");
                var doc = ws.AddDocument(info);

                Assert.Equal(ws.CurrentSolution.GetDocument(info.Id), doc);
                Assert.Equal(info.Name, doc.Name);                
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddDocument_NameAndText()
        {
            using (var ws = new CustomWorkspace())
            {
                var project = ws.AddProject("TestProject", LanguageNames.CSharp);
                var name = "code.cs";
                var source = "class C {}";
                var doc = ws.AddDocument(project.Id, name, SourceText.From(source));

                Assert.Equal(name, doc.Name);
                Assert.Equal(source, doc.GetTextAsync().Result.ToString());
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddSolution_SolutionInfo()
        {
            using (var ws = new CustomWorkspace())
            {
                var pinfo = ProjectInfo.Create(
                        ProjectId.CreateNewId(),
                        version: VersionStamp.Default,
                        name: "TestProject",
                        assemblyName: "TestProject.dll",
                        language: LanguageNames.CSharp);

                var sinfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, projects: new ProjectInfo[] { pinfo });

                var solution = ws.AddSolution(sinfo);

                Assert.Same(ws.CurrentSolution, solution);
                Assert.Equal(solution.Id, sinfo.Id);

                Assert.Equal(sinfo.Projects.Count, solution.ProjectIds.Count);
                var project = solution.Projects.FirstOrDefault();
                Assert.NotNull(project);
                Assert.Equal(pinfo.Name, project.Name);
                Assert.Equal(pinfo.Id, project.Id);
                Assert.Equal(pinfo.AssemblyName, project.AssemblyName);
                Assert.Equal(pinfo.Language, project.Language);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProjects()
        {
            var id1 = ProjectId.CreateNewId();
            var info1 = ProjectInfo.Create(
                id1,
                version: VersionStamp.Default,
                name: "TestProject1",
                assemblyName: "TestProject1.dll",
                language: LanguageNames.CSharp);

            var id2 = ProjectId.CreateNewId();
            var info2 = ProjectInfo.Create(
                id2,
                version: VersionStamp.Default,
                name: "TestProject2",
                assemblyName: "TestProject2.dll",
                language: LanguageNames.VisualBasic,
                projectReferences: new[] { new ProjectReference(id1) });

            using (var ws = new CustomWorkspace())
            {
                ws.AddProjects(new[] { info1, info2 });
                var solution = ws.CurrentSolution;
                Assert.Equal(2, solution.ProjectIds.Count);

                var project1 = solution.GetProject(id1);
                Assert.Equal(info1.Name, project1.Name);
                Assert.Equal(info1.Id, project1.Id);
                Assert.Equal(info1.AssemblyName, project1.AssemblyName);
                Assert.Equal(info1.Language, project1.Language);

                var project2 = solution.GetProject(id2);
                Assert.Equal(info2.Name, project2.Name);
                Assert.Equal(info2.Id, project2.Id);
                Assert.Equal(info2.AssemblyName, project2.AssemblyName);
                Assert.Equal(info2.Language, project2.Language);
                Assert.Equal(1, project2.ProjectReferences.Count());
                Assert.Equal(id1, project2.ProjectReferences.First().ProjectId);
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestAddProject_CommandLineProject()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());

            string commandLine = @"CSharpClass.cs /out:foo.dll /target:library";
            var baseDirectory = Path.Combine(this.SolutionDirectory.Path, "CSharpProject");

            using (var ws = new CustomWorkspace())
            {
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, baseDirectory);
                ws.AddProject(info);
                var project = ws.CurrentSolution.GetProject(info.Id);

                Assert.Equal("TestProject", project.Name);
                Assert.Equal("foo", project.AssemblyName);
                Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

                Assert.Equal(1, project.Documents.Count());

                var fooDoc = project.Documents.First(d => d.Name == "CSharpClass.cs");
                Assert.Equal(0, fooDoc.Folders.Count);
                var expectedPath = Path.Combine(baseDirectory, "CSharpClass.cs");
                Assert.Equal(expectedPath, fooDoc.FilePath);

                var text = fooDoc.GetTextAsync().Result.ToString();
                Assert.NotEqual("", text);

                var tree = fooDoc.GetSyntaxRootAsync().Result;
                Assert.Equal(false, tree.ContainsDiagnostics);

                var compilation = project.GetCompilationAsync().Result;
            }
        }
    }
}