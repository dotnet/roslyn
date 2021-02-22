// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
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
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var completionItem = await CreateCompletionItemAsync(
                label: "A", LSP.CompletionItemKind.Class, tags, completionParams, document,
                commitCharacters: CompletionRules.Default.DefaultCommitCharacters, insertText: "A").ConfigureAwait(false);
            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var expected = CreateResolvedCompletionItem(completionItem, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, completionItem, clientCapabilities).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        [Fact]
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
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var completionItem = await CreateCompletionItemAsync(
                label: "M()", LSP.CompletionItemKind.Method, tags, completionParams, document,
                commitCharacters: CompletionRules.Default.DefaultCommitCharacters).ConfigureAwait(false);
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(
                testLspServer, completionItem, clientCapabilities).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();
    }", results.TextEdit.NewText);
        }

        [Fact]
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
            var document = testLspServer.GetCurrentSolution().Projects.First().Documents.First();

            var completionItem = await CreateCompletionItemAsync(
                label: "M()", LSP.CompletionItemKind.Method, tags, completionParams, document,
                commitCharacters: CompletionRules.Default.DefaultCommitCharacters).ConfigureAwait(false);

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
                testLspServer, completionItem, clientCapabilities).ConfigureAwait(false);

            Assert.NotNull(results.TextEdit);
            Assert.Null(results.InsertText);
            Assert.Equal(@"public override void M()
    {
        throw new System.NotImplementedException();$0
    }", results.TextEdit.NewText);
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
    }
}
