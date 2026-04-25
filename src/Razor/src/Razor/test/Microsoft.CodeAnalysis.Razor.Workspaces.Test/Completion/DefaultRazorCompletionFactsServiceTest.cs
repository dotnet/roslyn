// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class DefaultRazorCompletionFactsServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetDirectiveCompletionItems_AllProvidersCompletionItems()
    {
        // Arrange
        var sourceDocument = RazorSourceDocument.Create("", RazorSourceDocumentProperties.Default);
        var codeDocument = RazorCodeDocument.Create(sourceDocument);
        var syntaxTree = RazorSyntaxTree.Parse(TestRazorSourceDocument.Create());
        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);

        var completionItem1 = RazorCompletionItem.CreateDirective(
            displayText: "displayText1",
            insertText: "insertText1",
            sortText: null,
            descriptionInfo: null!,
            commitCharacters: [],
            isSnippet: false);

        var context = new RazorCompletionContext(codeDocument, AbsoluteIndex: 0, Owner: null, SyntaxTree: syntaxTree, TagHelperDocumentContext: tagHelperDocumentContext);
        var provider1 = StrictMock.Of<IRazorCompletionItemProvider>(p =>
            p.GetCompletionItems(context) == ImmutableArray.Create(completionItem1));

        var completionItem2 = RazorCompletionItem.CreateDirective(
            displayText: "displayText2",
            insertText: "insertText2",
            sortText: null,
            descriptionInfo: null!,
            commitCharacters: [],
            isSnippet: false);

        var provider2 = StrictMock.Of<IRazorCompletionItemProvider>(p =>
            p.GetCompletionItems(context) == ImmutableArray.Create(completionItem2));

        var completionFactsService = new TestRazorCompletionFactsProvider(provider1, provider2);

        // Act
        var completionItems = completionFactsService.GetCompletionItems(context);

        // Assert
        Assert.Equal<RazorCompletionItem>([completionItem1, completionItem2], completionItems);
    }

    private sealed class TestRazorCompletionFactsProvider(
        params ImmutableArray<IRazorCompletionItemProvider> providers)
        : AbstractRazorCompletionFactsService(providers);
}
