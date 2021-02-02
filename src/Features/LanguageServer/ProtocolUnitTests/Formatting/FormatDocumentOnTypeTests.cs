﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting
{
    public class FormatDocumentOnTypeTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestFormatDocumentOnTypeAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        if (true)
            {{|type:|}
    }
}";
            var expected =
@"class A
{
    void M()
    {
        if (true)
        {
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var characterTyped = ";";
            var locationTyped = locations["type"].Single();
            var documentText = await testLspServer.GetCurrentSolution().GetDocuments(locationTyped.Uri).Single().GetTextAsync();

            var results = await RunFormatDocumentOnTypeAsync(testLspServer, characterTyped, locationTyped);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentOnTypeAsync(TestLspServer testLspServer, string characterTyped, LSP.Location locationTyped)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.DocumentOnTypeFormattingParams, LSP.TextEdit[]>(LSP.Methods.TextDocumentOnTypeFormattingName,
                           CreateDocumentOnTypeFormattingParams(characterTyped, locationTyped), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.DocumentOnTypeFormattingParams CreateDocumentOnTypeFormattingParams(string characterTyped, LSP.Location locationTyped)
            => new LSP.DocumentOnTypeFormattingParams()
            {
                Position = locationTyped.Range.Start,
                Character = characterTyped,
                TextDocument = CreateTextDocumentIdentifier(locationTyped.Uri),
                Options = new LSP.FormattingOptions()
                {
                    // TODO - Format should respect formatting options.
                }
            };
    }
}
