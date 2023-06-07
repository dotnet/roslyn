// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using Newtonsoft.Json;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionResolveTests : AbstractLanguageServerProtocolTests
    {
        public CompletionResolveTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [Theory, CombinatorialData]
        public async Task TestResolveCompletionItemFromListAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";

            var clientCapabilities = new LSP.VSInternalClientCapabilities
            {
                SupportsVisualStudioExtensions = true,
                TextDocument = new TextDocumentClientCapabilities()
                {
                    Completion = new VSInternalCompletionSetting()
                    {
                        CompletionList = new VSInternalCompletionListSetting()
                        {
                            Data = true,
                        }
                    }
                }
            };
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);

            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSInternalCompletionItem>(
                testLspServer,
                label: "A").ConfigureAwait(false);

            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var expected = CreateResolvedCompletionItem(clientCompletionItem, description, null);

            var results = (LSP.VSInternalCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Theory, CombinatorialData]
        public async Task TestResolveCompletionItemAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.VSInternalClientCapabilities { SupportsVisualStudioExtensions = true });
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSInternalCompletionItem>(testLspServer, label: "A").ConfigureAwait(false);

            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var expected = CreateResolvedCompletionItem(clientCompletionItem, description, null);

            var results = (LSP.VSInternalCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Theory, CombinatorialData]
        public async Task TestResolveOverridesCompletionItemAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.VSInternalClientCapabilities { SupportsVisualStudioExtensions = true });
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSInternalCompletionItem>(testLspServer, label: "M()").ConfigureAwait(false);
            var results = (LSP.VSInternalCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", results.TextEdit.Value.First.NewText);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/51125")]
        public async Task TestResolveOverridesCompletionItem_SnippetsEnabledAsync(bool mutatingLspWorkspace)
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

            // Explicitly enable snippets. This allows us to set the cursor with $0. Currently only applies to C# in Razor docs.
            var clientCapabilities = new LSP.VSInternalClientCapabilities
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSInternalCompletionItem>(
                testLspServer,
                label: "M()").ConfigureAwait(false);

            var results = (LSP.VSInternalCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();$0
    }", results.TextEdit.Value.First.NewText);
        }

        [Theory, CombinatorialData, WorkItem("https://github.com/dotnet/roslyn/issues/51125")]
        public async Task TestResolveOverridesCompletionItem_SnippetsEnabled_CaretOutOfSnippetScopeAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var selectedItem = CodeAnalysis.Completion.CompletionItem.Create(displayText: "M", isComplexTextEdit: true);
            var textEdit = await EditorLspCompletionResultCreationService.GenerateTextEditAsync(
                document, new TestCaretOutOfScopeCompletionService(testLspServer.TestWorkspace.Services.SolutionServices), selectedItem, snippetsSupported: true, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", textEdit.NewText);
        }

        [Theory, CombinatorialData]
        public async Task TestResolveCompletionItemWithMarkupContentAsync(bool mutatingLspWorkspace)
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
            var clientCapabilities = new ClientCapabilities
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
            };
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, clientCapabilities);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.CompletionItem>(
                testLspServer,
                label: "AMethod").ConfigureAwait(false);
            Assert.True(clientCompletionItem is not VSInternalCompletionItem);

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
                clientCompletionItem).ConfigureAwait(false);
            Assert.Equal(expected, results.Documentation.Value.Second.Value);
        }

        [Theory, CombinatorialData]
        public async Task TestResolveCompletionItemWithPlainTextAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.CompletionItem>(
                testLspServer,
                label: "AMethod").ConfigureAwait(false);
            Assert.True(clientCompletionItem is not VSInternalCompletionItem);

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
                clientCompletionItem).ConfigureAwait(false);
            Assert.Equal(expected, results.Documentation.Value.Second.Value);
        }

        [Theory, CombinatorialData]
        public async Task TestResolveCompletionItemWithPrefixSuffixAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new LSP.VSInternalClientCapabilities { SupportsVisualStudioExtensions = true });
            var clientCompletionItem = await GetCompletionItemToResolveAsync<LSP.VSInternalCompletionItem>(testLspServer, label: "(byte)").ConfigureAwait(false);

            var results = (LSP.VSInternalCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem).ConfigureAwait(false);
            Assert.Equal("(byte)", results.Label);
            Assert.NotNull(results.Description);
        }

        private static async Task<LSP.CompletionItem> RunResolveCompletionItemAsync(TestLspServer testLspServer, LSP.CompletionItem completionItem)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName,
                           completionItem, CancellationToken.None);
        }

        private static VSInternalCompletionItem Clone(VSInternalCompletionItem completionItem)
        {
            return new VSInternalCompletionItem()
            {
                Label = completionItem.Label,
                Kind = completionItem.Kind,
                Detail = completionItem.Detail,
                Documentation = completionItem.Documentation,
                Preselect = completionItem.Preselect,
                SortText = completionItem.SortText,
                FilterText = completionItem.FilterText,
                InsertText = completionItem.InsertText,
                InsertTextFormat = completionItem.InsertTextFormat,
                TextEdit = completionItem.TextEdit,
                AdditionalTextEdits = completionItem.AdditionalTextEdits,
                CommitCharacters = completionItem.CommitCharacters,
                Command = completionItem.Command,
                Data = completionItem.Data,
                Icon = completionItem.Icon,
                Description = completionItem.Description,
                VsCommitCharacters = completionItem.VsCommitCharacters,
                VsResolveTextEditOnCommit = completionItem.VsResolveTextEditOnCommit,
            };
        }

        private static VSInternalCompletionItem CreateResolvedCompletionItem(
            VSInternalCompletionItem completionItem,
            ClassifiedTextElement description,
            string documentation)
        {
            var expectedCompletionItem = Clone(completionItem);

            if (documentation != null)
            {
                expectedCompletionItem.Documentation = new MarkupContent()
                {
                    Kind = LSP.MarkupKind.PlainText,
                    Value = documentation
                };
            }

            expectedCompletionItem.Description = description;
            return expectedCompletionItem;
        }

        private static ClassifiedTextRun[] CreateClassifiedTextRunForClass(string className)
            => new ClassifiedTextRun[]
            {
                new ClassifiedTextRun("whitespace", string.Empty),
                new ClassifiedTextRun("keyword", "class"),
                new ClassifiedTextRun("whitespace", " "),
                new ClassifiedTextRun("class name", className),
                new ClassifiedTextRun("whitespace", string.Empty),
            };

        private static async Task<T> GetCompletionItemToResolveAsync<T>(
            TestLspServer testLspServer,
            string label) where T : LSP.CompletionItem
        {
            var completionParams = CreateCompletionParams(
                testLspServer.GetLocations("caret").Single(), LSP.VSInternalCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);

            var completionList = await RunGetCompletionsAsync(testLspServer, completionParams);

            if (testLspServer.ClientCapabilities.HasCompletionListDataCapability())
            {
                var vsCompletionList = Assert.IsAssignableFrom<VSInternalCompletionList>(completionList);
                Assert.NotNull(vsCompletionList.Data);
            }

            var serverCompletionItem = completionList.Items.FirstOrDefault(item => item.Label == label);
            var clientCompletionItem = ConvertToClientCompletionItem((T)serverCompletionItem);
            return clientCompletionItem;
        }

        private static async Task<LSP.CompletionList> RunGetCompletionsAsync(
            TestLspServer testLspServer,
            LSP.CompletionParams completionParams)
        {
            var completionList = await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName,
                completionParams, CancellationToken.None);

            // Emulate client behavior of promoting "Data" completion list properties onto completion items.
            if (testLspServer.ClientCapabilities.HasCompletionListDataCapability() &&
                completionList is VSInternalCompletionList vsCompletionList &&
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
            public TestCaretOutOfScopeCompletionService(SolutionServices services) : base(services, AsynchronousOperationListenerProvider.NullProvider)
            {
            }

            public override string Language => LanguageNames.CSharp;

            internal override Task<CodeAnalysis.Completion.CompletionList> GetCompletionsAsync(Document document,
                int caretPosition,
                CodeAnalysis.Completion.CompletionOptions options,
                OptionSet passThroughOptions,
                CompletionTrigger trigger = default,
                ImmutableHashSet<string> roles = null,
                CancellationToken cancellationToken = default) => Task.FromResult(CodeAnalysis.Completion.CompletionList.Empty);

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

            internal override bool ShouldTriggerCompletion(Project project, LanguageServices languageServices, SourceText text, int caretPosition, CompletionTrigger trigger, CodeAnalysis.Completion.CompletionOptions options, OptionSet passthroughOptions, ImmutableHashSet<string> roles = null)
                => false;

            internal override CompletionRules GetRules(CodeAnalysis.Completion.CompletionOptions options)
                => CompletionRules.Default;

            internal override Task<CompletionDescription> GetDescriptionAsync(Document document, CodeAnalysis.Completion.CompletionItem item, CodeAnalysis.Completion.CompletionOptions options, SymbolDescriptionOptions displayOptions, CancellationToken cancellationToken = default)
                => Task.FromResult(CompletionDescription.Empty);
        }
    }
}
