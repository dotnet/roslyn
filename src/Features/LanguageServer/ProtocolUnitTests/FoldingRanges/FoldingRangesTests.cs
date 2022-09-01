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
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.FoldingRanges
{
    public class FoldingRangesTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetFoldingRangeAsync_Imports()
        {
            var markup =
@"using {|foldingRange:System;
using System.Linq;|}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Imports, location.Range))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support comments.")]
        public async Task TestGetFoldingRangeAsync_Comments()
        {
            var markup =
@"{|foldingRange:// A comment|}
{|foldingRange:/* A multiline
comment */|}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Comment, location.Range))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support regions.")]
        public async Task TestGetFoldingRangeAsync_Regions()
        {
            var markup =
@"{|foldingRange:#region ARegion
#endregion|}
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expected = testLspServer.GetLocations("foldingRange")
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Region, location.Range))
                .ToArray();

            var results = await RunGetFoldingRangeAsync(testLspServer);
            AssertJsonEquals(expected, results);
        }

        private static async Task<LSP.FoldingRange[]> RunGetFoldingRangeAsync(TestLspServer testLspServer)
        {
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var request = new LSP.FoldingRangeParams()
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            return await testLspServer.ExecuteRequestAsync<LSP.FoldingRangeParams, LSP.FoldingRange[]>(LSP.Methods.TextDocumentFoldingRangeName,
                request, CancellationToken.None);
        }

        private static LSP.FoldingRange CreateFoldingRange(LSP.FoldingRangeKind kind, LSP.Range range)
            => new LSP.FoldingRange()
            {
                Kind = kind,
                StartCharacter = range.Start.Character,
                EndCharacter = range.End.Character,
                StartLine = range.Start.Line,
                EndLine = range.End.Line
            };
    }
}
