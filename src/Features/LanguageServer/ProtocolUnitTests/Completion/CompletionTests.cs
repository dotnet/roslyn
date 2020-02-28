// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionTests : AbstractLanguageServerProtocolTests
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
            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" }, CreateCompletionParams(locations["caret"].Single()));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var results = (LSP.CompletionItem[])await RunGetCompletionsAsync(solution, locations["caret"].Single(), clientCapabilities);
            AssertJsonEquals(expected, results.First());
        }

        private static async Task<object> RunGetCompletionsAsync(Solution solution, LSP.Location caret, LSP.ClientCapabilities clientCapabilities = null)
            => await GetLanguageServer(solution).GetCompletionsAsync(solution, CreateCompletionParams(caret), clientCapabilities, CancellationToken.None);
    }
}
