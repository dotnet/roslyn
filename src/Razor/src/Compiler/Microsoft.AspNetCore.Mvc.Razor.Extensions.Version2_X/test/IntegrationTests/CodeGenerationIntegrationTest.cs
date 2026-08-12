// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X.IntegrationTests;

public class CodeGenerationIntegrationTest : IntegrationTestBase
{
    private static readonly CSharpCompilation DefaultBaseCompilation = MvcShim.BaseCompilation.WithAssemblyName("AppCode");

    public CodeGenerationIntegrationTest()
        : base(layer: TestProject.Layer.Compiler, projectDirectoryHint: "Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X")
    {
        Configuration = new(RazorLanguageVersion.Version_2_0, "MVC-2.1", Extensions: []);
    }

    protected override CSharpCompilation BaseCompilation => DefaultBaseCompilation;

    protected override RazorConfiguration Configuration { get; }

    protected override CSharpParseOptions CSharpParseOptions => base.CSharpParseOptions.WithLanguageVersion(LanguageVersion.Latest);

    #region Runtime

    [ConditionalFact(Is.EnglishLocale)]
    public void UsingDirectives()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("The using directive for 'System' appeared previously in this namespace", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public void InvalidNamespaceAtEOF()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
    }

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

        // We expect this test to generate a bunch of errors.
        Assert.NotEmpty(compiled.CodeDocument.GetCSharpDocument().Diagnostics);
    }

    [Fact]
    public void InheritsViewModel()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor;

public class MyBasePageForViews<TModel> : RazorPage
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}
public class MyModel
{

}
");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void InheritsWithViewImports()
    {
        // Arrange
        AddCSharpSyntaxTree(@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

public abstract class MyPageModel<T> : Page
{
    public override Task ExecuteAsync()
    {
        throw new System.NotImplementedException();
    }
}

public class MyModel
{

}");
        AddProjectItemFromText(@"@inherits MyPageModel<TModel>");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void MalformedPageDirective()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1016", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void Basic()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void Sections()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {{typeof(TagHelper).FullName}}
{
    public ModelExpression For { get; set; }
}
""");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void _ViewImports()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
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

    [Fact]
    public void Model()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());

        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void ModelExpressionTagHelper()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
using Microsoft.AspNetCore.Mvc.ViewFeatures;

public class InputTestTagHelper : {{typeof(TagHelper).FullName}}
{
    public ModelExpression For { get; set; }
}
""");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void RazorPages()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
public class DivTagHelper : {{typeof(TagHelper).FullName}}
{

}
""");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void RazorPagesWithRouteTemplate()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void RazorPagesWithoutModel()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
public class DivTagHelper : {{typeof(TagHelper).FullName}}
{

}
""");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void PageWithNamespace()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void ViewWithNamespace()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void ViewComponentTagHelper()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
public class TestViewComponent
{
    public string Invoke(string firstName)
    {
        return firstName;
    }
}

[{{typeof(HtmlTargetElementAttribute).FullName}}]
public class AllTagHelper : {{typeof(TagHelper).FullName}}
{
    public string Bar { get; set; }
}
""");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
    }

    [Fact]
    public void RazorPageWithNoLeadingPageDirective()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ3906", Assert.Single(diagnotics).Id);
    }
    #endregion
}
