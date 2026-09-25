// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class UseExplicitExpressionTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Theory]
    [InlineData("")]
    [InlineData(" foo")]
    [InlineData("{}")]
    [InlineData(" {}")]
    [InlineData(" {<summary>Text</summary>}")]
    [InlineData(" {<summary>\nText\n</summary>}")]
    [InlineData(" {\n<summary>Text</summary>\n}")]
    [InlineData("\r\n{\r\n<summary>Text</summary>\r\n}")]
    [InlineData(" {")]
    [InlineData(" {\n<summary>Text</summary>")]
    public Task DocumentationVariants(string suffix)
        => VerifyAsync(
            input: $"""
                @[||]documentation{suffix}
                """,
            expected: $"""
                @(documentation){suffix}
                """,
            languageVersion: "11.0");

    [Fact]
    public Task NotOfferedForDocumentationDirectiveInRazor12()
        => VerifyAsync(
            input: """
                @docu[||]mentation {
                    <summary>Text</summary>
                }
                """,
            expected: null,
            languageVersion: "12.0");

    [Theory]
    [InlineData("documentation.Length")]
    [InlineData("documentation.ToString()")]
    [InlineData("documentation()")]
    [InlineData("documentation[0]")]
    [InlineData("documentation?.Length")]
    [InlineData("documentation.ToString().Length")]
    [InlineData("documentation(\n    1,\n    2)")]
    [InlineData("""documentation["}"]""")]
    public Task WrapsTheWholeExpression(string expression)
        => VerifyAsync(
            input: $"""
                @[||]{expression} after
                """,
            expected: $"""
                @({expression}) after
                """,
            languageVersion: "11.0");

    [Fact]
    public Task NotOfferedForInvocationInRazor12()
        => VerifyAsync(
            input: """
                @docu[||]mentation.ToString().Length after
                """,
            expected: null,
            languageVersion: "12.0");

    [Theory]
    [InlineData("@[||]documentation foo")]
    [InlineData("@docu[||]mentation foo")]
    [InlineData("@documentation[||] foo")]
    public Task CaretWithinDiagnostic(string input)
        => VerifyAsync(
            input,
            expected: """
                @(documentation) foo
                """);

    [Fact]
    public Task SelectionIncludesDiagnostic()
        => VerifyAsync(
            input: """
                [|  @documentation.Length foo|]
                """,
            expected: """
                  @(documentation.Length) foo
                """);

    [Fact]
    public Task OnlyChangesTheSelectedExpression()
        => VerifyAsync(
            """
            @documentation {<summary>First</summary>}
            @docu[||]mentation.ToString()
            @documentation foo
            """,
            """
            @documentation {<summary>First</summary>}
            @(documentation.ToString())
            @documentation foo
            """);

    [Fact]
    public Task NotOfferedForUnrelatedDiagnostic()
        => VerifyAsync(
            input: """
                @{|CS0103:docu[||]mentation|} foo
                """,
            expected: null,
            makeDiagnosticsRequest: false);

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public Task NotOfferedWhenWarningIsSuppressed(int warningLevel)
        => VerifyAsync(
            input: """
                @docu[||]mentation foo
                """,
            expected: null,
            warningLevel: warningLevel);

    [Fact]
    public Task OfferedInRazor9WhenWarningLevelIsRaised()
        => VerifyAsync(
            input: """
                @docu[||]mentation foo
                """,
            expected: """
                @(documentation) foo
                """,
            languageVersion: "9.0",
            warningLevel: 11);

    [Fact]
    public Task NotOfferedForSuppressedWarningInRazor9()
        => VerifyAsync(
            input: """
                @docu[||]mentation foo
                """,
            expected: null,
            languageVersion: "9.0",
            warningLevel: 9);

    [Fact]
    public Task NotOfferedForExplicitExpression()
        => VerifyAsync(
            input: """
                @([||]documentation) foo
                """,
            expected: null);

    [Fact]
    public Task NotOfferedForExplicitMemberAccess()
        => VerifyAsync(
            input: """
                @([||]documentation.Length)
                """,
            expected: null);

    [Fact]
    public Task NotOfferedForExplicitInvocation()
        => VerifyAsync(
            input: """
                @([||]documentation())
                """,
            expected: null);

    [Fact]
    public Task NotOfferedForExplicitExpressionBeforeBraces()
        => VerifyAsync(
            input: """
                @([||]documentation) {<summary>Text</summary>}
                """,
            expected: null);

    [Fact]
    public Task NotOfferedForEscapedTransition()
        => VerifyAsync(
            input: """
                @@[||]documentation foo
                """,
            expected: null);

    [Fact]
    public Task NotOfferedOutsideTheDiagnostic()
        => VerifyAsync(
            input: """
                @documentation <p>[||]After</p>
                """,
            expected: null);

    private Task VerifyAsync(
        string input,
        string? expected,
        string languageVersion = "11.0",
        int warningLevel = 11,
        bool makeDiagnosticsRequest = true)
        => VerifyCodeActionAsync(
            input,
            expected,
            LanguageServerConstants.CodeActions.UseExplicitExpression,
            fileKind: RazorFileKind.Component,
            makeDiagnosticsRequest: makeDiagnosticsRequest,
            projectConfigure: project =>
            {
                project.RazorLanguageVersion = RazorLanguageVersion.Parse(languageVersion);
                project.AddAnalyzerConfigDocument(
                    FilePath("Warnings.globalconfig"),
                    SourceText.From($"""
                        is_global = true
                        build_property.RazorWarningLevel = {warningLevel}
                        """));
            });
}
