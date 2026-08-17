// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public sealed class CohostDataTipRangeEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task Handle_CSharpInHtml_DataTipRange_FirstExpression()
    {
        var input = """
            @{
                {|expression:{|hover:a$$aa|}|}.bbb.ccc;
            }
            """;

        await VerifyDataTipRangeAsync(input);
    }

    [Fact]
    public async Task Handle_CSharpInHtml_DataTipRange_SecondExpression()
    {
        var input = """
            @{
                {|expression:{|hover:aaa.b$$bb|}|}.ccc;
            }
            """;

        await VerifyDataTipRangeAsync(input);
    }

    [Fact]
    public async Task Handle_CSharpInHtml_DataTipRange_LastExpression()
    {
        var input = """
            @{
                {|expression:{|hover:aaa.bbb.c$$cc|}|};
            }
            """;

        await VerifyDataTipRangeAsync(input);
    }

    [Fact]
    public async Task Handle_CSharpInHtml_DataTipRange_LinqExpression()
    {
        var input = """
            @using System.Linq;

            @{
                int[] args;
                var v = {|expression:{|hover:args.Se$$lect|}(a => a.ToString())|}.Where(a => a.Length >= 0);
            }
            """;

        await VerifyDataTipRangeAsync(input, VSInternalDataTipTags.LinqExpression);
    }

    private async Task VerifyDataTipRangeAsync(TestCode input, VSInternalDataTipTags dataTipTags = 0)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetPosition(input.Position);

        var endpoint = new CohostDataTipRangeEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(document, position, DisposalToken);

        Assumes.NotNull(result);

        var expectedExpressionSpan = input.GetNamedSpans("expression")[0];
        var expectedExpressionRange = inputText.GetRange(expectedExpressionSpan);
        Assert.Equal(expectedExpressionRange, result.ExpressionRange);

        var expectedHoverSpan = input.GetNamedSpans("hover")[0];
        var expectedHoverRange = inputText.GetRange(expectedHoverSpan);
        Assert.Equal(expectedHoverRange, result.HoverRange);

        Assert.Equal(dataTipTags, result.DataTipTags);
    }
}
