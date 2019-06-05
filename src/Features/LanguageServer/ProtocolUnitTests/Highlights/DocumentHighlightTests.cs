// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Highlights
{
    public class DocumentHighlightTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetDocumentHighlightAsync()
        {
            var markup =
@"class B
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
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.DocumentHighlight[]
            {
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Text, locations["text"].First()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Write, locations["write"].First()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Read, locations["read"].First())
            };

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].First());
            AssertDocumentHighlightCollectionsEqual(expected, results);
        }

        [Fact]
        public async Task TestGetDocumentHighlightAsync_InvalidLocation()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            var (solution, locations) = CreateTestSolution(markup);

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].First());
            Assert.Empty(results);
        }

        private static async Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).GetDocumentHighlightAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);

        /// <summary>
        /// Assert that two highligh lists are equivalent.
        /// Highlights are not returned in a consistent order, so they must be sorted.
        /// </summary>
        private static void AssertDocumentHighlightCollectionsEqual(IEnumerable<LSP.DocumentHighlight> expectedHighlights, IEnumerable<LSP.DocumentHighlight> actualHighlights)
        {
            AssertCollectionsEqual(expectedHighlights, actualHighlights.Select(highlight => (object)highlight), AssertDocumentHighlightsEqual, CompareHighlights);

            // local functions
            static int CompareHighlights(LSP.DocumentHighlight h1, LSP.DocumentHighlight h2)
            {
                var compareKind = h1.Kind.CompareTo(h2.Kind);
                var compareRange = CompareRange(h1.Range, h2.Range);
                return compareKind != 0 ? compareKind : compareRange;
            }
        }

        private static void AssertDocumentHighlightsEqual(LSP.DocumentHighlight expected, LSP.DocumentHighlight actual)
        {
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Range, actual.Range);
        }

        private LSP.DocumentHighlight CreateDocumentHighlight(LSP.DocumentHighlightKind kind, LSP.Location location)
            => new LSP.DocumentHighlight()
            {
                Kind = kind,
                Range = location.Range
            };
    }
}
