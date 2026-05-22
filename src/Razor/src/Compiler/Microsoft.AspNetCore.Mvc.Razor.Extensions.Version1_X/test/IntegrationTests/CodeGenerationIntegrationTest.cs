// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X.IntegrationTests;

public class CodeGenerationIntegrationTest : IntegrationTestBase
{
    private static readonly CSharpCompilation DefaultBaseCompilation = MvcShim.BaseCompilation.WithAssemblyName("AppCode");

    public CodeGenerationIntegrationTest()
        : base(layer: TestProject.Layer.Compiler, projectDirectoryHint: "Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X")
    {
        Configuration = new(RazorLanguageVersion.Version_1_1, "MVC-1.1", Extensions: []);
    }

    protected override CSharpCompilation BaseCompilation => DefaultBaseCompilation;

    protected override RazorConfiguration Configuration { get; }

    protected override CSharpParseOptions CSharpParseOptions => base.CSharpParseOptions.WithLanguageVersion(LanguageVersion.CSharp10);

    [Fact]
    public void IncompleteDirectives()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
public class MyService<TModel>
{
    public string Html { get; set; }
}");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        // We expect this test to generate a bunch of errors.
        Assert.True(compiled.CodeDocument.GetCSharpDocument().Diagnostics.Length > 0);
    }

    [Fact]
    public void Inject()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
            public class MyApp
            {
                public string MyProperty { get; set; }
            }
");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithModel()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
            public class MyModel
            {

            }

            public class MyService<TModel>
            {
                public string Html { get; set; }
            }

            public class MyApp
            {
                public string MyProperty { get; set; }
}");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithSemicolon()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
            public class MyModel
            {

            }

            public class MyApp
            {
                public string MyProperty { get; set; }
            }

            public class MyService<TModel>
            {
                public string Html { get; set; }
            }
");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }
}
