// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.DocumentHighlight[]
            {
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Text, locations["text"].Single()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Read, locations["read"].Single()),
                CreateDocumentHighlight(LSP.DocumentHighlightKind.Write, locations["write"].Single())
            };

            var results = await RunGetDocumentHighlightAsync(workspace.CurrentSolution, locations["caret"].Single());
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
            using var workspace = CreateTestWorkspace(markup, out var locations);

            var results = await RunGetDocumentHighlightAsync(workspace.CurrentSolution, locations["caret"].Single());
            Assert.Empty(results);
        }

        private static async Task<LSP.DocumentHighlight[]> RunGetDocumentHighlightAsync(Solution solution, LSP.Location caret)
        {
            var results = await GetLanguageServer(solution).ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.DocumentHighlight[]>(LSP.Methods.TextDocumentDocumentHighlightName,
                CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
            Array.Sort(results, (h1, h2) =>
            {
                var compareKind = h1.Kind.CompareTo(h2.Kind);
                var compareRange = CompareRange(h1.Range, h2.Range);
                return compareKind != 0 ? compareKind : compareRange;
            });

            return results;
        }

        private static LSP.DocumentHighlight CreateDocumentHighlight(LSP.DocumentHighlightKind kind, LSP.Location location)
            => new LSP.DocumentHighlight()
            {
                Kind = kind,
                Range = location.Range
            };
    }
}
