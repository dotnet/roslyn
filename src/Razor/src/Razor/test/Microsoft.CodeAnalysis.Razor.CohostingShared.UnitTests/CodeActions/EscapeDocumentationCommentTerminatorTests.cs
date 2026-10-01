// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Roslyn.Test.Utilities.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

[WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
public class EscapeDocumentationCommentTerminatorTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task EscapeTerminatorInXmlText()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>/* example *[||]/</summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>/* example *&#47;</summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInAttributeValue()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>
                        <see href="*[||]/"/>
                    </summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>
                        <see href="*&#47;"/>
                    </summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInCData()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[/* example *[||]/]]></summary>
                }
                """,
            expected: """
                @documentation {
                    <summary><![CDATA[/* example *]]>&#47;<![CDATA[]]></summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInMultilineCData_LF()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[
                /* example *[||]/
                ]]></summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation {
                    <summary><![CDATA[
                /* example *]]>&#47;<![CDATA[
                ]]></summary>
                }
                """.Replace("\r\n", "\n"),
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInMultilineCData_CRLF()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[
                /* example *[||]/
                ]]></summary>
                }
                """.Replace("\r\n", "\n").Replace("\n", "\r\n"),
            expected: """
                @documentation {
                    <summary><![CDATA[
                /* example *]]>&#47;<![CDATA[
                ]]></summary>
                }
                """.Replace("\r\n", "\n").Replace("\n", "\r\n"),
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInXmlComment()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>
                        <!-- *[||]/ -->
                    </summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>
                        <!-- *&#47; -->
                    </summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorAfterCDataMarkerInXmlComment()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>
                        <!-- <![CDATA[ -->*[||]/
                    </summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>
                        <!-- <![CDATA[ -->*&#47;
                    </summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorAfterCDataMarkerInProcessingInstruction()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>
                        <?doc <![CDATA[ *[||]/ ?>
                    </summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>
                        <?doc <![CDATA[ *&#47; ?>
                    </summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorAfterClosedCData()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[done]]>*[||]/</summary>
                }
                """,
            expected: """
                @documentation {
                    <summary><![CDATA[done]]>*&#47;</summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInUnterminatedCData()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[*[||]/}
                """,
            expected: """
                @documentation {
                    <summary><![CDATA[*]]>&#47;<![CDATA[}
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task OnlyEscapesReportedTerminator()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>*[||]/ and */</summary>
                }
                """,
            expected: """
                @documentation {
                    <summary>*&#47; and */</summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorInView()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary><![CDATA[/* example *[||]/]]></summary>
                }
                """,
            expected: """
                @documentation {
                    <summary><![CDATA[/* example *]]>&#47;<![CDATA[]]></summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task MultilineDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                <p>Before</p>
                @documentation
                {
                    <summary>
                    /* example *[||]/
                    </summary>
                }
                <p>After</p>
                """,
            expected: """
                <p>Before</p>
                @documentation
                {
                    <summary>
                    /* example *&#47;
                    </summary>
                }
                <p>After</p>
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task DiagnosticInSelection()
    {
        await VerifyCodeActionAsync(
            input: """
                [|  @documentation {<summary>*/</summary>}|]
                """,
            expected: """
                  @documentation {<summary>*&#47;</summary>}
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task OnlyChangesSelectedDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>*/</summary>}
                @documentation {
                    <summary><![CDATA[*[||]/]]></summary>
                }
                """,
            expected: """
                @documentation {<summary>*/</summary>}
                @documentation {
                    <summary><![CDATA[*]]>&#47;<![CDATA[]]></summary>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForUnrelatedDiagnostic()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation { [||]This is the summary }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForCSharpCommentAfterMalformedDocumentation()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag}
                @code { /* note *[||]/ }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForCSharpCommentAfterClosingTagString()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag}
                <p>After</p>
                @code { public string EndTag => "</summary>"; /* note *[||]/ }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForCSharpCommentAfterSameLineMarkup()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag}<p>After</p>
                @code { /* note *[||]/ public int Value => 42; }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForCSharpCommentAfterMissingDocumentationBrace()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag and brace
                @code { /* note *[||]/ public int Value => 42; }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForViewCommentAfterClosingTagString()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag}
                <p>After</p>
                @functions { public string EndTag => "</summary>"; /* note *[||]/ }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForViewCommentAfterSameLineMarkup()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {<summary>Missing end tag}<p>After</p>
                @functions { /* note *[||]/ public int Value => 42; }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            fileKind: RazorFileKind.Legacy,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task EscapeTerminatorAfterBraceInDocumentationExample()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>
                    }
                    @code { /* documentation example *[||]/ }
                    </summary>
                }
                @code { /* actual code */ public int Value => 42; }
                """,
            expected: """
                @documentation {
                    <summary>
                    }
                    @code { /* documentation example *&#47; }
                    </summary>
                }
                @code { /* actual code */ public int Value => 42; }
                """,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedForEscapedTerminator()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>*[||]&#47;</summary>
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedWithoutTerminator()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>[||]No terminator.</summary>
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedAwayFromTerminator()
    {
        await VerifyCodeActionAsync(
            input: """
                @documentation {
                    <summary>[||]Before */</summary>
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }

    [Fact]
    public async Task NotOfferedOutsideDocumentation()
    {
        await VerifyCodeActionAsync(
            input: """
                <p>*[||]/</p>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.EscapeDocumentationCommentTerminator,
            makeDiagnosticsRequest: true);
    }
}
