// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class DocumentationDirectiveCompatibilityTest()
    : IntegrationTestBase(TestProject.Layer.Compiler)
{
    [Theory]
    [CombinatorialData]
    public void Razor11WarnsWithoutChangingExpressionSemantics(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "@documentation",
            "@documentation foo",
            "@documentation.Length",
            "@documentation.ToString()",
            "@documentation.ToString().Length",
            "@documentation[0]",
            "@documentation?.Length",
            "@documentation{}",
            "@documentation {}",
            "@documentation { This is the summary }",
            "@documentation {<summary>Text</summary>}",
            "@documentation {<summary>\nText\n</summary>}",
            "@documentation {\n<summary>Text</summary>\n}",
            "@documentation\r\n{\r\n<summary>Text</summary>\r\n}",
            "@documentation {",
            "@documentation {\n<summary>Text</summary>")]
        string expression)
    {
        var source = $$"""
            {{expression}}
            @functions {
                private string documentation => "Value";
            }
            """;
        var document = Process(source, isComponent, RazorLanguageVersion.Version_11_0, 11, useRoslynTokenizer);
        var generated = document.GetRequiredImplCSharpDocument();
        var diagnostic = Assert.Single(generated.Diagnostics);

        Assert.Equal("RZ1048", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(11, diagnostic.WarningLevel);
        Assert.Equal(source.IndexOf("documentation", StringComparison.Ordinal), diagnostic.Span.AbsoluteIndex);
        Assert.Equal("documentation".Length, diagnostic.Span.Length);
        Assert.Equal(
            "The '@documentation' syntax will be treated as a directive in Razor 12.0. Use an explicit expression ('@(...)') to preserve the current meaning.",
            diagnostic.GetMessage());
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
        Assert.Single<CSharpImplicitExpressionSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<CSharpImplicitExpressionSyntax>()]);
        CompileToAssembly(new CompiledCSharpCode(BaseCompilation, document), ignoreRazorDiagnostics: true);

        var suppressed = Process(source, isComponent, RazorLanguageVersion.Version_11_0, 10, useRoslynTokenizer);
        Assert.Empty(suppressed.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal(generated.Text.ToString(), suppressed.GetRequiredImplCSharpDocument().Text.ToString());
        Assert.Equal(document.GetDeclCSharpDocument()?.Text.ToString(), suppressed.GetDeclCSharpDocument()?.Text.ToString());
    }

    [Theory]
    [CombinatorialData]
    public void CompatibilityWarningIsReportedAtWarningLevel11OrHigher(
        bool isComponent,
        [CombinatorialValues("9.0", "11.0")] string languageVersion,
        [CombinatorialValues(11, 12)] int warningLevel)
    {
        var document = Process("@documentation foo", isComponent, RazorLanguageVersion.Parse(languageVersion), warningLevel);

        Assert.Equal("RZ1048", Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics).Id);
    }

    [Theory]
    [CombinatorialData]
    public void CompatibilityWarningIsSuppressedBelowWarningLevel11(
        bool isComponent,
        [CombinatorialValues("9.0", "11.0")] string languageVersion,
        [CombinatorialValues(0, 10)] int warningLevel)
    {
        var document = Process("@documentation foo", isComponent, RazorLanguageVersion.Parse(languageVersion), warningLevel);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
    }

    [Theory]
    [CombinatorialData]
    public void ExplicitExpressionsAndOtherIdentifiersDoNotWarn(
        bool isComponent,
        [CombinatorialValues("11.0", "12.0")] string languageVersion,
        [CombinatorialValues(
            "@(documentation) foo",
            "@(documentation.Length)",
            "@(documentation.ToString())",
            "@(documentation.ToString().Length)",
            "@(documentation[0])",
            "@(documentation?.Length)",
            "@(documentation){}",
            "@(documentation) {}",
            "@(documentation) {<summary>Text</summary>}",
            "@(documentation) {<summary>\nText\n</summary>}",
            "@(documentation) {\n<summary>Text</summary>\n}",
            "@(documentation)\r\n{\r\n<summary>Text</summary>\r\n}",
            "@(documentation) {",
            "@(documentation) {\n<summary>Text</summary>",
            "@@documentation foo",
            "@documentationOther foo",
            "@Documentation foo",
            "@* @documentation foo *@",
            "<p>documentation@example.com</p>")]
        string expression)
    {
        var source = $$"""
            {{expression}}
            @functions {
                private string documentation => "Value";
                private string documentationOther => "Other value";
                private string Documentation => "Case-sensitive value";
            }
            """;
        var document = Process(source, isComponent, RazorLanguageVersion.Parse(languageVersion), warningLevel: 12);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
        CompileToAssembly(new CompiledCSharpCode(BaseCompilation, document));
    }

    [Theory]
    [CombinatorialData]
    public void ImplicitDocumentationInMarkupProducesCompatibilityWarning(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "<p>@documentation</p>",
            """<p title="@documentation">After</p>""",
            "@if (true) { <p>@documentation</p> }")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_11_0, 11, useRoslynTokenizer);

        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal("RZ1048", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    [Theory]
    [CombinatorialData]
    public void Razor12ParsesDocumentationInMarkupAsADirective(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "<p>@documentation</p>",
            """<p title="@documentation">After</p>""",
            "@if (true) { <p>@documentation</p> }")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);

        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
    }

    [Theory]
    [CombinatorialData]
    public void Razor11DoesNotWarnForCSharpDocumentationReferences(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues("@{ var value = documentation; }", "@(documentation)")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_11_0, 11, useRoslynTokenizer);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    [Theory]
    [CombinatorialData]
    public void Razor12DoesNotParseCSharpDocumentationReferencesAsDirectives(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues("@{ var value = documentation; }", "@(documentation)")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    [Theory]
    [CombinatorialData]
    public void Razor12RecognizesDocumentation(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "@documentation{}",
            "@documentation {}",
            "@documentation {<summary>Text</summary>}",
            "@documentation {<summary>\nText\n</summary>}",
            "@documentation {\n<summary>Text</summary>\n}",
            "@documentation\r\n{\r\n<summary>Text</summary>\r\n}")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
        CompileToAssembly(new CompiledCSharpCode(BaseCompilation, document));
    }

    [Theory]
    [CombinatorialData]
    public void DocumentationPrefixWarningIsAlwaysOn(bool isComponent, bool useRoslynTokenizer)
    {
        var document = Process(
            "@documentation { This is the summary }",
            isComponent, RazorLanguageVersion.Version_12_0, warningLevel: 0, useRoslynTokenizer);

        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);
        Assert.Equal("RZ1049", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal(0, diagnostic.WarningLevel);
        Assert.Equal(17, diagnostic.Span.AbsoluteIndex);
        Assert.Equal(1, diagnostic.Span.Length);
        CompileToAssembly(new CompiledCSharpCode(BaseCompilation, document), ignoreRazorDiagnostics: true);
    }

    [Theory]
    [CombinatorialData]
    public void Razor11ReportsCompatibilityWarningForDocumentationWithMissingBraces(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "@documentation",
            "@documentation foo",
            "@documentation.Length",
            "@documentation()",
            "@documentation[0]",
            "@documentation {",
            "@documentation {\n<summary>Text</summary>")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_11_0, 12, useRoslynTokenizer);
        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);

        Assert.Equal("RZ1048", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Null(document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>().FirstOrDefault());
    }

    [Theory]
    [CombinatorialData]
    public void Razor12ReportsUnexpectedEndOfFileAfterDocumentation(bool isComponent, bool useRoslynTokenizer)
    {
        var document = Process("@documentation", isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);
        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);

        Assert.Equal("RZ1012", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
    }

    [Theory]
    [CombinatorialData]
    public void Razor12ReportsMissingOpeningBrace(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues(
            "@documentation foo",
            "@documentation.Length",
            "@documentation()",
            "@documentation[0]")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);
        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);

        Assert.Equal("RZ1017", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
    }

    [Theory]
    [CombinatorialData]
    public void Razor12ReportsMissingClosingBrace(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues("@documentation {", "@documentation {\n<summary>Text</summary>")]
        string source)
    {
        var document = Process(source, isComponent, RazorLanguageVersion.Version_12_0, 12, useRoslynTokenizer);
        var diagnostic = Assert.Single(document.GetRequiredImplCSharpDocument().Diagnostics);

        Assert.Equal("RZ1006", diagnostic.Id);
        Assert.Equal(RazorDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Single<RazorDocumentationDirectiveSyntax>(
            [.. document.GetRequiredSyntaxTree().Root.DescendantNodes().OfType<RazorDocumentationDirectiveSyntax>()]);
    }

    [Theory]
    [CombinatorialData]
    public void ExplicitInvocationCompilesInBothVersions(
        bool isComponent,
        bool useRoslynTokenizer,
        [CombinatorialValues("11.0", "12.0")] string languageVersion)
    {
        var document = Process("""
            @(documentation())
            @functions {
                private string documentation() => "Value";
            }
            """,
            isComponent, RazorLanguageVersion.Parse(languageVersion), 12, useRoslynTokenizer);

        Assert.Empty(document.GetRequiredImplCSharpDocument().Diagnostics);
        CompileToAssembly(new CompiledCSharpCode(BaseCompilation, document));
    }

    private RazorCodeDocument Process(
        string source,
        bool isComponent,
        RazorLanguageVersion languageVersion,
        int warningLevel,
        bool useRoslynTokenizer = true)
    {
        var configuration = Configuration with
        {
            LanguageVersion = languageVersion,
            RazorWarningLevel = warningLevel
        };
        var engine = RazorProjectEngine.Create(configuration, FileSystem, builder =>
        {
            RazorExtensions.Register(builder);
            builder.SetCSharpLanguageVersion(LanguageVersion.Preview);
            builder.ConfigureCodeGenerationOptions(options => options.EnableMarkupSplit = true);
            builder.ConfigureParserOptions(options =>
            {
                options.UseRoslynTokenizer = useRoslynTokenizer;
                options.CSharpParseOptions = CSharpParseOptions;
            });
        });
        var item = new TestRazorProjectItem(
            basePath: "/",
            filePath: isComponent ? "/TestComponent.razor" : "/TestView.cshtml")
        {
            Content = source
        };
        return engine.Process(item);
    }
}
