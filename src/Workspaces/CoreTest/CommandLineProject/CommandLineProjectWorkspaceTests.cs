// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Workspace)]
public sealed class CommandLineProjectWorkspaceTests
{
    [Fact]
    public async Task TestAddProject_CommandLineProjectAsync()
    {
        using var tempRoot = new TempRoot();
        var tempDirectory = tempRoot.CreateDirectory();
        var tempFile = tempDirectory.CreateFile("CSharpClass.cs");
        tempFile.WriteAllText("class CSharpClass { }");

        using var ws = new AdhocWorkspace();
        var commandLine = @"CSharpClass.cs /out:goo.dll /target:library";
        var info = CommandLineProject.CreateProjectInfo("TestProject", LanguageNames.CSharp, commandLine, tempDirectory.Path, ws);
        ws.AddProject(info);
        var project = ws.CurrentSolution.GetProject(info.Id);

        Assert.Equal("TestProject", project.Name);
        Assert.Equal("goo", project.AssemblyName);
        Assert.Equal(OutputKind.DynamicallyLinkedLibrary, project.CompilationOptions.OutputKind);

        Assert.Equal(1, project.Documents.Count());

        var gooDoc = project.Documents.First(d => d.Name == "CSharpClass.cs");
        Assert.Equal(0, gooDoc.Folders.Count);
        Assert.Equal(tempFile.Path, gooDoc.FilePath);

        var text = (await gooDoc.GetTextAsync()).ToString();
        Assert.Equal(tempFile.ReadAllText(), text);

        var tree = await gooDoc.GetSyntaxRootAsync();
        Assert.False(tree.ContainsDiagnostics);

        var compilation = await project.GetCompilationAsync();
    }

    [Fact]
    public void TestLoadProjectFromCommandLine()
    {
        var commandLine = @"goo.cs subdir\bar.cs /out:goo.dll /target:library";
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
