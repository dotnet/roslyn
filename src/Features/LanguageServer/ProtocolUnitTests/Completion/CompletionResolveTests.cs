// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion
{
    public class CompletionResolveTests : LanguageServerProtocolTestsBase
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
            var (solution, locations) = CreateTestSolution(markup);
            var tags = new string[] { "Class", "Internal" };
            var completionParams = CreateCompletionParams(locations["caret"].First());
            var completionItem = CreateCompletionItem("A", LSP.CompletionItemKind.Class, tags, completionParams);
            var taggedText = new RoslynTaggedText[]
            {
                CreateTaggedText("Keyword", "class"),
                CreateTaggedText("Space", " "),
                CreateTaggedText("Class", "A")
            };

            var expected = CreateResolvedCompletionItem("A", LSP.CompletionItemKind.Class, null, completionParams, taggedText, "class A", null);

            var results = (LSP.CompletionItem)await RunResolveCompletionItemAsync(solution, completionItem);
            AssertCompletionItemsEqual(expected, results, true);
        }

        private static async Task<object> RunResolveCompletionItemAsync(Solution solution, LSP.CompletionItem completionItem)
            => await GetLanguageServer(solution).ResolveCompletionItemAsync(solution, completionItem, new LSP.ClientCapabilities(), CancellationToken.None);

        private static LSP.CompletionItem CreateResolvedCompletionItem(string text, LSP.CompletionItemKind kind, string[] tags, LSP.CompletionParams requestParameters,
            RoslynTaggedText[] taggedText, string detail, string documentation)
        {
            var resolvedCompletionItem = CreateCompletionItem(text, kind, tags, requestParameters);
            resolvedCompletionItem.Detail = detail;
            if (documentation != null)
            {
                resolvedCompletionItem.Documentation = new LSP.MarkupContent()
                {
                    Kind = LSP.MarkupKind.PlainText,
                    Value = documentation
                };
            }

            resolvedCompletionItem.Description = taggedText;
            return resolvedCompletionItem;
        }

        private static RoslynTaggedText CreateTaggedText(string tag, string text)
            => new RoslynTaggedText()
            {
                Tag = tag,
                Text = text
            };
    }
}
