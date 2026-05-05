// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class PromoteUsingDirectiveTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task PromoteUsingDirective()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("..", "_Imports.razor")), """
                    @using System
                    """)]);
    }

    [Fact]
    public async Task Indented()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    @using [||]System
                </div>

                <div>
                    Hello World
                </div>
                """,
            expected: """
                <div>
                </div>

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("..", "_Imports.razor")), """
                    @using System
                    """)]);
    }

    [Fact]
    public async Task Mvc()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>
                    Hello World
                </div>
                """,
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            fileKind: RazorFileKind.Legacy,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("..", "_ViewImports.cshtml")), """
                    @using System
                    """)]);
    }

    [Fact]
    public async Task ExistingImports()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>
                    Hello World
                </div>
                """,
            documentFilePath: FilePath(Path.Combine("My", "Deeply", "Nested", "File.razor")),
            additionalFiles: [
                (FilePath(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                    """)],
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                    @using System
                    """)]);
    }

    [Fact]
    public async Task ExistingImports_BlankLineAtEnd()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>
                    Hello World
                </div>
                """,
            documentFilePath: FilePath(Path.Combine("My", "Deeply", "Nested", "File.razor")),
            additionalFiles: [
                (FilePath(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                    
                    """)],
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                    @using System
                    """)]);
    }

    [Fact]
    public async Task ExistingImports_WhitespaceLineAtEnd()
    {
        await VerifyCodeActionAsync(
            input: """
                @using [||]System

                <div>
                    Hello World
                </div>
                """,
            documentFilePath: FilePath(Path.Combine("My", "Deeply", "Nested", "File.razor")),
            additionalFiles: [
                (FilePath(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                        
                    """)],
            expected: """

                <div>
                    Hello World
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.PromoteUsingDirective,
            additionalExpectedFiles: [
                (FileUri(Path.Combine("My", "Deeply", "_Imports.razor")), """
                    @using System.Text
                    @using Foo.Bar
                    @using System    
                    """)]);
    }
}
