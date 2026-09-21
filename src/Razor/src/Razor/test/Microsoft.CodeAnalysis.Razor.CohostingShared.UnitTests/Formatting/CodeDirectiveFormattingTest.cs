// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;

public class CodeDirectiveFormattingTest(ITestOutputHelper testOutput) : DocumentFormattingTestBase(testOutput)
{
    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectivePreservesXmlText()
    {
        await RunFormattingTestAsync(
            input: """
                @documentation {
                        <summary>
                  Keep   this spacing, @transitions, and }.
                        </summary>
                    <example><![CDATA[
                if (true)
                {
                }
                    ]]></example>
                }

                @code {
                private   int _count;
                }
                """,
            htmlFormatted: """
                @documentation {
                        <summary>
                  Keep   this spacing, @transitions, and }.
                        </summary>
                    <example><![CDATA[
                if (true)
                {
                }
                    ]]></example>
                }

                @code {
                private   int _count;
                }
                """,
            expected: """
                @documentation {
                        <summary>
                  Keep   this spacing, @transitions, and }.
                        </summary>
                    <example><![CDATA[
                if (true)
                {
                }
                    ]]></example>
                }

                @code {
                    private int _count;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: """
                @documentation {
                    <summary>
                    Keep   this spacing, @transitions, and }.
                    </summary>
                }

                @code {
                    private int _count;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithBraceOnNextLine()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: """
                @documentation
                {
                    <summary>
                    Keep   this spacing, @transitions, and }.
                    </summary>
                }

                @code {
                    private int _count;
                }
                """);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithBraceOptionEnabled()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: """
                @documentation
                {
                    <summary>
                    Keep   this spacing, @transitions, and }.
                    </summary>
                }

                @code
                {
                    private int _count;
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithBraceOnNextLineAndBraceOptionEnabled()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: """
                @documentation
                {
                    <summary>
                    Keep   this spacing, @transitions, and }.
                    </summary>
                }

