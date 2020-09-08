// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var completionParams = CreateCompletionParams(locations["caret"].Single(), "\0", LSP.CompletionTriggerKind.Invoked);
            var completionItem = CreateCompletionItem
                ("A", LSP.CompletionItemKind.Class, tags, completionParams);
            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var expected = CreateResolvedCompletionItem(
                "A", LSP.CompletionItemKind.Class, null, completionParams, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(workspace.CurrentSolution, completionItem, clientCapabilities);
            AssertJsonEquals(expected, results);
        }

        private static async Task<object> RunResolveCompletionItemAsync(Solution solution, LSP.CompletionItem completionItem, LSP.ClientCapabilities clientCapabilities = null)
            => await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CompletionItem, LSP.CompletionItem>(LSP.Methods.TextDocumentCompletionResolveName,
                completionItem, clientCapabilities, null, CancellationToken.None);

        private static LSP.VSCompletionItem CreateResolvedCompletionItem(string text, LSP.CompletionItemKind kind, string[] tags, LSP.CompletionParams requestParameters,
            ClassifiedTextElement description, string detail, string documentation, string[] commitCharacters = null)
        {
            var resolvedCompletionItem = CreateCompletionItem(text, kind, tags, requestParameters, commitCharacters: commitCharacters);
            resolvedCompletionItem.Detail = detail;
            if (documentation != null)
            {
                resolvedCompletionItem.Documentation = new LSP.MarkupContent()
                {
                    Kind = LSP.MarkupKind.PlainText,
                    Value = documentation
                };
            }

            resolvedCompletionItem.Description = description;
            return resolvedCompletionItem;
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
