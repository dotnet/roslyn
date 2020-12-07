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

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting
{
    public class FormatDocumentTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestFormatDocumentAsync()
        {
            var markup =
@"class A
{
void M()
{
            int i = 1;{|caret:|}
    }
}";
            var expected =
@"class A
{
    void M()
    {
        int i = 1;
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var documentURI = locations["caret"].Single().Uri;
            var documentText = await workspace.CurrentSolution.GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(workspace.CurrentSolution, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentAsync(Solution solution, Uri uri)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.DocumentFormattingParams, LSP.TextEdit[]>(queue, LSP.Methods.TextDocumentFormattingName,
                           CreateDocumentFormattingParams(uri), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.DocumentFormattingParams CreateDocumentFormattingParams(Uri uri)
            => new LSP.DocumentFormattingParams()
            {
                TextDocument = CreateTextDocumentIdentifier(uri),
                Options = new LSP.FormattingOptions()
                {
                    // TODO - Format should respect formatting options.
                }
            };
    }
}
