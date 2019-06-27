// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Text;
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
            var (solution, locations) = CreateTestSolution(markup);
            var documentURI = locations["caret"].Single().Uri;
            var documentText = await solution.GetDocumentFromURI(documentURI).GetTextAsync();

            var results = await RunFormatDocumentAsync(solution, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentAsync(Solution solution, Uri uri)
            => await GetLanguageServer(solution).FormatDocumentAsync(solution, CreateDocumentFormattingParams(uri), new LSP.ClientCapabilities(), CancellationToken.None);

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
