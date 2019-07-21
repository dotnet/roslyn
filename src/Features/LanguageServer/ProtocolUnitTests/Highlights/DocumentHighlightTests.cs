// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Text, locations["text"].Single()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Read, locations["read"].Single()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Write, locations["write"].Single())
            };

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].Single());
            AssertJsonEquals(expected, results);
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

            var results = await RunGetDocumentHighlightAsync(solution, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(Solution solution, LSP.Location caret)
        {
            var results = await GetLanguageServer(solution).GetDocumentHighlightAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);
            Array.Sort(results, (h1, h2) =>
            {
                var compareKind = h1.Kind.CompareTo(h2.Kind);
                var compareRange = CompareRange(h1.Range, h2.Range);
                return compareKind != 0 ? compareKind : compareRange;
            });

            return results;
        }

        private LSP.DocumentHighlight CreateDocumentHighlight(LSP.DocumentHighlightKind kind, LSP.Location location)
            => new LSP.DocumentHighlight()
            {
                Kind = kind,
                Range = location.Range
            };
    }
}
