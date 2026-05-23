// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class CreateComponentFromTagTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task NotOfferedInLegacy()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <He[||]llo></Hello>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.CreateComponentFromTag,
            fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task CreateComponentFromTag()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <He[||]llo></Hello>
                """,
            expected: """
                <div></div>

                <Hello></Hello>
                """,
            codeActionName: LanguageServerConstants.CodeActions.CreateComponentFromTag,
            additionalExpectedFiles: [
                (FileUri("Hello.razor"), "")]);
    }

    [Fact]
    public async Task CreateComponentFromTag_WithNamespace()
    {
        await VerifyCodeActionAsync(
            input: """
                @namespace MyApp.Components

                <div></div>

                <He[||]llo></Hello>
                """,
            expected: """
                @namespace MyApp.Components

                <div></div>

                <Hello></Hello>
                """,
            codeActionName: LanguageServerConstants.CodeActions.CreateComponentFromTag,
            additionalExpectedFiles: [
                (FileUri("Hello.razor"), """
                @namespace MyApp.Components
                
                """)]);
    }

    [Fact]
    public async Task Attribute()
    {
        await VerifyCodeActionAsync(
            input: """
                <div></div>

                <Hello wor[||]ld="true"></Hello>
                """,
            expected: """
                <div></div>
                
                <Hello world="true"></Hello>
                """,
            codeActionName: LanguageServerConstants.CodeActions.CreateComponentFromTag,
            additionalExpectedFiles: [
                (FileUri("Hello.razor"), "")]);
    }
}
