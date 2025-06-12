// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.DataTips;

public sealed class DataTipRangeHandlerTests(ITestOutputHelper testOutputHelper)
    : AbstractLanguageServerProtocolTests(testOutputHelper)
{
    private static async Task<LSP.VSInternalDataTip?> RunAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.VSInternalDataTip?>(
            LSP.VSInternalMethods.TextDocumentDataTipRangeName,
            new LSP.TextDocumentPositionParams()
            {
                TextDocument = new LSP.TextDocumentIdentifier { DocumentUri = caret.DocumentUri },
                Position = caret.Range.Start,
            },
            CancellationToken.None);
    }

    [Theory, CombinatorialData]
    public async Task SimpleStatement(bool mutatingLspWorkspace)
    {
        var markup = """
            using System.Linq;

            int[] args;
            var v = args.{|caret:|}Select(a => a.ToString()).Where(a => a.Length >= 0);
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

        var caret = testLspServer.GetLocations("caret").Single();

        var result = await RunAsync(testLspServer, caret);
        Assert.NotNull(result);

        Assert.Equal(new LSP.VSInternalDataTip
        {
            DataTipTags = LSP.VSInternalDataTipTags.LinqExpression,
            HoverRange = new LSP.Range
            {
                Start = new LSP.Position { Line = 3, Character = 8 },
                End = new LSP.Position { Line = 3, Character = 19 },
            },
            ExpressionRange = new LSP.Range
            {
                Start = new LSP.Position { Line = 3, Character = 8 },
                End = new LSP.Position { Line = 3, Character = 38 },
            },
        }, result);
    }
}
