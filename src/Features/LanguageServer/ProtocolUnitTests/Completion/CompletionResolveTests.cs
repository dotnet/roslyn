﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionResolveTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestResolveCompletionItemFromListAsync()
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
            var clientCapabilities = new LSP.VSClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new TextDocumentClientCapabilities()
                {
                    Completion = new VSCompletionSetting()
                    {
                        CompletionList = new VSCompletionListSetting()
                        {
                            Data = true,
                        }
                    }
                }
            };
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSCompletionItem>(
                testLspServer,
                locations,
                label: "A",
                clientCapabilities).ConfigureAwait(false);

            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var expected = CreateResolvedCompletionItem(clientCompletionItem, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem, clientCapabilities).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        public async Task TestResolveCompletionItemAsync()
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
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSCompletionItem>(testLspServer, locations, label: "A").ConfigureAwait(false);

            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var expected = CreateResolvedCompletionItem(clientCompletionItem, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
        [WorkItem(51125, "https://github.com/dotnet/roslyn/issues/51125")]
        public async Task TestResolveOverridesCompletionItemAsync()
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
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSCompletionItem>(testLspServer, locations, label: "M()").ConfigureAwait(false);
            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", results.TextEdit.NewText);
        }

        [Fact]
        [WorkItem(51125, "https://github.com/dotnet/roslyn/issues/51125")]
        public async Task TestResolveOverridesCompletionItem_SnippetsEnabledAsync()
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
            // Explicitly enable snippets. This allows us to set the cursor with $0. Currently only applies to C# in Razor docs.
            var clientCapabilities = new LSP.VSClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new LSP.TextDocumentClientCapabilities
                {
                    Completion = new CompletionSetting
                    {
                        CompletionItem = new CompletionItemSetting
                        {
                            SnippetSupport = true
                        }
                    }
                }
            };
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSCompletionItem>(
                testLspServer,
                locations,
                label: "M()",
                clientCapabilities).ConfigureAwait(false);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem, clientCapabilities).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();$0
    }", results.TextEdit.NewText);
        }

        [Fact]
        [WorkItem(51125, "https://github.com/dotnet/roslyn/issues/51125")]
        public async Task TestResolveOverridesCompletionItem_SnippetsEnabled_CaretOutOfSnippetScopeAsync()
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
            using var testLspServer = CreateTestLspServer(markup, out _);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var selectedItem = CodeAnalysis.Completion.CompletionItem.Create(displayText: "M");
            var textEdit = await CompletionResolveHandler.GenerateTextEditAsync(
                document, new TestCaretOutOfScopeCompletionService(), selectedItem, snippetsSupported: true, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", textEdit.NewText);
        }

        [Fact]
        public async Task TestResolveCompletionItemWithMarkupContentAsync()
        {
            var markup =
@"
class A
{
    /// <summary>
    /// A cref <see cref=""AMethod""/>
    /// <br/>
    /// <strong>strong text</strong>
    /// <br/>
    /// <em>italic text</em>
    /// <br/>
    /// <u>underline text</u>
    /// <para>
    /// <list type='bullet'>
    /// <item>
    /// <description>Item 1.</description>
    /// </item>
    /// <item>
    /// <description>Item 2.</description>
    /// </item>
    /// </list>
    /// <a href = ""https://google.com"" > link text</a>
    /// </para>
    /// </summary>
    void AMethod(int i)
    {
    }

    void M()
    {
        AMet{|caret:|}
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.CompletionItem>(
                testLspServer,
                locations,
                label: "AMethod",
                new ClientCapabilities()).ConfigureAwait(false);
            Assert.True(clientCompletionItem is not VSCompletionItem);

            var expected = @"```csharp
void A.AMethod(int i)
```
  
A&nbsp;cref&nbsp;A\.AMethod\(int\)  
**strong&nbsp;text**  
_italic&nbsp;text_  
<u>underline&nbsp;text</u>  
  
•&nbsp;Item&nbsp;1\.  
•&nbsp;Item&nbsp;2\.  
  
[link text](https://google.com)";

            var results = await RunResolveCompletionItemAsync(
                testLspServer,
                clientCompletionItem,
                new ClientCapabilities
                {
                    TextDocument = new TextDocumentClientCapabilities
                    {
                        Completion = new CompletionSetting
                        {
                            CompletionItem = new CompletionItemSetting
                            {
                                DocumentationFormat = new MarkupKind[] { MarkupKind.Markdown }
                            }
                        }
                    }
                }).ConfigureAwait(false);
            Assert.Equal(expected, results.Documentation.Value.Second.Value);
        }

        [Fact]
        public async Task TestResolveCompletionItemWithPlainTextAsync()
        {
            var markup =
@"
class A
{
    /// <summary>
    /// A cref <see cref=""AMethod""/>
    /// <br/>
    /// <strong>strong text</strong>
    /// <br/>
    /// <em>italic text</em>
    /// <br/>
    /// <u>underline text</u>
    /// <para>
    /// <list type='bullet'>
    /// <item>
    /// <description>Item 1.</description>
    /// </item>
    /// <item>
    /// <description>Item 2.</description>
    /// </item>
    /// </list>
    /// <a href = ""https://google.com"" > link text</a>
    /// </para>
    /// </summary>
    void AMethod(int i)
    {
    }

    void M()
    {
        AMet{|caret:|}
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.CompletionItem>(
                testLspServer,
                locations,
                label: "AMethod",
                new ClientCapabilities()).ConfigureAwait(false);
            Assert.True(clientCompletionItem is not VSCompletionItem);

            var expected = @"void A.AMethod(int i)
A cref A.AMethod(int)
strong text
italic text
underline text

• Item 1.
• Item 2.

link text";

            var results = await RunResolveCompletionItemAsync(
                testLspServer,
                clientCompletionItem,
                new ClientCapabilities()).ConfigureAwait(false);
            Assert.Equal(expected, results.Documentation.Value.Second.Value);
        }

        [Fact]
        public async Task TestResolveCompletionItemWithPrefixSuffixAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        var a = 10;
        a.{|caret:|}
    }
}";
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSCompletionItem>(testLspServer, locations, label: "(byte)").ConfigureAwait(false);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);
            Assert.Equal("(byte)", results.Label);
            Assert.NotNull(results.Description);
        }

        private static async Task<LSP.CompletionItem> RunResolveCompletionItemAsync(TestLspServer testLspServer, LSP.CompletionItem completionItem, LSP.ClientCapabilities clientCapabilities = null)
        {
            clientCapabilities ??= new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            return await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName,
                           completionItem, clientCapabilities, null, CancellationToken.None);
        }

        private static LSP.VSCompletionItem CreateResolvedCompletionItem(
            VSCompletionItem completionItem,
            ClassifiedTextElement description,
            string detail,
            string documentation)
        {
            completionItem.Detail = detail;
            if (documentation != null)
            {
                completionItem.Documentation = new LSP.MarkupContent()
                {
                    Kind = LSP.MarkupKind.PlainText,
                    Value = documentation
                };
            }

            completionItem.Description = description;
            return completionItem;
        }

        private static ClassifiedTextRun[] CreateClassifiedTextRunForClass(string className)
            => new ClassifiedTextRun[]
            {
                new ClassifiedTextRun("keyword", "class"),
                new ClassifiedTextRun("whitespace", " "),
                new ClassifiedTextRun("class name", className)
            };

        private static async Task<T> GetCompletionItemToResolveAsync<T>(
            TestLspServer testLspServer,
            Dictionary<string, IList<LSP.Location>> locations,
            string label,
            LSP.ClientCapabilities clientCapabilities = null) where T : LSP.CompletionItem
        {
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), LSP.VSCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);

            clientCapabilities ??= new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            var completionList = await RunGetCompletionsAsync(testLspServer, completionParams, clientCapabilities);

            if (clientCapabilities.HasCompletionListDataCapability())
            {
                var vsCompletionList = Assert.IsAssignableFrom<VSCompletionList>(completionList);
                Assert.NotNull(vsCompletionList.Data);
            }

            var serverCompletionItem = completionList.Items.FirstOrDefault(item => item.Label == label);
            var clientCompletionItem = ConvertToClientCompletionItem((T)serverCompletionItem);
            return clientCompletionItem;
        }

        private static async Task<LSP.CompletionList> RunGetCompletionsAsync(
            TestLspServer testLspServer,
            LSP.CompletionParams completionParams,
            LSP.ClientCapabilities clientCapabilities)
        {
            var completionList = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName,
                completionParams, clientCapabilities, null, CancellationToken.None);

            // Emulate client behavior of promoting "Data" completion list properties onto completion items.
            if (clientCapabilities.HasCompletionListDataCapability() &&
                completionList is VSCompletionList vsCompletionList &&
                vsCompletionList.Data != null)
            {
                foreach (var completionItem in completionList.Items)
                {
                    Assert.Null(completionItem.Data);
                    completionItem.Data = vsCompletionList.Data;
                }
            }

            return completionList;
        }

        private static T ConvertToClientCompletionItem<T>(T serverCompletionItem) where T : LSP.CompletionItem
        {
            var serializedItem = JsonConvert.SerializeObject(serverCompletionItem);
            var clientCompletionItem = JsonConvert.DeserializeObject<T>(serializedItem);
            return clientCompletionItem;
        }

        private class TestCaretOutOfScopeCompletionService : CompletionService
        {
            public override string Language => LanguageNames.CSharp;

            public override Task<CodeAnalysis.Completion.CompletionList> GetCompletionsAsync(
                Document document,
                int caretPosition,
                CompletionTrigger trigger = default,
                ImmutableHashSet<string> roles = null,
                OptionSet options = null,
                CancellationToken cancellationToken = default)
                => Task.FromResult(CodeAnalysis.Completion.CompletionList.Empty);

            public override Task<CompletionChange> GetChangeAsync(
                Document document,
                CodeAnalysis.Completion.CompletionItem item,
                char? commitCharacter = null,
                CancellationToken cancellationToken = default)
            {
                var textChange = new TextChange(span: new TextSpan(start: 77, length: 9), newText: @"public override void M()
    {
        throw new System.NotImplementedException();
    }");

                return Task.FromResult(CompletionChange.Create(textChange, newPosition: 0));
            }
        }
    }
}
