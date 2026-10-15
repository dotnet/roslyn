// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class WrapDocumentationInSummaryTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WrapsPlainText(bool isComponent)
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]This is the summary }
                """,
            expected: """
                @documentation { <summary>This is the summary</summary> }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task OfferedAsQuickFix()
    {
        TestCode input = "@documentation { [||]This is the summary }";
        var document = CreateRazorDocument(input);
        var actions = await GetCodeActionsAsync(document, input, makeDiagnosticsRequest: true);

        Assert.NotNull(actions);
        var action = Assert.Single(actions, static action => action.Value is RazorVSInternalCodeAction
        {
            Name: LanguageServerConstants.CodeActions.WrapDocumentationInSummary
        });
        var codeAction = Assert.IsType<RazorVSInternalCodeAction>(action.Value);
        Assert.Equal("Wrap in <summary>", codeAction.Title);
        Assert.Equal(CodeActionKind.QuickFix, codeAction.Kind);
        Assert.NotNull(codeAction.Edit);
    }

    [Fact]
    public async Task WrapsMultilineText()
    {
        await VerifyCodeActionAsync(
            input: """
                <p>Before</p>
                @documentation
                {
                    [||]This is the summary.
                    It has another line.
                }
                <p>After</p>
                @code { public int Value => 42; }
                """,
            expected: """
                <p>Before</p>
                @documentation
                {
                    <summary>This is the summary.
                    It has another line.</summary>
                }
                <p>After</p>
                @code { public int Value => 42; }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task PreservesOuterWhitespace()
    {
        await VerifyCodeActionAsync(
            input: "@documentation {\t\u00a0\u2003[||]This is the summary\u00a0 \t}",
            expected: "@documentation {\t\u00a0\u2003<summary>This is the summary</summary>\u00a0 \t}",
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u0085")]
    [InlineData("\u2028")]
    [InlineData("\u2029")]
    public async Task PreservesLineEndings(string lineEnding)
    {
        await VerifyCodeActionAsync(
            input: $"@documentation {{{lineEnding}\t[||]This is{lineEnding}\t  the summary  {lineEnding}}}",
            expected: $"@documentation {{{lineEnding}\t<summary>This is{lineEnding}\t  the summary</summary>  {lineEnding}}}",
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task PreservesExistingXmlAndRazorTransitions()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]See <see cref="System.String"/> &amp; @literal. }
                """,
            expected: """
                @documentation { <summary>See <see cref="System.String"/> &amp; @literal.</summary> }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task SelectionContainingDiagnosticWrapsWholeBody()
    {
        await VerifyCodeActionAsync(
            input: """
                [|  @documentation { This is|] the summary }
                """,
            expected: """
                  @documentation { <summary>This is the summary</summary> }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task OnlyChangesSelectedDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { First summary }
                @documentation { [||]Second summary }
                """,
            expected: """
                @documentation { First summary }
                @documentation { <summary>Second summary</summary> }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task MissingClosingBraceDoesNotConsumeFollowingCode()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    [||]This is the summary
                @code { public int Value => 42; }
                """,
            expected: """
                @documentation {
                    <summary>This is the summary</summary>
                @code { public int Value => 42; }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task WrappedDocumentationHasNoDiagnostics()
    {
        var document = CreateRazorDocument("@documentation { <summary>This is the summary</summary> }");
        var requestInvoker = new TestHtmlRequestInvoker();

        var diagnostics = await CohostDocumentPullDiagnosticsTest.MakeDiagnosticsRequestAsync(
            document, taskListRequest: false, requestInvoker, IncompatibleProjectService,
            RemoteServiceInvoker, ClientCapabilitiesService, LoggerFactory, DisposalToken);

        Assert.NotNull(diagnostics);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NotOfferedOutsideDiagnostic()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { This is [||]the summary }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForExistingXml()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { <summary>[||]This is the summary</summary> }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Theory]
    [InlineData("@documentation {[||]}")]
    [InlineData("@documentation { \t[||]\r\n }")]
    public async Task NotOfferedForEmptyBody(string input)
    {
        await VerifyCodeActionAsync(
            input,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForUnrelatedDiagnostic()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { <summary>*[||]/</summary> }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedWithoutDiagnostic()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]This is the summary }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary);
    }

    [Fact]
    public async Task NotOfferedBeforeRazor12()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]This is the summary }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true,
            projectConfigure: static builder => builder.RazorLanguageVersion = RazorLanguageVersion.Version_11_0);
    }

    [Fact]
    public async Task NotOfferedForCommentTerminator()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]This is the summary */ }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapDocumentationInSummary,
            makeDiagnosticsRequest: true);
    }
}
