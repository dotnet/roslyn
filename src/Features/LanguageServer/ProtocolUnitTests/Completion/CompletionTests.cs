// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(label: "A", kind: LSP.CompletionItemKind.Class, tags: new string[] { "Class", "Internal" },
                request: completionParams, document: document, commitCharacters: CompletionRules.Default.DefaultCommitCharacters, insertText: "A").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var solution = testLspServer.TestWorkspace.CurrentSolution;

            // Make sure the unimported types option is on by default.
            testLspServer.TestWorkspace.SetOptions(testLspServer.TestWorkspace.CurrentSolution.Options
                .WithChangedOption(CompletionOptions.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp, true)
                .WithChangedOption(CompletionServiceOptions.IsExpandedCompletion, true));

            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var solution = testLspServer.TestWorkspace.CurrentSolution;
            solution = solution.WithOptions(solution.Options
                .WithChangedOption(CompletionOptions.SnippetsBehavior, LanguageNames.CSharp, SnippetsRule.AlwaysInclude));

            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" },
                completionParams, document, preselect: true, commitCharacters: ImmutableArray.Create(' ', '(', '[', '{', ';', '.'),
                insertText: "A").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Typing,
                triggerCharacter: "i",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = (LSP.VSCompletionList)await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Typing,
                triggerCharacter: "\"",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(
                label: "d", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, insertText: "d", sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        [WorkItem(50964, "https://github.com/dotnet/roslyn/issues/50964")]
        public async Task TestGetRegexCompletionsAsync()
        {
            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        new Regex(""{|caret:|}"");
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            // Set to use prototype completion behavior (i.e. feature flag).
            var options = solution.Workspace.Options.WithChangedOption(CompletionOptions.ForceRoslynLSPCompletionExperiment, LanguageNames.CSharp, true);
            Assert.True(solution.Workspace.TryApplyChanges(solution.WithOptions(options)));

            var textEdit = GenerateTextEdit(@"\\A", startLine: 5, startChar: 19, endLine: 5, endChar: 19);

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, textEdit: textEdit,
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        [WorkItem(50964, "https://github.com/dotnet/roslyn/issues/50964")]
        public async Task TestGetRegexLiteralCompletionsAsync()
        {
            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        new Regex(@""\{|caret:|}"");
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\\",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            // Set to use prototype completion behavior (i.e. feature flag).
            var options = solution.Workspace.Options.WithChangedOption(CompletionOptions.ForceRoslynLSPCompletionExperiment, LanguageNames.CSharp, true);
            Assert.True(solution.Workspace.TryApplyChanges(solution.WithOptions(options)));

            var textEdit = GenerateTextEdit(@"\A", startLine: 5, startChar: 20, endLine: 5, endChar: 21);

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, textEdit: textEdit,
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact]
        [WorkItem(50964, "https://github.com/dotnet/roslyn/issues/50964")]
        public async Task TestGetRegexCompletionsReplaceTextAsync()
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Typing,
                triggerCharacter: "\\",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            // Set to use prototype completion behavior (i.e. feature flag).
            var options = solution.Workspace.Options.WithChangedOption(CompletionOptions.ForceRoslynLSPCompletionExperiment, LanguageNames.CSharp, true);
            Assert.True(solution.Workspace.TryApplyChanges(solution.WithOptions(options)));

            var textEdit = GenerateTextEdit(@"\\A", startLine: 5, startChar: 23, endLine: 5, endChar: 25);

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, textEdit: textEdit,
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var cache = GetCompletionListCache(testLspServer);
            Assert.NotNull(cache);

            var testAccessor = cache.GetTestAccessor();

            // This test assumes that the maximum cache size is 3, and will have to modified if this number changes.
            Assert.True(CompletionListCache.TestAccessor.MaximumCacheSize == 3);

            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            // 1 item in cache
            await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            var completionList = cache.GetCachedCompletionList(0).CompletionList;
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 1);

            // 2 items in cache
            await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            completionList = cache.GetCachedCompletionList(0).CompletionList;
            Assert.NotNull(completionList);
            completionList = cache.GetCachedCompletionList(1).CompletionList;
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 2);

            // 3 items in cache
            await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            completionList = cache.GetCachedCompletionList(0).CompletionList;
            Assert.NotNull(completionList);
            completionList = cache.GetCachedCompletionList(1).CompletionList;
            Assert.NotNull(completionList);
            completionList = cache.GetCachedCompletionList(2).CompletionList;
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 3);

            // Maximum size of cache (3) should not be exceeded - oldest item should be ejected
            await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            var cacheEntry = cache.GetCachedCompletionList(0);
            Assert.Null(cacheEntry);
            completionList = cache.GetCachedCompletionList(1).CompletionList;
            Assert.NotNull(completionList);
            completionList = cache.GetCachedCompletionList(2).CompletionList;
            Assert.NotNull(completionList);
            completionList = cache.GetCachedCompletionList(3).CompletionList;
            Assert.NotNull(completionList);
            Assert.True(testAccessor.GetCacheContents().Count == 3);
        }

        [Fact]
        public async Task TestGetCompletionsWithDeletionInvokeKindAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Deletion,
                triggerCharacter: "M",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" },
                completionParams, document, commitCharacters: CompletionRules.Default.DefaultCommitCharacters).ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);

            // By default, completion doesn't trigger on deletion.
            Assert.Null(results);
        }

        [Fact]
        public async Task TestDoNotProvideOverrideTextEditsOrInsertTextAsync()
        {
            var markup =
@"abstract class A
{
    public abstract void M();
}

class B : A
{
    override {|caret:|}
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results.Items.First().TextEdit);
            Assert.Null(results.Items.First().InsertText);
        }

        [Fact]
        public async Task TestDoNotProvidePartialMethodTextEditsOrInsertTextAsync()
        {
            var markup =
@"partial class C
{
    partial void Method();
}

partial class C
{
    partial {|caret:|}
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(),
                invokeKind: LSP.VSCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results.Items.First().TextEdit);
            Assert.Null(results.Items.First().InsertText);
        }

        private static async Task<LSP.CompletionList> RunGetCompletionsAsync(TestLspServer testLspServer, LSP.CompletionParams completionParams)
        {
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            return await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName,
                completionParams, clientCapabilities, null, CancellationToken.None);
        }

        private static CompletionListCache GetCompletionListCache(TestLspServer testLspServer)
        {
            var dispatchAccessor = testLspServer.GetDispatcherAccessor();
            var handler = (CompletionHandler)dispatchAccessor.GetHandler<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName);
            Assert.NotNull(handler);
            return handler.GetTestAccessor().GetCache();
        }
    }
}
