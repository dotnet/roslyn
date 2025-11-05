// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Highlights;

public sealed class DocumentHighlightTests : AbstractLanguageServerProtocolTests
{
    public DocumentHighlightTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentHighlightAsync(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class B
            {
            }
            class A
            {
                B {|text:classB|};
                void M()
                {
                    var someVar = {|read:classB|};
                    {|caret:|}{|write:classB|} = new B();
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);
        var expected = new LSP.DocumentHighlight[]
        {
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Text, testLspServer.GetLocations("text").Single()),
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Read, testLspServer.GetLocations("read").Single()),
            CreateDocumentHighlight(LSP.DocumentHighlightKind.Write, testLspServer.GetLocations("write").Single())
        };

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        AssertJsonEquals(expected, results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/59120")]
    public async Task TestGetDocumentHighlightAsync_Keywords(bool lspMutatingWorkspace)
    {
        var markup =
            """
            using System.Threading.Tasks;
            class A
            {
                {|text:async|} Task MAsync()
                {
                    {|text:await|} Task.Delay(100);
                    {|caret:|}{|text:await|} Task.Delay(100);
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var expectedLocations = testLspServer.GetLocations("text");

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(LSP.DocumentHighlightKind.Text, r.Kind));
        Assert.Equal(expectedLocations[0].Range, results[0].Range);
        Assert.Equal(expectedLocations[1].Range, results[1].Range);
        Assert.Equal(expectedLocations[2].Range, results[2].Range);
    }

    [Theory, CombinatorialData]
    public async Task TestGetDocumentHighlightAsync_InvalidLocation(bool lspMutatingWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                    {|caret:|}
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());
        Assert.Empty(results);
    }

    [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/76089")]
    public async Task TestGetDocumentHighlightAsync_PartialConstructor(bool lspMutatingWorkspace)
    {
        var markup =
            """
            partial class C
            {
                partial {|caret:|}{|text:C|}();
                partial {|text:C|}()
                {
                }
            }
            """;
        await using var testLspServer = await CreateTestLspServerAsync(markup, lspMutatingWorkspace);

        var expectedLocations = testLspServer.GetLocations("text");

        var results = await RunGetDocumentHighlightAsync(testLspServer, testLspServer.GetLocations("caret").Single());

        Assert.Equal(2, results.Length);
        Assert.All(results, r => Assert.Equal(LSP.DocumentHighlightKind.Text, r.Kind));
        Assert.Equal(expectedLocations[0].Range, results[0].Range);
        Assert.Equal(expectedLocations[1].Range, results[1].Range);
    }

    private static async Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var results = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]>(LSP.Methods.TextDocumentDocumentHighlightName,
            CreateTextDocumentPositionParams(caret), CancellationToken.None);
        Array.Sort(results, (h1, h2) =>
        {
            var compareKind = h1.Kind.CompareTo(h2.Kind);
            var compareRange = CompareRange(h1.Range, h2.Range);
            return compareKind != 0 ? compareKind : compareRange;
        });

        return results;
    }

    private static LSP.DocumentHighlight CreateDocumentHighlight(LSP.DocumentHighlightKind kind, LSP.Location location)
        => new()
        {
            Kind = kind,
            Range = location.Range
        };
}
