// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.IntegrationTests;

public class CodeGenerationIntegrationTest : IntegrationTestBase
{
    private static readonly CSharpCompilation DefaultBaseCompilation = TestCompilation.Create().WithAssemblyName("AppCode");

    private RazorConfiguration _configuration;

    public CodeGenerationIntegrationTest()
        : base(layer: TestProject.Layer.Compiler, projectDirectoryHint: "Microsoft.AspNetCore.Mvc.Razor.Extensions")
    {
        _configuration = new(RazorLanguageVersion.Latest, "MVC-3.0", Extensions: []);
    }

    protected override CSharpCompilation BaseCompilation { get; set; } = DefaultBaseCompilation;

    protected override RazorConfiguration Configuration => _configuration;

    #region Runtime

    [Fact]
    public void UsingDirectives_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("The using directive for 'System' appeared previously in this namespace", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public void InvalidNamespaceAtEOF_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void IncompleteDirectives_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            public class MyService<TModel>
            {
                public string Html { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        // We expect this test to generate a bunch of errors.
        Assert.NotEmpty(compiled.CodeDocument.GetCSharpDocument().Diagnostics);
    }

    [Fact]
    public void InheritsViewModel_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void InheritsWithViewImports_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            }
            """);
        AddProjectItemFromText(@"@inherits MyPageModel<TModel>");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void AttributeDirectiveWithViewImports_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();
        AddProjectItemFromText("""

            @using System
            @attribute [Serializable]
            """);

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("Duplicate 'Serializable' attribute", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public void MalformedPageDirective_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1016", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void Basic_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void BasicComponent_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile(fileKind: RazorFileKind.Component);

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void Sections_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            using Microsoft.AspNetCore.Mvc.ViewFeatures;

            public class InputTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public ModelExpression For { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void _ViewImports_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void Inject_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            public class MyApp
            {
                public string MyProperty { get; set; }
            }

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithModel_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithSemicolon_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void Model_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());

        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ModelExpressionTagHelper_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            using Microsoft.AspNetCore.Mvc.ViewFeatures;

            public class InputTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public ModelExpression For { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPages_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPagesWithRouteTemplate_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPagesWithoutModel_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void PageWithNamespace_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ViewWithNamespace_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ViewComponentTagHelper_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class TestViewComponent
            {
                public string Invoke(string firstName)
                {
                    return firstName;
                }
            }

            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute]
            public class AllTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public string Bar { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ViewComponentTagHelperOptionalParam_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree($$"""
            using System;

            public class OptionalTestViewComponent
            {
                public string Invoke(bool showSecret = false)
                {
                    return showSecret ? "what a secret" : "not a secret";
                }
            }
            public class OptionalTestWithParamViewComponent
            {
                public string Invoke(string secret, bool showSecret = false)
                {
                    var isSecret = showSecret ? "what a secret" : "not a secret";
                    return isSecret + " : " + secret;
                }
            }
            public class OptionalWithMultipleTypesViewComponent
            {
                public string Invoke(
                    int age = 42,
                    double favoriteDecimal = 12.3,
                    char favoriteLetter = 'b',
                    DateTime? birthDate = null,
                    string anotherOne = null)
                {
                    birthDate = new DateTime(1979, 8, 23);
                    return age + " : " + favoriteDecimal + " : " + favoriteLetter + " : " + birthDate + " : " + anotherOne;
                }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPageWithNoLeadingPageDirective_Runtime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ3906", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void RazorPage_WithCssScope()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("all")]
            public class AllTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public string Bar { get; set; }
            }

            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("form")]
            public class FormTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""
            @page
            @addTagHelper *, AppCode
            @{
                ViewData["Title"] = "Home page";
            }
            <div class="text-center">
                <h1 class="display-4">Welcome</h1>
                <p>Learn about<a href= "https://docs.microsoft.com/aspnet/core" > building Web apps with ASP.NET Core</a>.</p>
            </div>
            <all Bar="Foo"></all>
            <form asp-route="register" method="post">
              <input name="regular input" />
            </form>

