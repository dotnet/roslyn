// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
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
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "\0", triggerKind: LSP.CompletionTriggerKind.Invoked);

            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" },
                completionParams, commitCharacters: CompletionRules.Default.DefaultCommitCharacters);

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
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

            var completionParams = CreateCompletionParams(locations["caret"].Single(), triggerCharacter: "\0", LSP.CompletionTriggerKind.Invoked);
            var results = await RunGetCompletionsAsync(solution, completionParams);

            Assert.False(results.Items.Any(item => "Console" == item.Label));
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

            var completionParams = CreateCompletionParams(locations["caret"].Single(), "\0", LSP.CompletionTriggerKind.Invoked);
            var results = await RunGetCompletionsAsync(solution, completionParams);

            Assert.False(results.Items.Any(item => "ctor" == item.Label));
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
            var completionParams = CreateCompletionParams(locations["caret"].Single(), triggerCharacter: "\0", LSP.CompletionTriggerKind.Invoked);
            var expected = CreateCompletionItem("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" },
                completionParams, preselect: true, commitCharacters: ImmutableArray.Create(' ', '(', '[', '{', ';', '.'));

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        public async Task TestGetCompletionsIsInSuggestionMode()
        {
            var markup =
@"
using System.Collections.Generic;
using System.Linq; 
namespace M
{
    class Item
    {
        void M()
        {
            var items = new List<Item>();
            items.Count(i{|caret:|}
        }
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "i", triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = (LSP.VSCompletionList)await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            Assert.True(results.Items.Any());
            Assert.True(results.SuggestionMode);
        }

        [Fact]
        public async Task TestGetDateAndTimeCompletionsAsync()
        {
            var markup =
@"using System;
class A
{
    void M()
    {
        DateTime.Now.ToString(""{|caret:|});
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "\"", triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);
            var expected = CreateCompletionItem("d", LSP.CompletionItemKind.Text, new string[] { "Text" }, completionParams, sortText: "0000");

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        public async Task TestGetRegexCompletionsAsync()
        {
            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        new Regex(""\\{|caret:|}"");
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "\\", triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);
            var expected = CreateCompletionItem("\\A", LSP.CompletionItemKind.Text, new string[] { "Text" }, completionParams, sortText: "0000");

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        [WorkItem(47743, "https://github.com/dotnet/roslyn/issues/47743")]
        public async Task TestGetRegexCompletionsTargetTypedAsync()
        {
            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        Regex r = new(""\\{|caret:|}"");
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "\\", triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);
            var expected = CreateCompletionItem("\\A", LSP.CompletionItemKind.Text, new string[] { "Text" }, completionParams, sortText: "0000");

            var results = await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        [WorkItem(46694, "https://github.com/dotnet/roslyn/issues/46694")]
        public async Task TestCompletionListCacheAsync()
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
            var cache = GetCompletionListCache(workspace);
            Assert.NotNull(cache);

            var testAccessor = cache.GetTestAccessor();

            // This test assumes that the maximum cache size is 3, and will have to modified if this number changes.
            Assert.True(CompletionListCache.TestAccessor.MaximumCacheSize == 3);

            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), triggerCharacter: "\0", triggerKind: LSP.CompletionTriggerKind.Invoked);

            // 1 item in cache
            await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            var completionList = await cache.GetCachedCompletionListAsync(0, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 1);

            // 2 items in cache
            await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            completionList = await cache.GetCachedCompletionListAsync(0, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            completionList = await cache.GetCachedCompletionListAsync(1, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 2);

            // 3 items in cache
            await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            completionList = await cache.GetCachedCompletionListAsync(0, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            completionList = await cache.GetCachedCompletionListAsync(1, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            completionList = await cache.GetCachedCompletionListAsync(2, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 3);

            // Maximum size of cache (3) should not be exceeded - oldest item should be ejected
            await RunGetCompletionsAsync(workspace.CurrentSolution, completionParams).ConfigureAwait(false);
            completionList = await cache.GetCachedCompletionListAsync(0, CancellationToken.None).ConfigureAwait(false);
            Assert.Null(completionList);
            completionList = await cache.GetCachedCompletionListAsync(1, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            completionList = await cache.GetCachedCompletionListAsync(2, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            completionList = await cache.GetCachedCompletionListAsync(3, CancellationToken.None).ConfigureAwait(false);
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 3);
        }

        private static async Task<LSP.CompletionList> RunGetCompletionsAsync(Solution solution, LSP.CompletionParams completionParams)
        {
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(queue, LSP.Methods.TextDocumentCompletionName,
                completionParams, clientCapabilities, null, CancellationToken.None);
        }

        private static CompletionListCache GetCompletionListCache(Workspace workspace)
        {
            var exportProvider = ((TestWorkspace)workspace).ExportProvider.GetExportedValue<CompletionListCache>();
            return Assert.IsType<CompletionListCache>(exportProvider);
        }
    }
}
