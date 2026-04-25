// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class CSharpRazorKeywordCompletionItemProviderTests(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly Action<RazorCompletionItem>[] s_csharpRazorpKeywordCollectionVerifiers = GetKeywordVerifies(CSharpRazorKeywordCompletionItemProvider.CSharpRazorKeywords);

    [Fact]
    public void CSharpRazorKeywordCompletionItems_ReturnsAllCSharpRazorKeywords()
    {
        // Act
        var completionItems = CSharpRazorKeywordCompletionItemProvider.CSharpRazorKeywordCompletionItems;

        // Assert
        Assert.Collection(
            completionItems,
            s_csharpRazorpKeywordCollectionVerifiers
        );
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsNotExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@$${");

        // Act
        var result = CSharpRazorKeywordCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsComplexExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@D$$ateTime.Now");

        // Act
        var result = CSharpRazorKeywordCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsExplicitExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@(so$$mething)");

        // Act
        var result = CSharpRazorKeywordCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsTrueForSimpleImplicitExpressions_WhenInvoked()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@w$$hi");

        // Act
        var result = CSharpRazorKeywordCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.True(result);
    }

    private static Action<RazorCompletionItem>[] GetKeywordVerifies(ImmutableArray<string> keywords)
    {
        using var builder = new PooledArrayBuilder<Action<RazorCompletionItem>>(keywords.Length);

        foreach (var keyword in keywords)
        {
            builder.Add(item => AssertRazorCompletionItem(keyword, item));
        }

        return builder.ToArray();
    }

    private static void AssertRazorCompletionItem(string keyword, RazorCompletionItem item)
    {
        Assert.Equal(keyword, item.InsertText);
        Assert.Equal(keyword, item.DisplayText);

        var completionDescription = Assert.IsType<CSharpRazorKeywordCompletionDescription>(item.DescriptionInfo);
        Assert.Equal(keyword + " Keyword", completionDescription.Description);

        Assert.Equal(CSharpRazorKeywordCompletionItemProvider.KeywordCommitCharacters, item.CommitCharacters);
    }

    private static RazorCompletionContext CreateRazorCompletionContext(TestCode text)
    {
        var syntaxTree = CreateSyntaxTree(text);
        var absoluteIndex = text.Position;
        var sourceDocument = RazorSourceDocument.Create("", RazorSourceDocumentProperties.Default);
        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);

        return new RazorCompletionContext(codeDocument, absoluteIndex, owner, syntaxTree, tagHelperDocumentContext, CompletionReason.Invoked);
    }

    private static RazorSyntaxTree CreateSyntaxTree(TestCode text)
    {
        var sourceDocument = TestRazorSourceDocument.Create(text.Text);

        var builder = new RazorParserOptions.Builder(RazorLanguageVersion.Latest, RazorFileKind.Legacy);
        var options = builder.ToOptions();

        return RazorSyntaxTree.Parse(sourceDocument, options);
    }
}
