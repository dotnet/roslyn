// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class ExtractToCodeBehindTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NotOfferedInLegacy()
    {
        await VerifyCodeActionAsync(
            input: """
                @co[||]de
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task OutsideCodeDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                <div$$></div>

                @code
                {
                    private int x = 1;
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind);
    }

    [Fact]
    public async Task NotInEmptyCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                @code {$$}
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind);
    }

    [Fact]
    public async Task NotInEmptyMalformedCodeBlock()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                @$$code
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind);
    }

    [Fact]
    public async Task NotWithMarkup()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>
                @$$code {
                    void Test()
                    {
                        <h1>Hello, world!</h1>
                    }
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind);
    }

    [Fact]
    public async Task NotWithoutFileCreation()
    {
        UpdateClientInitializationOptions(c =>
        {
            c.SupportsFileManipulation = false;
            return c;
        });

        await VerifyCodeActionAsync(
            input: """
                <div></div>
                @$$code {
                    void Test()
                    {
                    }
                }
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind);
    }

    [Fact]
    public async Task ExtractToCodeBehind()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                @co[||]de
                {
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>


                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), $$"""
                    namespace SomeProject
                    {
                        public partial class File1
                        {
                            private int x = 1;
                        }
                    }
                    """)]);
    }

    [Theory]
    [InlineData("[||]@code {")]
    [InlineData("@[||]code {")]
    [InlineData("@c[||]ode {")]
    [InlineData("@co[||]de {")]
    [InlineData("@cod[||]e {")]
    [InlineData("@code[||] {")]
    [InlineData("[||]@code\n{")]
    [InlineData("@[||]code\n{")]
    [InlineData("@c[||]ode\n{")]
    [InlineData("@co[||]de\n{")]
    [InlineData("@cod[||]e\n{")]
    [InlineData("@code[||]\n{")]
    [InlineData("@code[||]{")]
    public async Task WorkAtAnyCursorPosition(string codeBlockStart)
    {
        await VerifyCodeActionAsync(
            input: $$"""
                <div></div>

                {{codeBlockStart}}
                    private int x = 1;
                }
                """,
            expected: """
                <div></div>


                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), $$"""
                    namespace SomeProject
                    {
                        public partial class File1
                        {
                            private int x = 1;
                        }
                    }
                    """)]);
    }

    [Fact]
    public async Task ExtractToCodeBehind_WithUsing()
    {
        await VerifyCodeActionAsync(
            input: """
                @using System.Diagnostics

                <div></div>

                @co[||]de
                {
                    private int x = 1;

                    private void M()
                    {
                        Debug.WriteLine("");
                    }
                }
                """,
            expected: """
                @using System.Diagnostics
                
                <div></div>


                """,
            codeActionName: LanguageServerConstants.CodeActions.ExtractToCodeBehind,
            additionalExpectedFiles: [
                (FileUri("File1.razor.cs"), $$"""
                    using System.Diagnostics;

                    namespace SomeProject
                    {
                        public partial class File1
                        {
                            private int x = 1;

                            private void M()
                            {
                                Debug.WriteLine("");
                            }
                        }
                    }
                    """)]);
    }
}
