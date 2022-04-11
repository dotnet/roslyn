// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionTests : AbstractLanguageServerProtocolTests
    {
        private static readonly LSP.VSInternalClientCapabilities s_vsCompletionCapabilities = new LSP.VSInternalClientCapabilities
        {
            SupportsVisualStudioExtensions = true,
            TextDocument = new LSP.TextDocumentClientCapabilities
            {
                Completion = new LSP.VSInternalCompletionSetting
                {
                    CompletionListSetting = new LSP.CompletionListSetting
                    {
                        ItemDefaults = new string[] { CompletionHandler.EditRangeSetting }
                    }
                }
            }
        };

        [Fact]
        public async Task TestGetCompletionsAsync_PromotesCommitCharactersToListAsync()
        {
            var clientCapabilities = new LSP.VSInternalClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new LSP.VSInternalCompletionSetting
                    {
                        CompletionListSetting = new LSP.CompletionListSetting
                        {
                            ItemDefaults = new string[] { CompletionHandler.EditRangeSetting }
                        },
                        CompletionList = new LSP.VSInternalCompletionListSetting
                        {
                            CommitCharacters = true,
                        }
                    },
                }
            };
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(label: "A", kind: LSP.CompletionItemKind.Class, tags: new string[] { "Class", "Internal" },
                request: completionParams, document: document, commitCharacters: CompletionRules.Default.DefaultCommitCharacters).ConfigureAwait(false);
            var expectedCommitCharacters = expected.CommitCharacters;

            // Null out the commit characters since we're expecting the commit characters will be lifted onto the completion list.
            expected.CommitCharacters = null;

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            var vsCompletionList = Assert.IsAssignableFrom<LSP.VSInternalCompletionList>(results);
            Assert.Equal(expectedCommitCharacters, vsCompletionList.CommitCharacters.Value.First);
        }

        [Fact]
        public async Task TestGetCompletions_PromotesNothingWhenNoCommitCharactersAsync()
        {
            var clientCapabilities = new LSP.VSInternalClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new LSP.VSInternalCompletionSetting
                    {
                        CompletionListSetting = new LSP.CompletionListSetting
                        {
                            ItemDefaults = new string[] { CompletionHandler.EditRangeSetting }
                        },
                        CompletionList = new LSP.VSInternalCompletionListSetting
                        {
                            CommitCharacters = true,
                        }
                    },
                }
            };
            var markup =
