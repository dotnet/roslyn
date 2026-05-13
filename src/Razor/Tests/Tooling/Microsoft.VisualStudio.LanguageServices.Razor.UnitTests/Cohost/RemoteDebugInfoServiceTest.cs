// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class RemoteDebugInfoServiceTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task ResolveProximityExpressionsAsync_Html()
    {
        var input = """
                $$<div></div>

                @{
                    var currentCount = 1;
                }

                <p>@currentCount</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_OnlyHtml()
    {
        var input = """
                $$<div></div>
                """;

        await VerifyProximityExpressionsAsync(input, []);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_ExplicitExpression()
    {
        var input = """
                <div></div>

                @{
                    var currentC$$ount = 1;
                }

                <p>@[|currentCount|]</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_OutsideImplicitExpression()
    {
        var input = """
                <div></div>

                @{
                    var [|currentCount|] = 1;
                }

                $$<p>@currentCount</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_OutsideExplicitStatement()
    {
        var input = """
                <div></div>

                $$<p>@{var [|abc|] = 123;}</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_InsideExplicitStatement()
    {
        var input = """
                <div></div>

                <p>@{var$$ [|abc|] = 123;}</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_ImplicitExpression()
    {
        var input = """
                <div></div>

                @{
                    var [|currentCount|] = 1;
                }

                <p>@curr$$entCount</p>
                """;

        await VerifyProximityExpressionsAsync(input, ["__builder", "this"]);
    }

    [Fact]
    public async Task ResolveProximityExpressionsAsync_CodeBlock()
    {
        var input = """
                <div></div>

                <p>@currentCount</p>

                @code
                {
                    private int [|currentCount|];
                    private bool hasBeenClicked;

                    private void M()
                    {
                        current$$Count++;
                    }
                }
                """;

        await VerifyProximityExpressionsAsync(input, ["this"]);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_Html()
    {
        var input = """
                $$<div></div>

                @{
                    var currentCount = 1;
                }

                <p>@currentCount</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_CodeBlock()
    {
        var input = """
                <div></div>

                <p>@currentCount</p>

                @code
                {
                    private int currentCount;

                    private void M()
                    {
                        [|current$$Count++;|]
                    }
                }
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_CodeBlock_InvalidLocation()
    {
        var input = """
                <div></div>

                <p>@currentCount</p>

                @code
                {
                    private bool hasBeen$$Clicked;
                }
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_OutsideImplicitExpression()
    {
        var input = """
                <div></div>

                @{
                    var currentCount = 1;
                }

                $$<p>@[|currentCount|]</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_OutsideExplicitStatement()
    {
        var input = """
                <div></div>

                $$<p>@{ [|var abc = 123;|] }</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_OutsideExplicitStatement_NoCSharpOnLine()
    {
        var input = """
                <div></div>

                $$<p>@{
                    var abc = 123;
                }</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_InsideExplicitStatement_NoCSharpOnLine()
    {
        var input = """
                <div></div>

                <p>@{
                $$
                    var abc = 123;
                }</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_OutsideExplicitStatement_InvalidLocation()
    {
        var input = """
                <div></div>

                $$<p>@{ var abc; }</p>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentStartTag()
    {
        var input = """
                <div></div>

                <Page$$Title>Hello</PageTitle>

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentAttribute()
    {
        var input = """
                <div></div>

                @{
                    var caption = "Hello";
                }

                <InputText Val$$ue="@caption" />

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentContent()
    {
        var input = """
                <div></div>

                @{
                    var caption = "Hello";
                }

                <PageTitle>@[|cap$$tion|]</PageTitle>

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentContent_FromStartOfLine()
    {
        var input = """
                <div></div>

                @{
                    var caption = "Hello";
                }

                $$<PageTitle>@[|caption|]</PageTitle>

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentContent_FromStartOfLine_WithAttribute()
    {
        var input = """
                <div></div>

                @{
                    var caption = "Hello";
                }

                $$<InputText Value="@caption" />@[|caption|]

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_ComponentContent_FromStartTag()
    {
        var input = """
                <div></div>

                @{
                    var caption = "Hello";
                }

                <PageT$$itle>@[|caption|]</PageTitle>

                """;

        await VerifyBreakpointRangeAsync(input);
    }

    [Theory]
    [CombinatorialData]
    public async Task ResolveBreakpointRangeAsync_CodeBlockInMiddleOfDocument(bool legacy)
    {
        var blockKind = legacy ? "functions" : "code";
        var fileKind = legacy ? RazorFileKind.Legacy : RazorFileKind.Component;

        var input = $$"""
                @{
                    ViewData["Title"] = "Home Page";
                }

                @{{blockKind}} {
                    string GetTimeStamp()
                    {
                $$        [|return DateTime.Now.ToString("F");|]
                    }
                }

                <div class="text-center">
                    <h1 class="display-4">Welcome</h1>
                    <p>Learn about <a href="https://learn.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
                    @{
                        string timeStamp = GetTimeStamp();
                        <span>Current time: @timeStamp</span>
                    }
                </div>
                """;

        await VerifyBreakpointRangeAsync(input, fileKind: fileKind);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_UsingDirectiveInLine1()
    {
        var input = $$"""
                @{
                    string x = "";
                }

                $$<div class="@([|x = nameof(Object)|])" accesskey="A" @using System.IO data="please don't do this">
                    <h1 class="display-4">Welcome</h1>
                </div>
                """;

        // This is only an issue for Legacy files. For Components, the using directive is treated as plain text
        await VerifyBreakpointRangeAsync(input, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_UsingDirectiveInLine2()
    {
        var input = $$"""
                @{
                    string x = "";
                }

                $$<div class="@[|x|]" accesskey="A" @using System.Text.RegularExpressions data="please don't do this">
                    <h1 class="display-4">Welcome</h1>
                </div>
                """;

        await VerifyBreakpointRangeAsync(input, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_UsingDirectiveInLine3()
    {
        var input = $$"""
                @{
                    ViewData["Title"] = "Home Page";

                    string x = "";
                }

                $$<div>@[|x|]</div> hello @using System.Text.Encodings world
                """;

        await VerifyBreakpointRangeAsync(input, fileKind: RazorFileKind.Legacy);
    }

    [Fact]
    public async Task ResolveBreakpointRangeAsync_MultipleCSharpExpressions()
    {
        var input = $$"""
                @{
                    string x = "";
                }

                <div accesskey="@x" $$ class="@[|x|]">
                    <h1 class="display-4">Welcome</h1>
                </div>
                """;

        await VerifyBreakpointRangeAsync(input);
    }

    private async Task VerifyProximityExpressionsAsync(TestCode input, string[] extraExpressions)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);

        var span = inputText.GetLinePosition(input.Position);

        var result = await RemoteServiceInvoker
            .TryInvokeAsync<IRemoteDebugInfoService, string[]?>(
                document.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.ResolveProximityExpressionsAsync(solutionInfo, document.Id, span, cancellationToken),
                DisposalToken);

        if (!input.HasSpans)
        {
            Assert.Null(result);
            return;
        }

        Assert.NotNull(result);

        var expected = input.Spans.Select(inputText.ToString).Concat(extraExpressions).OrderAsArray();
        AssertEx.SequenceEqual(expected, result.OrderAsArray());
    }

    private async Task VerifyBreakpointRangeAsync(TestCode input, RazorFileKind? fileKind = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text, fileKind: fileKind);
        var inputText = await document.GetTextAsync(DisposalToken);

        var span = inputText.GetLinePosition(input.Position);

        var result = await RemoteServiceInvoker
            .TryInvokeAsync<IRemoteDebugInfoService, LinePositionSpan?>(
                document.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.ResolveBreakpointRangeAsync(solutionInfo, document.Id, span, cancellationToken),
                DisposalToken);

        if (result is not { } breakpoint)
        {
            Assert.False(input.HasSpans);
            return;
        }

        var expected = inputText.GetLinePositionSpan(input.Span);
        Assert.Equal(expected, breakpoint);
    }
}
