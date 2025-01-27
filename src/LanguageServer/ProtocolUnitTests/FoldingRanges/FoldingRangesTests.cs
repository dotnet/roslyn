// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Structure;
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
                """
                using {|imports:System;
                using System.Linq;|}
                """;
            await AssertFoldingRanges(mutatingLspWorkspace, markup);
        }

        [Theory(Skip = "GetFoldingRangeAsync does not yet support comments."), CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Comments(bool mutatingLspWorkspace)
        {
            var markup =
                """
                {|foldingRange:// A comment|}
                {|foldingRange:/* A multiline
                comment */|}
                """;
            await AssertFoldingRanges(mutatingLspWorkspace, markup);
        }

        [Theory, CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Regions(bool mutatingLspWorkspace)
        {
            var markup =
                """
                {|region:#region ARegion
                #endregion|}
                """;
            await AssertFoldingRanges(mutatingLspWorkspace, markup, "ARegion");
        }

        [Theory, CombinatorialData]
        public async Task TestGetFoldingRangeAsync_Members(bool mutatingLspWorkspace)
        {
            var markup =
                """
                class C{|foldingRange:
                {
                    public void M(){|implementation:
                    {
                    }|}
                }|}
                """;

            await AssertFoldingRanges(mutatingLspWorkspace, markup);
        }

        private async Task AssertFoldingRanges(bool mutatingLspWorkspace, string markup, string collapsedText = null)
        {
            var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var expected = testLspServer.GetLocations()
                .SelectMany(kvp => kvp.Value.Select(location => CreateFoldingRange(kvp.Key, location.Range, collapsedText ?? "...")))
                .OrderByDescending(range => range.StartLine)
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

        private static LSP.FoldingRange CreateFoldingRange(string kind, LSP.Range range, string collapsedText)
            => new LSP.FoldingRange()
            {
                Kind = kind switch
                {
                    "foldingRange" => null,
                    null => null,
                    _ => new(kind)
                },
                StartCharacter = range.Start.Character,
                EndCharacter = range.End.Character,
                StartLine = range.Start.Line,
                EndLine = range.End.Line,
                CollapsedText = collapsedText
            };
    }
}
