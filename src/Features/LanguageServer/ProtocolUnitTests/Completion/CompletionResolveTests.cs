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
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var tags = new string[] { "Class", "Internal" };
            var completionParams = CreateCompletionParams(locations["caret"].Single(), LSP.VSCompletionInvokeKind.Explicit, "\0", LSP.CompletionTriggerKind.Invoked);
            var document = workspace.CurrentSolution.Projects.First().Documents.First();

            var completionItem = await CreateCompletionItemAsync(
                "A", LSP.CompletionItemKind.Class, tags, completionParams, document, commitCharacters: CompletionRules.Default.DefaultCommitCharacters).ConfigureAwait(false);
            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var expected = CreateResolvedCompletionItem(completionItem, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(workspace.CurrentSolution, completionItem, clientCapabilities).ConfigureAwait(false);
            AssertJsonEquals(expected, results);
        }

        private static async Task<object> RunResolveCompletionItemAsync(Solution solution, LSP.CompletionItem completionItem, LSP.ClientCapabilities clientCapabilities = null)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(queue, LSP.Methods.TextDocumentCompletionResolveName,
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
