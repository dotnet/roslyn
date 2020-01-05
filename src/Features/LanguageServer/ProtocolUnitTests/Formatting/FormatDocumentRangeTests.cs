// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Formatting
{
    public class FormatDocumentRangeTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestFormatDocumentRangeAsync()
        {
            var markup =
@"class A
{
{|format:void|} M()
{
            int i = 1;
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
            var rangeToFormat = locations["format"].Single();
            var documentText = await solution.GetDocumentFromURI(rangeToFormat.Uri).GetTextAsync();

            var results = await RunFormatDocumentRangeAsync(solution, rangeToFormat);
            var actualText = ApplyTextEdits(results, documentText);
            Assert.Equal(expected, actualText);
        }

        private static async Task<LSP.TextEdit[]> RunFormatDocumentRangeAsync(Solution solution, LSP.Location location)
            => await GetLanguageServer(solution).FormatDocumentRangeAsync(solution, CreateDocumentRangeFormattingParams(location), new LSP.ClientCapabilities(), CancellationToken.None);

        private static LSP.DocumentRangeFormattingParams CreateDocumentRangeFormattingParams(LSP.Location location)
            => new LSP.DocumentRangeFormattingParams()
            {
                Range = location.Range,
                TextDocument = CreateTextDocumentIdentifier(location.Uri),
                Options = new LSP.FormattingOptions()
                {
                    // TODO - Format should respect formatting options.
                }
            };
    }
}
