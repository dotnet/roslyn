// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveCodeGenerationTest_Legacy()
    : IntegrationTestBase(TestProject.Layer.Compiler)
{
    private RazorConfiguration _configuration =
        RazorConfiguration.Default with { LanguageVersion = RazorLanguageVersion.Preview };

    protected override RazorConfiguration Configuration => _configuration;

    protected override CSharpParseOptions CSharpParseOptions { get; } =
        new(LanguageVersion.Preview, documentationMode: DocumentationMode.Diagnose);

    protected override void ConfigureProjectEngine(RazorProjectEngineBuilder builder)
        => RazorExtensions.Register(builder);

    [Fact]
    public void Documentation()
    {
        var result = VerifyBaselineWithSourceMappings();

        var compiled = CompileToAssembly(result);
        var @namespace = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryNamespace();
        var @class = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryClass();
        Assert.NotNull(@namespace);
        Assert.NotNull(@class);
        var type = compiled.Compilation.GetTypeByMetadataName($"{@namespace.Name}.{@class.Name}");
        Assert.NotNull(type);
        Assert.Contains("<remarks>More documentation.</remarks>", type.GetDocumentationCommentXml());
    }

    [Fact]
    public void SingleLine()
        => CompileToAssembly(VerifyBaselineWithSourceMappings());

    [Fact]
    public void XmlOnOpeningBraceLine()
        => CompileToAssembly(VerifyBaselineWithSourceMappings());

    [Fact]
    public void XmlOnClosingBraceLine()
        => CompileToAssembly(VerifyBaselineWithSourceMappings());

    [Fact]
    public void XmlOnBothBraceLines()
        => CompileToAssembly(VerifyBaselineWithSourceMappings());

    [Fact]
    public void RawXml()
    {
        var result = VerifyBaselineWithSourceMappings();

        CompileToAssembly(result);
    }

    [Fact]
    public void MalformedInlineXml()
    {
        var result = VerifyBaselineWithSourceMappings();

        Assert.Empty(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics);
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void ClosingTagInFollowingCode()
    {
        var result = VerifyBaselineWithSourceMappings();

        Assert.Empty(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics);
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void SameLineMarkupAfterMalformedXml()
    {
        var result = VerifyBaselineWithSourceMappings();

        Assert.Empty(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics);
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void MissingClosingBraceBeforeCode()
    {
        var result = VerifyBaseline();

        Assert.Equal("RZ1006", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        CompileToAssembly(result, ignoreRazorDiagnostics: true);
    }

    [Theory]
    [InlineData("</summary>")]
    [InlineData("</div>")]
    public void MalformedXmlDoesNotConsumeClosingTagsInFollowingFunctions(string endTag)
    {
        var result = CompileToCSharp($$"""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @functions { /* note */ public string EndTag => "{{endTag}}"; }
            """);
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();

        Assert.Empty(generated.Diagnostics);
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("/* note */", generated.Text.ToString());
        Assert.Contains("public string EndTag", generated.Text.ToString());
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Theory]
    [InlineData("<summary>Missing end tag")]
    [InlineData("""<summary title="Missing quote""")]
    [InlineData("<!-- Missing end")]
    [InlineData("<![CDATA[Missing end")]
    [InlineData("<?example Missing end")]
    public void MalformedXmlDoesNotConsumeSameLineMarkup(string xml)
    {
        var result = CompileToCSharp($$"""
            @documentation { {{xml}} }<p>After</p>
            @functions { /* note */ public int Value => 42; }
            """);
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();

        Assert.Empty(generated.Diagnostics);
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("/* note */", generated.Text.ToString());
        Assert.Contains("public int Value", generated.Text.ToString());
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingStatementBlock()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            <p>After</p>
            @{
                /* note */
                var endTag = "</summary>";
                Write(endTag);
            }
            """);
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();

        Assert.Empty(generated.Diagnostics);
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("/* note */", generated.Text.ToString());
        Assert.Contains("Write(endTag);", generated.Text.ToString());
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void MalformedXmlDoesNotConsumeFollowingSection()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag}
            @section Footer
            {
                <p>After</p>
                @{
                    /* note */
                    var endTag = "</summary>";
                    Write(endTag);
                }
            }
            """);
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();

        Assert.Empty(generated.Diagnostics);
        Assert.Contains("DefineSection(\"Footer\"", generated.Text.ToString());
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("/* note */", generated.Text.ToString());
        Assert.Contains("Write(endTag);", generated.Text.ToString());
        var compiled = CompileToAssembly(result, throwOnFailure: false);
        var diagnostics = compiled.Compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity >= DiagnosticSeverity.Warning);
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("CS1570", diagnostic.Id));
    }

    [Fact]
    public void MissingDocumentationBraceDoesNotConsumeFollowingFunctions()
    {
        var result = CompileToCSharp("""
            @documentation {<summary>Missing end tag and brace
            @functions
            {
                /* note */
                public string EndTag => "</summary>";
            }
            <p>After</p>
            """);
        var generated = result.CodeDocument.GetRequiredImplCSharpDocument();

        Assert.Equal("RZ1006", Assert.Single(generated.Diagnostics).Id);
        Assert.Contains("<p>After</p>", generated.Text.ToString());
        Assert.Contains("/* note */", generated.Text.ToString());
        Assert.Contains("public string EndTag", generated.Text.ToString());
        Assert.DoesNotContain("/**", generated.Text.ToString());
        CompileToAssembly(result, ignoreRazorDiagnostics: true);
    }

    [Fact]
    public void DirectiveLikeTextInsideXmlRemainsDocumentation()
    {
        var result = CompileToCSharp("""
            @documentation {
                <summary>
                }
                @functions { public string Example => "&lt;/summary&gt;"; }
                </summary>
            }
            @functions { public int Actual => 42; }
            <p>After</p>
            """);

        Assert.Empty(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics);
        var compiled = CompileToAssembly(result);
        var @namespace = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryNamespace();
        var @class = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryClass();
        Assert.NotNull(@namespace);
        Assert.NotNull(@class);
        var type = compiled.Compilation.GetTypeByMetadataName($"{@namespace.Name}.{@class.Name}");
        Assert.NotNull(type);
        Assert.Single(type.GetMembers("Actual"));
        Assert.Empty(type.GetMembers("Example"));
        Assert.Contains("@functions { public string Example", type.GetDocumentationCommentXml());
    }

    [Fact]
    public void DuplicateDocumentation()
    {
        var result = VerifyBaseline();

        var diagnostic = Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal("RZ2001", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
    }

    [Fact]
    public void CommentTerminator()
    {
        var result = VerifyBaseline();

        Assert.Equal("RZ1047", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
    }

    [Fact]
    public void IdentifierInRazor11()
    {
        _configuration = _configuration with
        {
            LanguageVersion = RazorLanguageVersion.Version_11_0,
            RazorWarningLevel = 11
        };
        var result = VerifyBaselineWithSourceMappings();

        Assert.Equal("RZ1048", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        CompileToAssembly(result, ignoreRazorDiagnostics: true);
    }

    [Fact]
    public void MissingOpeningBrace()
    {
        var result = VerifyBaseline();

        Assert.Equal("RZ1017", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        CompileToAssembly(result, ignoreRazorDiagnostics: true);
    }

    [Fact]
    public void DocumentationCannotBeImported()
    {
        AddProjectItemFromText("@documentation {<summary>Imported</summary>}");
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Equal("RZ0000", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        Assert.DoesNotContain("Imported", result.CodeDocument.GetRequiredImplCSharpDocument().Text.ToString());
    }

    [Fact]
    public void ImportedDocumentationWithMissingBodyPreservesLocalDocumentation()
    {
        AddProjectItemFromText("@documentation");
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Equal("RZ1012", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        var compiled = CompileToAssembly(result, ignoreRazorDiagnostics: true);
        var @namespace = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryNamespace();
        var @class = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryClass();
        Assert.NotNull(@namespace);
        Assert.NotNull(@class);
        var type = compiled.Compilation.GetTypeByMetadataName($"{@namespace.Name}.{@class.Name}");
        Assert.NotNull(type);
        Assert.Contains("<summary>Local</summary>", type.GetDocumentationCommentXml());
    }

    [Fact]
    public void ImportedDocumentationWithCommentTerminatorPreservesLocalDocumentation()
    {
        AddProjectItemFromText("@documentation {<summary>Imported */</summary>}");
        var result = CompileToCSharp("@documentation {<summary>Local</summary>}");

        Assert.Equal("RZ1047", Assert.Single(result.CodeDocument.GetRequiredImplCSharpDocument().Diagnostics).Id);
        var compiled = CompileToAssembly(result, ignoreRazorDiagnostics: true);
        var @namespace = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryNamespace();
        var @class = result.CodeDocument.GetRequiredDocumentNode().FindPrimaryClass();
        Assert.NotNull(@namespace);
        Assert.NotNull(@class);
        var type = compiled.Compilation.GetTypeByMetadataName($"{@namespace.Name}.{@class.Name}");
        Assert.NotNull(type);
        Assert.Contains("<summary>Local</summary>", type.GetDocumentationCommentXml());
    }

    [Theory]
    [InlineData("9.0")]
    [InlineData("11.0")]
    public void DocumentationRequiresRazor12(string languageVersion)
    {
        _configuration = _configuration with { LanguageVersion = RazorLanguageVersion.Parse(languageVersion) };
        var result = CompileToCSharp("@documentation {<summary>Not supported</summary>}");

        Assert.Null(result.CodeDocument.GetRequiredSyntaxTree().Root.DescendantNodes()
            .OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    private CompiledCSharpCode VerifyBaseline([CallerMemberName] string testName = "")
    {
        var result = CompileToCSharp(CreateProjectItemFromFile(testName: testName));
        AssertSyntaxTreeMatchesBaseline(result.CodeDocument, testName);
        AssertDocumentNodeMatchesBaseline(result.CodeDocument.GetRequiredDocumentNode(), testName);
        AssertCSharpDocumentMatchesBaseline(result.CodeDocument.GetRequiredImplCSharpDocument(), testName);

        return result;
    }

    private CompiledCSharpCode VerifyBaselineWithSourceMappings([CallerMemberName] string testName = "")
    {
        var result = VerifyBaseline(testName);
        AssertSourceMappingsMatchBaseline(result.CodeDocument, testName);
        return result;
    }
}
