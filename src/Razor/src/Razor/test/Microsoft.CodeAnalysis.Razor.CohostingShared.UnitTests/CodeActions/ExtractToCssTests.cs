// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class ExtractToCssTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task ExtractToCss()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCss_NotWithEmptyTag()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss);
    }

    [Fact]
    public async Task ExtractToCss_NotWithWhitespaceOnlyTag()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>


                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss);
    }

    [Fact]
    public async Task ExtractToCss_NotWithCSharp()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: @red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss);
    }

    [Fact]
    public async Task ExtractToCss_ExistingFile()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalFiles: [
                (FilePath("File1.razor.css"), $$$"""
                    h1 {
                            color: blue;
                        }
                    """)],
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    h1 {
                            color: blue;
                        }

                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCss_ExistingFile_LastLineEmpty()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalFiles: [
                (FilePath("File1.razor.css"), $$$"""
                    h1 {
                        color: blue;
                    }

                    """)],
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    h1 {
                        color: blue;
                    }


                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCss_ExistingFile_Empty()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalFiles: [
                (FilePath("File1.razor.css"), "")],
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCss_ExistingFile_OneNonEmptyLine()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <sty[||]le>
                    body {
                        background-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalFiles: [
                (FilePath("File1.razor.css"), "h1 { color: red }")],
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    h1 { color: red }

                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Theory]
    [InlineData("[||]<style>", "</style>")]
    [InlineData("<[||]style>", "</style>")]
    [InlineData("<s[||]tyle>", "</style>")]
    [InlineData("<st[||]yle>", "</style>")]
    [InlineData("<sty[||]le>", "</style>")]
    [InlineData("<styl[||]e>", "</style>")]
    [InlineData("<style[||]>", "</style>")]
    [InlineData("<style>[||]", "</style>")]
    [InlineData("<style>", "[||]</style>")]
    [InlineData("<style>", "<[||]/style>")]
    [InlineData("<style>", "</[||]style>")]
    [InlineData("<style>", "</s[||]tyle>")]
    [InlineData("<style>", "</st[||]yle>")]
    [InlineData("<style>", "</sty[||]le>")]
    [InlineData("<style>", "</styl[||]e>")]
    [InlineData("<style>", "</style[||]>")]
    [InlineData("<style>", "</style>[||]")]
    public async Task WorkAtAnyCursorPosition(string startTag, string endTag)
    {
        await VerifyCodeActionAsync(
            input: $$"""
                <div></div>
                
                {{startTag}}
                    body {
                        background-color: red;
                    }
                {{endTag}}
                
                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$"""
                    body {
                            background-color: red;
                        }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCss_FromInside()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <style>
                    body {
                        back[||]ground-color: red;
                    }
                </style>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>



                @code
                {
                    private int x = 1;
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCss,
            additionalExpectedFiles: [
                (FileUri("File1.razor.css"), $$$"""
                    body {
                            background-color: red;
                        }
                    """)]);
    }
}
