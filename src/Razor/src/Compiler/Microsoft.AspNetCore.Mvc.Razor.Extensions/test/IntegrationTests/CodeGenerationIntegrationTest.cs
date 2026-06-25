// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;
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

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
    {
        base.ConfigureProjectEngine(builder);

        // Register the UTF-8 WriteLiteral feature with a pre-computed support map.
        var supportMap = new DefaultUtf8WriteLiteralFeature.Utf8SupportMap(
            ImmutableSortedDictionary<string, string>.Empty,
            ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, new[]
            {
                new KeyValuePair<string, bool>("MyUtf8PageBase", true),
                new KeyValuePair<string, bool>("MyPageBase", false),
            }));
        builder.Features.Add(new DefaultUtf8WriteLiteralFeature { SupportMap = supportMap });
    }

    #region Runtime

    [ConditionalFact(typeof(IsEnglishLocal))]
    public void UsingDirectives()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

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
        AssertLinePragmas(compiled.CodeDocument);

        var diagnotics = compiled.CodeDocument.GetCSharpDocument().Diagnostics;
        Assert.Equal("RZ1014", Assert.Single(diagnotics).Id);
    }

    [Fact]
    public void IncompleteDirectives()
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
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        // We expect this test to generate a bunch of errors.
        Assert.NotEmpty(compiled.CodeDocument.GetCSharpDocument().Diagnostics);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/7421")]
    public void RazorComment_AdjacentCommentsInCodeBlock()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToCSharp(projectItem);

        // Assert
        AssertSyntaxTreeMatchesBaseline(compiled.CodeDocument);
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void InheritsViewModel()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void InheritsWithViewImports()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [ConditionalFact(typeof(IsEnglishLocal))]
    public void AttributeDirectiveWithViewImports()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile();
        AddProjectItemFromText("""

            @using System
            @attribute [Serializable]
            """);

        // Act
        var compiled = CompileToAssembly(projectItem, throwOnFailure: false);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);

        var diagnostics = compiled.Compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning);
        Assert.Equal("Duplicate 'Serializable' attribute", Assert.Single(diagnostics).GetMessage());
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
        AssertLinePragmas(compiled.CodeDocument);

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
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void BasicComponent()
    {
        // Arrange
        var projectItem = CreateProjectItemFromFile(fileKind: RazorFileKind.Component);

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void Sections()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
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
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void Inject()
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
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ModelExpressionTagHelper()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPages()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
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
        AssertSourceMappingsMatchBaseline(compiled.CodeDocument);
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void RazorPagesWithoutModel()
    {
        // Arrange
        AddCSharpSyntaxTree("""
            public class DivTagHelper : Microsoft.AspNetCore.Razor.TagHelpers.TagHelper
            {

            }
            """);

        var projectItem = CreateProjectItemFromFile();

        // Act
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
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
        AssertLinePragmas(compiled.CodeDocument);
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
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ViewComponentTagHelper()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
    }

    [Fact]
    public void ViewComponentTagHelperOptionalParam()
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
        var compiled = CompileToAssembly(projectItem);

        // Assert
        AssertDocumentNodeMatchesBaseline(compiled.CodeDocument.GetDocumentNode());
        AssertCSharpDocumentMatchesBaseline(compiled.CodeDocument.GetCSharpDocument());
        AssertLinePragmas(compiled.CodeDocument);
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
    public void InheritsDirective_RazorPages()
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
    public void InvalidCode_EmptyImplicitExpression()
    {
        // Act
        var generated = CompileToCSharp("""
            <html>
                <head>
                    @
                </head>
            </html>
            """);

        // Assert
        var intermediate = generated.CodeDocument.GetDocumentNode();
        var csharp = generated.CodeDocument.GetCSharpDocument();
        AssertDocumentNodeMatchesBaseline(intermediate);
        AssertCSharpDocumentMatchesBaseline(csharp);
        AssertSourceMappingsMatchBaseline(generated.CodeDocument);
        CompileToAssembly(generated, throwOnFailure: false, ignoreRazorDiagnostics: true);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public void Utf8HtmlLiterals_AutoDetectedFromInherits()
    {
        // Arrange
        _configuration = new(RazorLanguageVersion.Preview, "MVC-3.0", Extensions: []);

        AddCSharpSyntaxTree("""

            using System;
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Mvc.Razor;

            public abstract class MyUtf8PageBase : RazorPage
            {
                public void WriteLiteral(ReadOnlySpan<byte> value)
                {
                    WriteLiteral(System.Text.Encoding.UTF8.GetString(value));
                }
            }

            """);

        // Act
        var generated = CompileToCSharp("""
            @inherits MyUtf8PageBase

            <html>
            <body>
                <h1>Hello World</h1>
                <p>This is UTF-8 encoded HTML content.</p>
            </body>
            </html>
            """);

        // Assert
        CompileToAssembly(generated);

        var generatedCode = generated.CodeDocument.GetCSharpDocument().Text.ToString();
        Assert.Contains("u8)", generatedCode);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/8429")]
    public void Utf8HtmlLiterals_WithoutOverload_UsesStringLiterals()
    {
        // Arrange
        _configuration = new(RazorLanguageVersion.Preview, "MVC-3.0", Extensions: []);

        AddCSharpSyntaxTree("""

            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Mvc.Razor;

            public abstract class MyPageBase : RazorPage
            {
            }

            """);

        // Act
        var generated = CompileToCSharp("""
            @inherits MyPageBase

            <html>
            <body>
                <h1>Hello World</h1>
            </body>
            </html>
            """);

        // Assert
        CompileToAssembly(generated);

        var generatedCode = generated.CodeDocument.GetCSharpDocument().Text.ToString();
        Assert.DoesNotContain("u8)", generatedCode);
    }

    #endregion

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/razor/issues/7286")]
    public void RazorPage_NullableModel(bool nullableModel, bool nullableContextEnabled,
        [CombinatorialValues("8.0", "9.0", "Latest")] string razorLangVersion)
    {
        // Arrange

        // Construct "key" for baselines.
        var testName = "RazorPage_With" +
            (nullableModel ? "" : "Non") +
            "NullableModel_Lang" +
            (razorLangVersion == "8.0" ? "Old" : "New");

        _configuration = _configuration with { LanguageVersion = RazorLanguageVersion.Parse(razorLangVersion) };

        BaseCompilation = BaseCompilation.WithOptions(BaseCompilation.Options.WithNullableContextOptions(
            nullableContextEnabled ? NullableContextOptions.Enable : NullableContextOptions.Disable));

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
            """);

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
