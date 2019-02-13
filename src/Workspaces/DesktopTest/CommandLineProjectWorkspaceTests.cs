// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [UseExportProvider]
    public class CommandLineProjectWorkspaceTests : WorkspaceTestBase
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public async Task TestAddProject_CommandLineProjectAsync()
        {
            CreateFiles(GetSimpleCSharpSolutionFiles());

            string commandLine = @"CSharpClass.cs /out:goo.dll /target:library";
            var baseDirectory = Path.Combine(SolutionDirectory.Path, "CSharpProject");

            using (var ws = new AdhocWorkspace(DesktopMefHostServices.DefaultServices))
            {
                var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, baseDirectory, ws);
                ws.AddProject(info);
                var project = ws.CurrentSolution.GetProject(info.Id);

                Assert.Equal("TestProject", project.Name);
                Assert.Equal("goo", project.AssemblyName);
                Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

                Assert.Equal(1, project.Documents.Count());

                var gooDoc = project.Documents.First(d => d.Name == "CSharpClass.cs");
                Assert.Equal(0, gooDoc.Folders.Count);
                var expectedPath = Path.Combine(baseDirectory, "CSharpClass.cs");
                Assert.Equal(expectedPath, gooDoc.FilePath);

                var text = (await gooDoc.GetTextAsync()).ToString();
                Assert.NotEqual("", text);

                var tree = await gooDoc.GetSyntaxRootAsync();
                Assert.Equal(false, tree.ContainsDiagnostics);

                var compilation = await project.GetCompilationAsync();
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Workspace)]
        public void TestLoadProjectFromCommandLine()
        {
            string commandLine = @"goo.cs subdir\bar.cs /out:goo.dll /target:library";
            var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, @"C:\ProjectDirectory");
            var ws = new AdhocWorkspace();
            ws.AddProject(info);
            var project = ws.CurrentSolution.GetProject(info.Id);

            Assert.Equal("TestProject", project.Name);
            Assert.Equal("goo", project.AssemblyName);
            Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

            Assert.Equal(2, project.Documents.Count());

            var gooDoc = project.Documents.First(d => d.Name == "goo.cs");
            Assert.Equal(0, gooDoc.Folders.Count);
            Assert.Equal(@"C:\ProjectDirectory\goo.cs", gooDoc.FilePath);

            var barDoc = project.Documents.First(d => d.Name == "bar.cs");
            Assert.Equal(1, barDoc.Folders.Count);
            Assert.Equal("subdir", barDoc.Folders[0]);
            Assert.Equal(@"C:\ProjectDirectory\subdir\bar.cs", barDoc.FilePath);
        }
    }
}
