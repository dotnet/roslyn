// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.RazorExtension.Test.Endpoints.Formatting;

/// <summary>
/// Tests that explore quirks of the VS Code html formatter, distinct from the VS variety
/// </summary>
public class VSCodeFormattingTest(ITestOutputHelper testOutputHelper) : DocumentFormattingTestBase(testOutputHelper)
{
    [Fact]
    public async Task HtmlFormatterWrappingInVSCode()
    {
        await RunFormattingTestAsync(
            input: """
                @{
                    var str = "This is a long string that will be wrapped by the Html formatter";
                }
                """,
            htmlFormatted: """
                @{
                    var str = "This is a long string that will
                    be wrapped by the Html formatter";
                }
                """,
            expected: """
                @{
                    var str = "This is a long string that will be wrapped by the Html formatter";
                }
                """);
    }

    [Fact]
    public Task HtmlAttributes_FirstAttributeOnNextLine_NotFixedByHtmlFormatter()
        => RunFormattingTestAsync(
            input: """
                <div
                    class="foo"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </div>
                """,
            htmlFormatted: """
                <div
                    class="foo"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </div>
                """,
            expected: """
                <div
                    class="foo"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </div>
                """);

    [Fact]
    public Task HtmlAttributes_FirstAttributeOnNextLine_UnWrappedByHtmlFormatter()
        => RunFormattingTestAsync(
            input: """
                <div
                    class="foo"
                    disabled
                    style="hello"
                    @onclick="foo()">
                </div>
                """,
            htmlFormatted: """
                <div class="foo" disabled
                    style="hello" @onclick="foo()">
                </div>
                """,
            expected: """
                <div class="foo" disabled
                     style="hello" @onclick="foo()">
                </div>
                """);
}
