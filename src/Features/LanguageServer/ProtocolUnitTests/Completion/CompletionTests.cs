// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionTests : LanguageServerProtocolTestsBase
    {
        [Fact]
        public async Task TestGetCompletionsAsync()
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
            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" }, CreateCompletionParams(locations["caret"].First()));

            var results = (LSP.CompletionItem[])await RunGetCompletionsAsync(solution, locations["caret"].First());
            AssertCompletionItemsEqual(expected, results.First(), false);
        }

        private static async Task<object> RunGetCompletionsAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).GetCompletionsAsync(solution, CreateCompletionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);
    }
}