@"namespace M
{{|caret:|}
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(label: "A", kind: LSP.CompletionItemKind.Class, tags: new string[] { "Class", "Internal" },
                request: completionParams, document: document, commitCharacters: CompletionRules.Default.DefaultCommitCharacters, insertText: "A").ConfigureAwait(false);
            var expectedCommitCharacters = expected.CommitCharacters;

            // Null out the commit characters since we're expecting the commit characters will be lifted onto the completion list.
            expected.CommitCharacters = null;

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.All(results.Items, item => Assert.Null(item.CommitCharacters));
            var vsCompletionList = Assert.IsAssignableFrom<LSP.VSInternalCompletionList>(results);
            Assert.Equal(expectedCommitCharacters, vsCompletionList.CommitCharacters.Value.First);
        }

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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(label: "A", kind: LSP.CompletionItemKind.Class, tags: new string[] { "Class", "Internal" },
                request: completionParams, document: document, commitCharacters: null).ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            Assert.NotNull(results.ItemDefaults.EditRange);
        }

        [Fact]
        public async Task TestGetCompletionsTypingAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        A{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "A",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(label: "A", kind: LSP.CompletionItemKind.Class, tags: new string[] { "Class", "Internal" },
                request: completionParams, document: document, commitCharacters: null).ConfigureAwait(false);

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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var solution = testLspServer.TestWorkspace.CurrentSolution;

            // Make sure the unimported types option is on by default.
            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(new OptionKey(CompletionOptionsStorage.ShowItemsFromUnimportedNamespaces, LanguageNames.CSharp), true);

            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams);
            Assert.False(results.Items.Any(item => "Console" == item.Label));
        }

        [Fact]
        public async Task TestGetCompletionsUsesSnippetOptionAsync()
        {
            var markup =
@"class A
{
    {|caret:|}
}";

            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);

            testLspServer.TestWorkspace.GlobalOptions.SetGlobalOption(
                new OptionKey(CompletionOptionsStorage.SnippetsBehavior, LanguageNames.CSharp), SnippetsRule.NeverInclude);

            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync("A", LSP.CompletionItemKind.Class, new string[] { "Class", "Internal" },
                completionParams, document, preselect: true, commitCharacters: ImmutableArray.Create(' ', '(', '[', '{', ';', '.')).ConfigureAwait(false);

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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "i",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = (LSP.VSInternalCompletionList)await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "\"",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var expected = await CreateCompletionItemAsync(
                label: "d", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
        }

        [Fact, WorkItem(59453, "https://github.com/dotnet/roslyn/issues/59453")]
        public async Task TestGetDateAndTimeCompletionOnGuid()
        {
            var markup =
@"using System;
class A
{
    void M()
    {
        Guid.NewGuid().ToString(""{|caret:|});
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "\"",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            var defaultRange = new LSP.Range
            {
                Start = new LSP.Position { Line = 5, Character = 19 },
                End = new LSP.Position { Line = 5, Character = 19 }
            };

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, insertText: @"\\A",
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            Assert.Equal(defaultRange, results.ItemDefaults.EditRange);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\\",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            var defaultRange = new LSP.Range
            {
                Start = new LSP.Position { Line = 5, Character = 20 },
                End = new LSP.Position { Line = 5, Character = 21 }
            };

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document,
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            Assert.Equal(defaultRange, results.ItemDefaults.EditRange);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "\\",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            var defaultRange = new LSP.Range
            {
                Start = new LSP.Position { Line = 5, Character = 23 },
                End = new LSP.Position { Line = 5, Character = 25 }
            };

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, insertText: @"\\A",
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            Assert.Equal(defaultRange, results.ItemDefaults.EditRange);
        }

        [Fact]
        [WorkItem(50964, "https://github.com/dotnet/roslyn/issues/50964")]
        public async Task TestGetRegexCompletionsWithoutItemDefaultSupportAsync()
        {
            var clientCapabilities = new LSP.VSInternalClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new LSP.VSInternalCompletionSetting
                    {
                        CompletionListSetting = new LSP.CompletionListSetting
                        {
                            ItemDefaults = null,
                        },
                    },
                }
            };

            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        new Regex(""{|caret:|}"");
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, clientCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var solution = testLspServer.GetCurrentSolution();
            var document = solution.Projects.First().Documents.First();

            var textEdit = GenerateTextEdit(@"\\A", startLine: 5, startChar: 19, endLine: 5, endChar: 19);

            var expected = await CreateCompletionItemAsync(
                label: @"\A", kind: LSP.CompletionItemKind.Text, tags: new string[] { "Text" }, request: completionParams, document: document, textEdit: textEdit,
                sortText: "0000").ConfigureAwait(false);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            AssertJsonEquals(expected, results.Items.First());
            Assert.Null(results.ItemDefaults);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var cache = GetCompletionListCache(testLspServer);
            Assert.NotNull(cache);

            var testAccessor = cache.GetTestAccessor();

            // This test assumes that the maximum cache size is 3, and will have to modified if this number changes.
            Assert.True(CompletionListCache.TestAccessor.MaximumCacheSize == 3);

            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Deletion,
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results.Items.First().TextEdit);
            Assert.Null(results.Items.First().InsertText);
            Assert.True(((LSP.VSInternalCompletionItem)results.Items.First()).VsResolveTextEditOnCommit);
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
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Null(results.Items.First().TextEdit);
            Assert.Null(results.Items.First().InsertText);
        }

        [Fact]
        public async Task TestAlwaysHasCommitCharactersWithoutVSCapabilityAsync()
        {
            var markup =
@"using System;
class A
{
    void M()
    {
        {|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results.Items);
            Assert.All(results.Items, (item) => Assert.NotNull(item.CommitCharacters));
        }

        [Fact]
        public async Task TestSoftSelectedItemsHaveNoCommitCharactersWithoutVSCapabilityAsync()
        {
            var markup =
@"using System.Text.RegularExpressions;
class A
{
    void M()
    {
        new Regex(""[{|caret:|}"")
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "[",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.NotNull(results);
            Assert.NotEmpty(results.Items);
            Assert.All(results.Items, (item) => Assert.Null(item.CommitCharacters));
        }

        [Fact]
        public async Task TestLargeCompletionListIsMarkedIncompleteAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
        }

        [Fact]
        public async Task TestIncompleteCompletionListContainsPreselectedItemAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    class W
    {
    }
    void M()
    {
        W someW = new {|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: " ",
                triggerKind: LSP.CompletionTriggerKind.TriggerCharacter);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            var itemW = results.Items.Single(item => item.Label == "W");
            Assert.True(itemW.Preselect);
        }

        [Fact]
        public async Task TestRequestForIncompleteListIsFilteredDownAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "a"));

            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "a",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Contains("ta", results.Items.First().Label, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TestIncompleteCompletionListFiltersWithPatternMatchingAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "C"));

            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "C",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Equal("TaiwanCalendar", results.Items.First().Label);
        }

        [Fact]
        public async Task TestIncompleteCompletionListWithDeletionAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            // Insert 'T' to make 'T' and trigger completion.
            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);
            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            // Insert 'ask' to make 'Task' and trigger completion.
            await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "ask"));
            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "k",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Equal("Task", results.Items.First().Label);

            // Delete 'ask' to make 'T' and trigger completion on deletion.
            await testLspServer.DeleteTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, caretLocation.Range.End.Line, caretLocation.Range.End.Character + 3));
            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Deletion,
                triggerCharacter: "a",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            // Insert 'i' to make 'Ti' and trigger completion.
            await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "i"));
            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "i",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Equal("Timeout", results.Items.First().Label);
        }

        [Fact]
        public async Task TestNewCompletionRequestDoesNotUseIncompleteListAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|firstCaret:|}
    }

    void M2()
    {
        Console.W{|secondCaret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var firstCaret = testLspServer.GetLocations("firstCaret").Single();
            await testLspServer.OpenDocumentAsync(firstCaret.Uri);

            // Make a completion request that returns an incomplete list.
            var completionParams = CreateCompletionParams(
                firstCaret,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);
            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            // Make a second completion request, but not for the original incomplete list.
            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("secondCaret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "W",
                triggerKind: LSP.CompletionTriggerKind.Invoked);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.False(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Equal("WindowHeight", results.Items.First().Label);
        }

        [Fact]
        public async Task TestRequestForIncompleteListWhenMissingCachedListAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        Ta{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();

            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "a",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.Contains("ta", results.Items.First().Label, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TestRequestForIncompleteListUsesCorrectCachedListAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M1()
    {
        int Taaa = 1;
        T{|firstCaret:|}
    }

    void M2()
    {
        int Saaa = 1;
        {|secondCaret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var firstCaretLocation = testLspServer.GetLocations("firstCaret").Single();
            await testLspServer.OpenDocumentAsync(firstCaretLocation.Uri);

            // Create request to on insertion of 'T'
            var completionParams = CreateCompletionParams(
                firstCaretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);
            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);
            Assert.Single(results.Items, item => item.Label == "Taaa");

            // Insert 'S' at the second caret
            var secondCaretLocation = testLspServer.GetLocations("secondCaret").Single();
            await testLspServer.InsertTextAsync(secondCaretLocation.Uri, (secondCaretLocation.Range.End.Line, secondCaretLocation.Range.End.Character, "S"));

            // Trigger completion on 'S'
            var triggerLocation = GetLocationPlusOne(secondCaretLocation);
            completionParams = CreateCompletionParams(
                triggerLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "S",
                triggerKind: LSP.CompletionTriggerKind.Invoked);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("Saaa", results.Items.First().Label);

            // Now type 'a' in M1 after 'T'
            await testLspServer.InsertTextAsync(firstCaretLocation.Uri, (firstCaretLocation.Range.End.Line, firstCaretLocation.Range.End.Character, "a"));

            // Trigger completion on 'a' (using incomplete as we previously returned incomplete completions from 'T').
            triggerLocation = GetLocationPlusOne(firstCaretLocation);
            completionParams = CreateCompletionParams(
                triggerLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "a",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);
            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);

            // Verify we get completions for 'Ta' and not from the 'S' location in M2
            Assert.True(results.IsIncomplete);
            Assert.True(results.Items.Length < 1000);
            Assert.DoesNotContain(results.Items, item => item.Label == "Saaa");
            Assert.Contains(results.Items, item => item.Label == "Taaa");

            static LSP.Location GetLocationPlusOne(LSP.Location originalLocation)
            {
                var newPosition = new LSP.Position { Character = originalLocation.Range.Start.Character + 1, Line = originalLocation.Range.Start.Line };
                return new LSP.Location
                {
                    Uri = originalLocation.Uri,
                    Range = new LSP.Range { Start = newPosition, End = newPosition }
                };
            }
        }

        [Fact]
        public async Task TestCompletionRequestRespectsListSizeOptionAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Explicit,
                triggerCharacter: "\0",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var globalOptions = testLspServer.TestWorkspace.GetService<IGlobalOptionService>();
            var listMaxSize = 1;

            globalOptions.SetGlobalOption(new OptionKey(LspOptions.MaxCompletionListSize), listMaxSize);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.Equal(listMaxSize, results.Items.Length);
        }

        [Fact]
        public async Task TestRequestForIncompleteListFiltersDownToEmptyAsync()
        {
            var markup =
@"using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
class A
{
    void M()
    {
        T{|caret:|}
    }
}";
            using var testLspServer = await CreateTestLspServerAsync(markup, s_vsCompletionCapabilities);
            var caretLocation = testLspServer.GetLocations("caret").Single();
            await testLspServer.OpenDocumentAsync(caretLocation.Uri);

            var completionParams = CreateCompletionParams(
                caretLocation,
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "T",
                triggerKind: LSP.CompletionTriggerKind.Invoked);

            var results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.Equal(1000, results.Items.Length);
            Assert.True(results.IsIncomplete);
            Assert.Equal("T", results.Items.First().Label);

            await testLspServer.InsertTextAsync(caretLocation.Uri, (caretLocation.Range.End.Line, caretLocation.Range.End.Character, "z"));

            completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(),
                invokeKind: LSP.VSInternalCompletionInvokeKind.Typing,
                triggerCharacter: "z",
                triggerKind: LSP.CompletionTriggerKind.TriggerForIncompleteCompletions);

            results = await RunGetCompletionsAsync(testLspServer, completionParams).ConfigureAwait(false);
            Assert.True(results.IsIncomplete);
            Assert.Empty(results.Items);
        }

        internal static Task<LSP.CompletionList> RunGetCompletionsAsync(TestLspServer testLspServer, LSP.CompletionParams completionParams)
        {
            return testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName,
                completionParams, CancellationToken.None);
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
