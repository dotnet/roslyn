// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class PreviewCodeActionsTests : LanguageServerProtocolTestsBase
    {
        [Fact]
        public async Task TestPreviewCodeActionsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}{|edit:int|} i = 1;
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = CreateTextEdit("var", locations["edit"].First().Range);

            var results = await RunPreviewCodeActionsAsync(solution, locations["caret"].First(), "Use implicit type");
            AssertCollectionsEqual(new LSP.TextEdit[] { expected }, results, AssertTextEditsEqual);
        }

        private static async Task<LSP.TextEdit[]> RunPreviewCodeActionsAsync(Solution solution, LSP.Location caret, string title)
            => await GetLanguageServer(solution).PreviewCodeActionsAsync(solution, CreateRunCodeActionParams(caret, title), new LSP.ClientCapabilities(), CancellationToken.None);

        private static LSP.TextEdit CreateTextEdit(string text, LSP.Range range)
            => new LSP.TextEdit()
            {
                NewText = text,
                Range = range
            };
    }
}
