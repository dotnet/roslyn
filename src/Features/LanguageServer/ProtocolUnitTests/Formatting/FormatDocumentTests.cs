﻿// Licensed to the .NET Foundation under one or more agreements.
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var documentURI = locations["caret"].Single().Uri;
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(documentURI).Single().GetTextAsync();

            var results = await RunFormatDocumentAsync(testLspServer, documentURI);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentAsync(TestLspServer testLspServer, Uri uri)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.DocumentFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentFormattingName,
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
