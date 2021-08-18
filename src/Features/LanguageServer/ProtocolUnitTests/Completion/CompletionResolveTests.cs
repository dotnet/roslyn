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
            var tags = new string[] { "Class", "Internal" };
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), LSP.VSCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);

            var completionList = await RunGetCompletionsAsync(testLspServer, completionParams);
            var serverCompletionItem = completionList.Items.FirstOrDefault(item => item.Label == "A");
            var completionResultId = ((CompletionResolveData)serverCompletionItem.Data).ResultId.Value;
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var clientCompletionItem = ConvertToClientCompletionItem(serverCompletionItem);

            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var expected = CreateResolvedCompletionItem(clientCompletionItem, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem, clientCapabilities).ConfigureAwait(false);
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
            var tags = new string[] { "Method", "Public" };
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), LSP.VSCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);
            var completionList = await RunGetCompletionsAsync(testLspServer, completionParams);
            var serverCompletionItem = completionList.Items.FirstOrDefault(item => item.Label == "M()");
            var completionResultId = ((CompletionResolveData)serverCompletionItem.Data).ResultId.Value;
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var clientCompletionItem = ConvertToClientCompletionItem(serverCompletionItem);
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, clientCompletionItem, clientCapabilities).ConfigureAwait(false);

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
            var tags = new string[] { "Method", "Public" };
            var completionParams = CreateCompletionParams(
                locations["caret"].Single(), LSP.VSCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);
            var completionList = await RunGetCompletionsAsync(testLspServer, completionParams);
            var serverCompletionItem = completionList.Items.FirstOrDefault(item => item.Label == "M()");
            var completionResultId = ((CompletionResolveData)serverCompletionItem.Data).ResultId.Value;
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();
            var clientCompletionItem = ConvertToClientCompletionItem(serverCompletionItem);

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
            var tags = new string[] { "Method", "Public" };

            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var selectedItem = CodeAnalysis.Completion.CompletionItem.Create(displayText: "M");
            var textEdit = await CompletionResolveHandler.GenerateTextEditAsync(
                document, new TestCaretOutOfScopeCompletionService(), selectedItem, snippetsSupported: true, CancellationToken.None).ConfigureAwait(false);

            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", textEdit.NewText);
        }

        private static async Task<object> RunResolveCompletionItemAsync(TestLspServer testLspServer, LSP.CompletionItem completionItem, LSP.ClientCapabilities clientCapabilities = null)
        {
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

        private static async Task<LSP.CompletionList> RunGetCompletionsAsync(TestLspServer testLspServer, LSP.CompletionParams completionParams)
        {
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };
            return await testLspServer.ExecuteRequestAsync<LSP.CompletionParams, LSP.CompletionList>(LSP.Methods.TextDocumentCompletionName,
                completionParams, clientCapabilities, null, CancellationToken.None);
        }

        private static LSP.VSCompletionItem ConvertToClientCompletionItem(LSP.CompletionItem serverCompletionItem)
        {
            var serializedItem = JsonConvert.SerializeObject(serverCompletionItem);
            var clientCompletionItem = JsonConvert.DeserializeObject<LSP.VSCompletionItem>(serializedItem);
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
