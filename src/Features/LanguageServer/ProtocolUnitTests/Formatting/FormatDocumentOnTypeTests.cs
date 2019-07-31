// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var (solution, locations) = CreateTestSolution(markup);
            var characterTyped = ";";
            var locationTyped = locations["type"].Single();
            var documentText = await solution.GetDocumentFromURI(locationTyped.Uri).GetTextAsync();

            var results = await RunFormatDocumentOnTypeAsync(solution, characterTyped, locationTyped);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentOnTypeAsync(Solution solution, string characterTyped, LSP.Location locationTyped)
            => await GetLanguageServer(solution)
            .FormatDocumentOnTypeAsync(solution, CreateDocumentOnTypeFormattingParams(characterTyped, locationTyped), new LSP.ClientCapabilities(), CancellationToken.None);

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
