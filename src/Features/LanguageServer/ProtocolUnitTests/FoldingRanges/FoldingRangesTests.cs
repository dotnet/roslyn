// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Imports, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollectionsEqual(expected, results, AssertFoldingRangesEqual);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support comments.")]
        public async Task TestGetFoldingRangeAsync_Comments()
        {
            var markup =
@"{|foldingRange:// A comment|}
{|foldingRange:/* A multiline
comment */|}";
            var (solution, locations) = CreateTestSolution(markup);
            var importLocation = locations["foldingRange"].First();
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Comment, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollectionsEqual(expected, results, AssertFoldingRangesEqual);
        }

        [Fact(Skip = "GetFoldingRangeAsync does not yet support regions.")]
        public async Task TestGetFoldingRangeAsync_Regions()
        {
            var markup =
@"{|foldingRange:#region ARegion
#endregion|}
}";
            var (solution, locations) = CreateTestSolution(markup);
            var importLocation = locations["foldingRange"].First();
            var expected = locations["foldingRange"]
                .Select(location => CreateFoldingRange(LSP.FoldingRangeKind.Region, location.Range))
                .ToImmutableArray();

            var results = await RunGetFoldingRangeAsync(solution);
            AssertCollectionsEqual(expected, results, AssertFoldingRangesEqual);
        }

        private static async Task<LSP.FoldingRange[]> RunGetFoldingRangeAsync(Solution solution)
        {
            var document = solution.Projects.First().Documents.First();
            var request = new LSP.FoldingRangeParams()
            {
                TextDocument = CreateTextDocumentIdentifier(new Uri(document.FilePath))
            };

            return await GetLanguageServer(solution).GetFoldingRangeAsync(solution, request, new LSP.ClientCapabilities(), CancellationToken.None);
        }

        private static void AssertFoldingRangesEqual(LSP.FoldingRange expected, LSP.FoldingRange actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.StartCharacter, actual.StartCharacter);
            Assert.Equal(expected.EndCharacter, actual.EndCharacter);
            Assert.Equal(expected.StartLine, actual.StartLine);
            Assert.Equal(expected.EndLine, actual.EndLine);
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
