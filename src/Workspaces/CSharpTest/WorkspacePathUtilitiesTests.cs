// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

[UseExportProvider]
[Trait(Traits.Feature, Traits.Features.Workspace)]
public sealed class WorkspacePathUtilitiesTests
{
    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            Basic.Reference.Assemblies.Net461.References.All);
    }

    private static AdhocWorkspace CreateWorkspace()
    {
        return new AdhocWorkspace(FeaturesTestCompositions.Features.GetHostServices());
    }
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public void GetExpectedFileNameForSymbol_TopLevelType()
    {
        var code = """
            class MyClass { }
            """;

        var compilation = CreateCompilation(code);
        var symbol = compilation.GetTypeByMetadataName("MyClass");
        Assert.NotNull(symbol);

        var fileName = WorkspacePathUtilities.GetExpectedFileNameForSymbol(symbol);
        Assert.Equal("MyClass", fileName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public void GetExpectedFileNameForSymbol_NestedType()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        var compilation = CreateCompilation(code);
        var outerSymbol = compilation.GetTypeByMetadataName("Outer");
        var innerSymbol = compilation.GetTypeByMetadataName("Outer+Inner");
        Assert.NotNull(outerSymbol);
        Assert.NotNull(innerSymbol);

        var outerFileName = WorkspacePathUtilities.GetExpectedFileNameForSymbol(outerSymbol);
        var innerFileName = WorkspacePathUtilities.GetExpectedFileNameForSymbol(innerSymbol);
        
        Assert.Equal("Outer", outerFileName);
        Assert.Equal("Outer.Inner", innerFileName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public void GetExpectedFileNameForSymbol_DeeplyNestedType()
    {
        var code = """
            class A 
            { 
                class B 
                {
                    class C { }
                }
            }
            """;

        var compilation = CreateCompilation(code);
        var symbol = compilation.GetTypeByMetadataName("A+B+C");
        Assert.NotNull(symbol);

        var fileName = WorkspacePathUtilities.GetExpectedFileNameForSymbol(symbol);
        Assert.Equal("A.B.C", fileName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task TypeNameMatchesDocumentNameWithContainers_SimpleName()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("Inner.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var innerSymbol = compilation.GetTypeByMetadataName("Outer+Inner");
        Assert.NotNull(innerSymbol);

        var matches = WorkspacePathUtilities.TypeNameMatchesDocumentNameWithContainers(document, innerSymbol);
        Assert.True(matches);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task TypeNameMatchesDocumentNameWithContainers_DottedName()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("Outer.Inner.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var innerSymbol = compilation.GetTypeByMetadataName("Outer+Inner");
        Assert.NotNull(innerSymbol);

        var matches = WorkspacePathUtilities.TypeNameMatchesDocumentNameWithContainers(document, innerSymbol);
        Assert.True(matches);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task TypeNameMatchesDocumentNameWithContainers_DoesNotMatch()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("SomeOtherName.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var innerSymbol = compilation.GetTypeByMetadataName("Outer+Inner");
        Assert.NotNull(innerSymbol);

        var matches = WorkspacePathUtilities.TypeNameMatchesDocumentNameWithContainers(document, innerSymbol);
        Assert.False(matches);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task GetUpdatedDocumentNameForSymbolRename_RenameInnerType()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("Outer.Inner.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var innerSymbol = compilation.GetTypeByMetadataName("Outer+Inner");
        Assert.NotNull(innerSymbol);

        var newName = WorkspacePathUtilities.GetUpdatedDocumentNameForSymbolRename(document, innerSymbol, "Foo");
        Assert.Equal("Outer.Foo.cs", newName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task GetUpdatedDocumentNameForSymbolRename_RenameOuterType()
    {
        var code = """
            class Outer 
            { 
                class Inner { }
            }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("Outer.Inner.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var outerSymbol = compilation.GetTypeByMetadataName("Outer");
        Assert.NotNull(outerSymbol);

        var newName = WorkspacePathUtilities.GetUpdatedDocumentNameForSymbolRename(document, outerSymbol, "Bar");
        Assert.Equal("Bar.Inner.cs", newName);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60960")]
    public async Task GetUpdatedDocumentNameForSymbolRename_TopLevelType()
    {
        var code = """
            class MyClass { }
            """;

        using var workspace = CreateWorkspace();
        var project = workspace.CurrentSolution.AddProject("TestProject", "TestAssembly", LanguageNames.CSharp);
        var document = project.AddDocument("MyClass.cs", code);
        var compilation = await document.Project.GetCompilationAsync();
        
        var symbol = compilation.GetTypeByMetadataName("MyClass");
        Assert.NotNull(symbol);

        var newName = WorkspacePathUtilities.GetUpdatedDocumentNameForSymbolRename(document, symbol, "NewClass");
        Assert.Equal("NewClass.cs", newName);
    }
}
