// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var (solution, locations) = CreateTestSolution(markup);
            var tags = new string[] { "Class", "Internal" };
            var completionParams = CreateCompletionParams(locations["caret"].Single());
            var completionItem = CreateCompletionItem("A", LSP.CompletionItemKind.Class, tags, completionParams);
            var description = new ClassifiedTextElement(CreateClassifiedTextRunForClass("A"));
            var clientCapabilities = new LSP.VSClientCapabilities { SupportsVisualStudioExtensions = true };

            var expected = CreateResolvedCompletionItem("A", LSP.CompletionItemKind.Class, null, completionParams, description, "class A", null);

            var results = (LSP.VSCompletionItem)await RunResolveCompletionItemAsync(solution, completionItem, clientCapabilities);
            AssertJsonEquals(expected, results);
        }

        private static async Task<object> RunResolveCompletionItemAsync(Solution solution, LSP.CompletionItem completionItem, LSP.ClientCapabilities clientCapabilities = null)
            => await GetLanguageServer(solution).ResolveCompletionItemAsync(solution, completionItem, clientCapabilities, CancellationToken.None);

        private static LSP.VSCompletionItem CreateResolvedCompletionItem(string text, LSP.CompletionItemKind kind, string[] tags, LSP.CompletionParams requestParameters,
            ClassifiedTextElement description, string detail, string documentation)
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
