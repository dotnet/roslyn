// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost.CodeActions;

public class WrapAttributeTests(ITestOutputHelper testOutputHelper) : CohostCodeActionsEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task WrapAttributes()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    <div [||]bar="Baz" Zip="Zap" checked @onclick="foo" Pop="Pap">
                        <div></div>
                    </div>
                </div>
                """,
            expected: """
                <div>
                    <div bar="Baz"
                         Zip="Zap"
                         checked
                         @onclick="foo"
                         Pop="Pap">
                        <div></div>
                    </div>
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }

    [Fact]
    public async Task Component()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    <EditForm [||]bar="Baz" Zip="Zap" checked @onclick="foo" Pop="Pap" />
                </div>
                """,
            expected: """
                <div>
                    <EditForm bar="Baz"
                              Zip="Zap"
                              checked
                              @onclick="foo"
                              Pop="Pap" />
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }

    [Fact]
    public async Task Whitespace()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    <Foo Bar="Baz"        Zip="Za[||]p"               Pop="Pap" />
                </div>
                """,
            expected: """
                <div>
                    <Foo Bar="Baz"
                         Zip="Zap"
                         Pop="Pap" />
                </div>
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }

    [Fact]
    public async Task MultiLine()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    <Foo Bar="Baz" Zip="Za[||]p"
                         Pop="Pap" />
                </div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }

    [Fact]
    public async Task OneAttribute()
    {
        await VerifyCodeActionAsync(
            input: """
                <div>
                    <Foo Zip="Za[||]p" />
                </div>
                """,
            expected: null,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }

    [Fact]
    public async Task SelfClosing()
    {
        await VerifyCodeActionAsync(
            input: """
                @if (true)
                {
                    <div>
                        <in[||]put bar="Baz" Zip="Zap" checked @onclick="foo" Pop="Pap" />
                    </div>
                }
                """,
            expected: """
                @if (true)
                {
                    <div>
                        <input bar="Baz"
                               Zip="Zap"
                               checked
                               @onclick="foo"
                               Pop="Pap" />
                    </div>
                }
                """,
            codeActionName: LanguageServerConstants.CodeActions.WrapAttributes);
    }
}
