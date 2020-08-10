// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
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
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" }, CreateCompletionParams(locations["caret"].Single()));

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, locations["caret"].Single()).ConfigureAwait(false);
            AssertJsonEquals(expected, results.First());
        }

        [Fact]
        public async Task TestGetCompletionsDoesNotIncludeUnimportedTypesAsync()
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
            var solution = workspace.CurrentSolution;

            // Make sure the unimported types option is on by default.
            solution = solution.WithOptions(solution.Options
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, true));

            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" }, CreateCompletionParams(locations["caret"].Single()));

            var results = await RunGetCompletionsAsync(solution, locations["caret"].Single());

            Assert.False(results.Any(item => "Console" == item.Label));
        }

        [Fact]
        public async Task TestGetCompletionsDoesNotIncludeSnippetsAsync()
        {
            var markup =
@"class A
{
    {|caret:|}
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var solution = workspace.CurrentSolution;
            solution = solution.WithOptions(solution.Options
                .WithChangedOption(CompletionOptions.SnippetsBehavior, LanguageNames.CSharp, SnippetsRule.AlwaysInclude));

            var results = await RunGetCompletionsAsync(solution, locations["caret"].Single());

            Assert.False(results.Any(item => "ctor" == item.Label));
        }

        [Fact]
        public async Task TestGetCompletionsWithPreselectAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        A classA = new {|caret:|}
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" }, CreateCompletionParams(locations["caret"].Single()), preselect: true);

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, locations["caret"].Single()).ConfigureAwait(false);
            AssertJsonEquals(expected, results.First());
        }

        private static async Task<LSP.CompletionItem[]> RunGetCompletionsAsync(Solution solution, LSP.Location caret)
        {
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionItem[]>(LSP.Methods.TextDocumentCompletionName,
                           CreateCompletionParams(caret), clientCapabilities, null, CancellationToken.None);
        }
    }
}
