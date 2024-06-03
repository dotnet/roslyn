// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.FoldingRanges
{
    public class FoldingRangesTests : AbstractLanguageServerProtocolTests
    {
        public FoldingRangesTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Imports(bool mutatingLspWorkspace)
        {
            var markup =
@"using {|foldingRange:System;
using System.Linq;|}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Imports, location.Range, "..."))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Theory(Skip = "GetFoldingRangeAsync does not yet support comments."), CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Comments(bool mutatingLspWorkspace)
        {
            var markup =
@"{|foldingRange:// A comment|}
{|foldingRange:/* A multiline
comment */|}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Comment, location.Range, ""))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Theory, CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Regions(bool mutatingLspWorkspace)
        {
            var markup =
@"{|foldingRange:#region ARegion
#endregion|}
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Region, location.Range, "ARegion"))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        private static async Task<LSP.FoldingRange[]> RunGetFoldingRangeAsync(TestLspServer testLspServer)
        {
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var request = new LSP.FoldingRangeParams()
            {
                TextDocument = CreateTextDocumentIdentifier(document.GetURI())
            };

            return await testLspServer.ExecuteRequestAsync<LSP.FoldingRangeParams, LSP.FoldingRange[]>(LSP.Methods.TextDocumentFoldingRangeName,
                request, CancellationToken.None);
        }

        private static LSP.FoldingRange CreateFoldingRange(LSP.FoldingRangeKind kind, LSP.Range range, string collapsedText)
            => new LSP.FoldingRange()
            {
                Kind = kind,
                StartCharacter = range.Start.Character,
                EndCharacter = range.End.Character,
                StartLine = range.Start.Line,
                EndLine = range.End.Line,
                CollapsedText = collapsedText
            };
    }
}