                @code
                {
                    private int _count;
                }
                """,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabs()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: $$"""
                @documentation {
                {{"\t"}}<summary>
                {{"\t"}}Keep   this spacing, @transitions, and }.
                {{"\t"}}</summary>
                }

                @code {
                {{"\t"}}private int _count;
                }
                """,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabsAndBraceOnNextLine()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: $$"""
                @documentation
                {
                {{"\t"}}<summary>
                {{"\t"}}Keep   this spacing, @transitions, and }.
                {{"\t"}}</summary>
                }

                @code {
                {{"\t"}}private int _count;
                }
                """,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabsAndBraceOptionEnabled()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: $$"""
                @documentation
                {
                {{"\t"}}<summary>
                {{"\t"}}Keep   this spacing, @transitions, and }.
                {{"\t"}}</summary>
                }

                @code
                {
                {{"\t"}}private int _count;
                }
                """,
            codeBlockBraceOnNextLine: true,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabsAndBraceOnNextLineAndBraceOptionEnabled()
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            htmlFormatted: """
                    @documentation
                    {
                        <summary>
                        Keep   this spacing, @transitions, and }.
                        </summary>
                    }

                    @code {
                    private   int _count;
                    }
                """,
            expected: $$"""
                @documentation
                {
                {{"\t"}}<summary>
                {{"\t"}}Keep   this spacing, @transitions, and }.
                {{"\t"}}</summary>
                }

                @code
                {
                {{"\t"}}private int _count;
                }
                """,
            codeBlockBraceOnNextLine: true,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_IsIdempotent()
    {
        const string input = """
            @documentation {
                <summary>
                Keep   this spacing, @transitions, and }.
                </summary>
            }

            @code {
                private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithBraceOnNextLine_IsIdempotent()
    {
        const string input = """
            @documentation
            {
                <summary>
                Keep   this spacing, @transitions, and }.
                </summary>
            }

            @code {
                private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithBraceOptionEnabled_IsIdempotent()
    {
        const string input = """
            @documentation
            {
                <summary>
                Keep   this spacing, @transitions, and }.
                </summary>
            }

            @code
            {
                private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            codeBlockBraceOnNextLine: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabs_IsIdempotent()
    {
        const string input = $$"""
            @documentation {
            {{"\t"}}<summary>
            {{"\t"}}Keep   this spacing, @transitions, and }.
            {{"\t"}}</summary>
            }

            @code {
            {{"\t"}}private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabsAndBraceOnNextLine_IsIdempotent()
    {
        const string input = $$"""
            @documentation
            {
            {{"\t"}}<summary>
            {{"\t"}}Keep   this spacing, @transitions, and }.
            {{"\t"}}</summary>
            }

            @code {
            {{"\t"}}private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            insertSpaces: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveUsesRoslynFormatting_WithTabsAndBraceOptionEnabled_IsIdempotent()
    {
        const string input = $$"""
            @documentation
            {
            {{"\t"}}<summary>
            {{"\t"}}Keep   this spacing, @transitions, and }.
            {{"\t"}}</summary>
            }

            @code
            {
            {{"\t"}}private int _count;
            }
            """;

        await RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            codeBlockBraceOnNextLine: true, insertSpaces: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveFormatting_RelativeIndentation(bool isComponent)
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                        <summary>
                            Keep   this text.
                          <para>Indented  paragraph.</para>
                        </summary>
                        <remarks><see  cref = "System.String" /></remarks>
                    }
                <p>After</p>
                """,
            htmlFormatted: """
                    @documentation {
                        <summary>
                            Keep   this text.
                          <para>Indented  paragraph.</para>
                        </summary>
                        <remarks><see  cref = "System.String" /></remarks>
                    }
                <p>After</p>
                """,
            expected: """
                @documentation {
                    <summary>
                        Keep   this text.
                      <para>Indented  paragraph.</para>
                    </summary>
                    <remarks><see  cref = "System.String" /></remarks>
                }
                <p>After</p>
                """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveFormatting_WithTabs(bool isComponent)
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation {
                          <summary>
                            Keep   this text.
                          </summary>
                    }
                <p>After</p>
                """,
            htmlFormatted: """
                    @documentation {
                          <summary>
                            Keep   this text.
                          </summary>
                    }
                <p>After</p>
                """,
            expected: $$"""
                @documentation {
                {{"\t"}}  <summary>
                {{"\t\t"}}Keep   this text.
                {{"\t"}}  </summary>
                }
                <p>After</p>
                """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            insertSpaces: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public async Task DocumentationDirectiveFormatting_WithXmlOnBraceLines(bool isComponent)
    {
        await RunFormattingTestAsync(
            input: """
                    @documentation { <summary>
                            Keep   this text.
                        </summary> }
                <p>After</p>
                """,
            htmlFormatted: """
                    @documentation { <summary>
                            Keep   this text.
                        </summary> }
                <p>After</p>
                """,
            expected: """
                @documentation { <summary>
                        Keep   this text.
                    </summary> }
                <p>After</p>
                """,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_SingleLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {<summary>Text</summary>}

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_OpeningTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>
                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>
                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {<summary>
                Text
                </summary>
                }

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_ClosingTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>
                Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>
                Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {
                <summary>
                Text
                </summary>}

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_BothTagsBesideBraces(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {<summary>Text
                </summary>}

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_XmlOnNextLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {
                <summary>Text</summary>}

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_BlankLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>

                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>

                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {
                <summary>

                Text
                </summary>
                }

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_Empty(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation {}

                {{directive}} {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_SingleLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {<summary>Text</summary>}

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_OpeningTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>
                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>
                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {<summary>
                Text
                </summary>
                }

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_ClosingTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>
                Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>
                Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {
                <summary>
                Text
                </summary>}

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_BothTagsBesideBraces(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {<summary>Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {<summary>Text
                </summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {<summary>Text
                </summary>}

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_XmlOnNextLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>Text</summary>}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {
                <summary>Text</summary>}

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_BlankLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {
                <summary>

                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {
                <summary>

                Text
                </summary>
                }

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {
                <summary>

                Text
                </summary>
                }

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_Empty(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";

        return RunFormattingTestAsync(
            input: $$"""
                @documentation {}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation {}

                {{directive}} {
                private   int _count;
                }
                """.Replace("\r\n", "\n"),
            expected: $$"""
                @documentation
                {}

                {{directive}}
                {
                    private int _count;
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_SingleLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {<summary>Text</summary>}

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_OpeningTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {<summary>
            Text
            </summary>
            }

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_ClosingTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {
            <summary>
            Text
            </summary>}

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_BothTagsBesideBraces(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {<summary>Text
            </summary>}

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_XmlOnNextLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {
            <summary>Text</summary>}

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_BlankLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {
            <summary>

            Text
            </summary>
            }

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_IsIdempotent_Empty(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation {}

            {{directive}} {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_SingleLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {<summary>Text</summary>}

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_OpeningTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {<summary>
            Text
            </summary>
            }

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_ClosingTagBesideBrace(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {
            <summary>
            Text
            </summary>}

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_BothTagsBesideBraces(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {<summary>Text
            </summary>}

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_XmlOnNextLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {
            <summary>Text</summary>}

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_BlankLine(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {
            <summary>

            Text
            </summary>
            }

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithInlineXml_WithBraceOnNextLine_IsIdempotent_Empty(bool isComponent)
    {
        var directive = isComponent ? "@code" : "@functions";
        var input = $$"""
            @documentation
            {}

            {{directive}}
            {
                private int _count;
            }
            """.Replace("\r\n", "\n");

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_SameLine_NoWhitespace(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_SameLine_Spaces(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation   {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation   {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_SameLine_Tab(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: $$"""
                @documentation{{"\t"}}{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation{{"\t"}}{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_ExistingNewLine(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation

                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation

                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation
                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: false);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_WithBraceOnNextLine_NoWhitespace(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation
                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_WithBraceOnNextLine_Spaces(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation   {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation   {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation
                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_WithBraceOnNextLine_Tab(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: $$"""
                @documentation{{"\t"}}{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: $$"""
                @documentation{{"\t"}}{
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation
                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveBraceWhitespace_WithBraceOnNextLine_ExistingNewLine(bool isComponent)
    {
        return RunFormattingTestAsync(
            input: """
                @documentation

                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            htmlFormatted: """
                @documentation

                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            expected: """
                @documentation
                {
                    <summary>Text</summary>
                }
                """.Replace("\r\n", "\n"),
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: true);
    }

    [Theory]
    [CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithIncompleteBrace(bool isComponent, bool codeBlockBraceOnNextLine)
    {
        const string input = "@documentation {";

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input,
            fileKind: isComponent ? RazorFileKind.Component : RazorFileKind.Legacy,
            codeBlockBraceOnNextLine: codeBlockBraceOnNextLine, allowDiagnostics: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithMalformedContent_NoBraces()
    {
        const string input = "@documentation";

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input, allowDiagnostics: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithMalformedContent_MissingClosingBrace()
    {
        const string input = "@documentation {<summary>Missing closing brace</summary>";

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input, allowDiagnostics: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/roslyn/issues/85414")]
    public Task DocumentationDirectiveWithMalformedContent_CommentTerminator()
    {
        const string input = "@documentation {<summary>*/ Not C# /*</summary>}";

        return RunFormattingTestAsync(input: input, htmlFormatted: input, expected: input, allowDiagnostics: true);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/counter"

                @if(true)
                    {
                                // indented
                        }

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGeneric>

                @if(true)
                    {
                                // indented
                            }

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            expected: """
                @page "/counter"

                @if (true)
                {
                    // indented
                }

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                        <div></div>
                    }
                </TestGeneric>

                @if (true)
                {
                    // indented
                }

                @code
                {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            htmlFormatted: """
                @page "/counter"

                @if(true)
                    {
                                // indented
                        }

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                    <div></div>
                    }
                </TestGeneric>

                @if(true)
                    {
                                // indented
                            }

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            additionalFiles: [
                (FilePath("TestGeneric.razor"), """
                @using System.Collections.Generic
                @using Microsoft.AspNetCore.Components
                @typeparam TItem
                @attribute [CascadingTypeParameter(nameof(TItem))]

                <h3>TestGeneric</h3>

                @code
                {
                    [Parameter] public IEnumerable<TItem> Items { get; set; }
                    [Parameter] public RenderFragment ChildContent { get; set; }
                }
                """)]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter_Nested()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/counter"

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGeneric>
                    </TestGeneric>

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            expected: """
                @page "/counter"

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                        <div></div>
                    }
                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGeneric>
                </TestGeneric>

                @code
                {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            htmlFormatted: """
                @page "/counter"

                <TestGeneric Items="_items">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                    <div></div>
                    }
                    <TestGeneric Items="_items">
                        @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                        <div></div>
                        }
                    </TestGeneric>
                </TestGeneric>

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                }
                """,
            additionalFiles: [
                (FilePath("TestGeneric.razor"), """
                @using System.Collections.Generic
                @using Microsoft.AspNetCore.Components
                @typeparam TItem
                @attribute [CascadingTypeParameter(nameof(TItem))]

                <h3>TestGeneric</h3>

                @code
                {
                    [Parameter] public IEnumerable<TItem> Items { get; set; }
                    [Parameter] public RenderFragment ChildContent { get; set; }
                }
                """)]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/5648")]
    public async Task GenericComponentWithCascadingTypeParameter_MultipleParameters()
    {
        await RunFormattingTestAsync(
            input: """
                @page "/counter"

                <TestGenericTwo Items="_items" ItemsTwo="_items2">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                        {
                            <div></div>
                        }
                    </TestGenericTwo>

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                }
                """,
            expected: """
                @page "/counter"

                <TestGenericTwo Items="_items" ItemsTwo="_items2">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                        <div></div>
                    }
                </TestGenericTwo>

                @code
                {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                }
                """,
            htmlFormatted: """
                @page "/counter"

                <TestGenericTwo Items="_items" ItemsTwo="_items2">
                    @foreach (var v in System.Linq.Enumerable.Range(1, 10))
                    {
                    <div></div>
                    }
                </TestGenericTwo>

                @code
                    {
                    private IEnumerable<int> _items = new[] { 1, 2, 3, 4, 5 };
                    private IEnumerable<long> _items2 = new long[] { 1, 2, 3, 4, 5 };
                }
                """,
            additionalFiles: [
                (FilePath("TestGenericTwo.razor"), """
                @using System.Collections.Generic
                @using Microsoft.AspNetCore.Components
                @typeparam TItem
                @typeparam TItemTwo
                @attribute [CascadingTypeParameter(nameof(TItem))]
                @attribute [CascadingTypeParameter(nameof(TItemTwo))]

                <h3>TestGeneric</h3>

                @code
                {
                    [Parameter] public IEnumerable<TItem> Items { get; set; }
                    [Parameter] public IEnumerable<TItemTwo> ItemsTwo { get; set; }
                    [Parameter] public RenderFragment ChildContent { get; set; }
                }
                """)]);
    }
}