            """, cssScope: "TestCssScope");

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void RazorView_WithCssScope()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("all")]
            public class AllTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public string Bar { get; set; }
            }

            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("form")]
            public class FormTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""
            @addTagHelper *, AppCode
            @{
                ViewData["Title"] = "Home page";
            }
            <div class="text-center">
                <h1 class="display-4">Welcome</h1>
                <p>Learn about<a href= "https://docs.microsoft.com/aspnet/core" > building Web apps with ASP.NET Core</a>.</p>
            </div>
            <all Bar="Foo"></all>
            <form asp-route="register" method="post">
              <input name="regular input" />
            </form>

            """, cssScope: "TestCssScope");

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void RazorView_Layout_WithCssScope()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("all")]
            public class AllTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public string Bar { get; set; }
            }
            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute("form")]
            public class FormTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""

            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <title>@ViewData["Title"] - Test layout component</title>
            </head>
            <body>
                <p>This is a body.</p>
            </body>
            </html>

            """, cssScope: "TestCssScope");

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void RazorView_WithNonNullableModel_NullableContextEnabled()
    {
        // Arrange
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        AddCSharpSyntaxTree("""

            namespace TestNamespace;

            public class TestModel
            {
                public string Name { get; set; } = string.Empty;

                public string? Address { get; set; }
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""

            @using TestNamespace
            @model TestModel

            <h1>@Model.Name</h1>

            <h2>@Model.Address</h2>
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void RazorView_WithNullableModel_NullableContextEnabled()
    {
        // Arrange
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        AddCSharpSyntaxTree("""

            namespace TestNamespace;

            public class TestModel
            {
                public string Name { get; set; } = string.Empty;

                public string? Address { get; set; }
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""

            @using TestNamespace
            @model TestModel?

            <h1>@Model?.Name</h1>

            <h2>@Model?.Address</h2>
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void RazorView_WithNullableModel_NullableContextNotEnabled()
    {
        // Arrange
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Disable));

        AddCSharpSyntaxTree("""

            namespace TestNamespace;

            public class TestModel
            {
                public string Name { get; set; } = string.Empty;

                public string Address { get; set; }
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""

            @using TestNamespace
            @model TestModel?

            <h1>@Model?.Name</h1>

            <h2>@Model?.Address</h2>
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        var compiledAssembly = CompileToAssembly(generated, throwOnFailure: false);
        // warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Assert.Contains("CS8669", compiledAssembly.Compilation.GetDiagnostics().Select(d => d.Id));
    }

    [Fact]
    public void RazorView_WithNullableBaseType_NullableContexEnabled()
    {
        // Arrange
        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(NullableContextOptions.Enable));

        AddCSharpSyntaxTree("""

            namespace TestNamespace;
            using Microsoft.AspNetCore.Mvc.Razor;

            public abstract class MyBasePage<TModel> : RazorPage<TModel> where TModel : class? {}

            public class TestModel
            {
                public string Name { get; set; } = string.Empty;

                public string? Address { get; set; }
            }
            """);

        // Act
        // This test case attempts to use all syntaxes that might interact with auto-generated attributes
        var generated = CompileToCSharp("""

            @using TestNamespace
            @inherits MyBasePage<TestModel?>

            <h1>@Model?.Name</h1>

            <h2>@Model?.Address</h2>
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact]
    public void InheritsDirective_RazorPages_Runtime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public abstract class MyBase : global::Microsoft.AspNetCore.Mvc.RazorPages.Page {

            }
            """);

        // Act
        var generated = CompileToCSharp("""
            @page
            @inherits MyBase
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10965")]
    public void InvalidCode_EmptyImplicitExpression_Runtime()
    {
        // Act
        var generated = CompileToCSharp("""
            <html>
                <head>
                    @
                </head>
            </html>
            """, designTime: false);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        AssertSourceMappingsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated, throwOnFailure: false, ignoreRazorDiagnostics: true);
    }

    #endregion

    #region DesignTime

    [Fact]
    public void UsingDirectives_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("The using directive for 'System' appeared previously in this namespace", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public void InvalidNamespaceAtEOF_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void IncompleteDirectives_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            public class MyService<TModel>
            {
                public string Html { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        // We expect this test to generate a bunch of errors.
        Assert.NotEmpty(compiled.CodeDocument.GetCSharpDocument().Diagnostics);
    }

    [Fact]
    public void InheritsViewModel_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InheritsWithViewImports_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            }
            """);

        AddProjectItemFromText(@"@inherits MyPageModel<TModel>");

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void AttributeDirectiveWithViewImports_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();
        AddProjectItemFromText("""

            @using System
            @attribute [Serializable]
            """);

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("Duplicate 'Serializable' attribute", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public void MalformedPageDirective_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1016", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void Basic_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void BasicComponent_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile(fileKind: RazorFileKind.Component);

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void Sections_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            using Microsoft.AspNetCore.Mvc.ViewFeatures;

            public class InputTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public ModelExpression For { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void _ViewImports_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void Inject_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            public class MyApp
            {
                public string MyProperty { get; set; }
            }

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithModel_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void InjectWithSemicolon_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

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

            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void Model_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void MultipleModels_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""

            public class ThisShouldBeGenerated
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ2001", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void ModelExpressionTagHelper_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            using Microsoft.AspNetCore.Mvc.ViewFeatures;

            public class InputTestTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public ModelExpression For { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPages_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPagesWithRouteTemplate_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPagesWithoutModel_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void PageWithNamespace_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void ViewWithNamespace_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void ViewComponentTagHelper_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class TestViewComponent
            {
                public string Invoke(string firstName)
                {
                    return firstName;
                }
            }

            [Microsoft.AspNetCore.Razor.TagHelpers.HtmlTargetElementAttribute]
            public class AllTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {
                public string Bar { get; set; }
            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPageWithNoLeadingPageDirective_DesignTime()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem, designTime: true);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(compiled.CodeDocument));
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ3906", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void InheritsDirective_RazorPages_DesignTime()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public abstract class MyBase : global::Microsoft.AspNetCore.Mvc.RazorPages.Page {

            }
            """);

        // Act
        var generated = CompileToCSharp("""
            @page
            @inherits MyBase
            """, designTime: true);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/10965")]
    public void InvalidCode_EmptyImplicitExpression_DesignTime()
    {
        // Act
        var generated = CompileToCSharp("""
            <html>
                <head>
                    @
                </head>
            </html>
            """, designTime: true);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        CompileToAssembly(generated, throwOnFailure: false, ignoreRazorDiagnostics: true);
    }

    #endregion

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/razor/issues/7286")]
    public void RazorPage_NullableModel(bool nullableModel, bool nullableContextEnabled, bool designTime,
        [CombinatorialValues("8.0", "9.0", "Latest")] string razorLangVersion)
    {
        // Arrange

        // Construct "key" for baselines.
        var testName = "RazorPage_With" +
            (nullableModel ? "" : "Non") +
            "NullableModel_Lang" +
            (razorLangVersion == "8.0" ? "Old" : "New") +
            "_" +
            (designTime ? "DesignTime" : "Runtime");

        _configuration = _configuration with { LanguageVersion = RazorLanguageVersion.Parse(razorLangVersion) };

        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(
            nullableContextEnabled ? NullableContextOptions.Enable: NullableContextOptions.Disable));

        AddCSharpSyntaxTree("""
            namespace TestNamespace;

            public class TestModel
            {
                public string Name { get; set; } = string.Empty;

                public string Address { get; set; } = string.Empty;
            }
            """);

        // Act
        var generated = CompileToCSharp($"""
            @page
            @using TestNamespace
            @model TestModel{(nullableModel ? "?" : "")}

            <h1>@Model.Name</h1>

            <h2>@Model?.Address</h2>
            """,
            designTime: designTime);

        // Assert
        AssertDocumentNodeMatchesBaseline(generated.CodeDocument.GetDocumentNode(), testName: testName);
        AssertHtmlDocumentMatchesBaseline(RazorHtmlWriter.GetHtmlDocument(generated.CodeDocument), testName: testName);
        AssertCSharpDocumentMatchesBaseline(generated.CodeDocument.GetCSharpDocument(), testName: testName);
        AssertLinePragmas(generated.CodeDocument);
        AssertSourceMappingsMatchBaseline(generated.CodeDocument, testName: testName);
        var compiledAssembly = CompileToAssembly(generated, throwOnFailure: false);

        var diagnostics = compiledAssembly.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);

        if (nullableModel)
        {
            var commonDiagnostics = new[]
            {
                // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(76,90): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //         public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestModel?> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestModel?>)PageContext?.ViewData;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?"),
                // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(76,180): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //         public global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestModel?> ViewData => (global::Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<TestModel?>)PageContext?.ViewData;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?"),
                // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(77,25): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                //         public TestModel? Model => ViewData.Model;
                Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?")
            };

            if (nullableContextEnabled)
            {
                if (razorLangVersion == "8.0")
                {
                    diagnostics.Verify([
                        // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(5,6): warning CS8602: Dereference of a possibly null reference.
                        // Model.Name
                        Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Model"),
                        ..commonDiagnostics]);
                }
                else
                {
                    diagnostics.Verify(
                        // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(5,6): warning CS8602: Dereference of a possibly null reference.
                        // Model.Name
                        Diagnostic(ErrorCode.WRN_NullReferenceReceiver, "Model"));
                }
            }
            else
            {
                diagnostics.Verify([
                    ..(designTime ? [
                        // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(3,10): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                        // TestModel? __typeHelper = default!;
                        Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?")] : DiagnosticDescription.None),
                    // TestFiles\IntegrationTests\CodeGenerationIntegrationTest\test.cshtml(74,80): warning CS8669: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. Auto-generated code requires an explicit '#nullable' directive in source.
                    //         public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<TestModel?> Html { get; private set; } = default!;
                    Diagnostic(ErrorCode.WRN_MissingNonNullTypesContextForAnnotationInGeneratedCode, "?"),
                    ..commonDiagnostics]);
            }
        }
        else
        {
            diagnostics.Verify();
        }
    }
}
