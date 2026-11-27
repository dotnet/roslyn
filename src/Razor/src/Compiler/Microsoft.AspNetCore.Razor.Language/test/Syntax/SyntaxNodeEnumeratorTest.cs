// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class SyntaxNodeEnumeratorTest
{
    private static RazorSyntaxTree ParseDocument(string content)
    {
        var source = TestRazorSourceDocument.Create(content);
        return RazorSyntaxTree.Parse(source);
    }

    [Fact]
    public void DescendantNodes_MatchesDescendantNodesAsIEnumerable()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantNodes().ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DescendantNodes_WithDescendIntoChildren_MatchesDescendantNodesAsIEnumerable()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;
        bool filter(SyntaxNode n) => n is RazorDocumentSyntax or MarkupBlockSyntax or MarkupElementSyntax;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable(filter).ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantNodes(filter).ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DescendantNodes_EmptyDocument_MatchesDescendantNodes()
    {
        // Arrange
        var tree = ParseDocument("");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantNodes().ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_FiltersCorrectly()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().Where(n => n is MarkupElementSyntax).ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantNodes()
            .Where(static n => n is MarkupElementSyntax)
            .ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OfType_FiltersCorrectly()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().OfType<MarkupElementSyntax>().ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantNodes()
            .OfType<MarkupElementSyntax>()
            .ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Where_Select_ProducesCorrectResults()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable()
#pragma warning restore CS0618
            .Where(n => n is MarkupElementSyntax)
            .Select(n => n.Parent)
            .ToArray();
        var actual = root.DescendantNodes()
            .Where(static n => n is MarkupElementSyntax)
            .Select(static n => n.Parent)
            .ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Select_Where_EvaluatesSelectorOnce()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;
        var callCount = 0;

        SyntaxNode selector(SyntaxNode n)
        {
            callCount++;
            return n.Parent;
        }

        // Act
        var results = root.DescendantNodes()
            .Select(selector)
            .Where(n => n is MarkupBlockSyntax)
            .ToImmutableArray();

        // Assert - selector should be called exactly once per node visited (not twice)
#pragma warning disable CS0618
        var totalNodes = root.DescendantNodesAsIEnumerable().Count();
#pragma warning restore CS0618
        Assert.Equal(totalNodes, callCount);
    }

    [Fact]
    public void FirstOrDefault_ReturnsFirstMatch()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().FirstOrDefault(n => n is MarkupElementSyntax);
#pragma warning restore CS0618
        var actual = root.DescendantNodes()
            .FirstOrDefault(static n => n is MarkupElementSyntax);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FirstOrDefault_NoMatch_ReturnsNull()
    {
        // Arrange
        var tree = ParseDocument("Hello world");
        var root = tree.Root;

        // Act
        var actual = root.DescendantNodes()
            .FirstOrDefault(static n => n is CSharpCodeBlockSyntax);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void LastOrDefault_ReturnsLastMatch()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> <span>World</span> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantNodesAsIEnumerable().LastOrDefault(n => n is MarkupElementSyntax);
#pragma warning restore CS0618
        var actual = root.DescendantNodes()
            .LastOrDefault(static n => n is MarkupElementSyntax);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Any_ReturnsTrueWhenMatchExists()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act & Assert
        Assert.True(root.DescendantNodes().Any(static n => n is MarkupElementSyntax));
        Assert.False(root.DescendantNodes().Any(static n => n is RazorCommentBlockSyntax));
    }

    [Fact]
    public void DescendantTokens_MatchesDescendantTokens()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantTokensAsIEnumerable().ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantTokens().ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DescendantTokens_WithDescendIntoChildren_MatchesDescendantTokens()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;
        bool filter(SyntaxNode n) => n is RazorDocumentSyntax or MarkupBlockSyntax or MarkupElementSyntax;

        // Act
#pragma warning disable CS0618
        var expected = root.DescendantTokensAsIEnumerable(filter).ToArray();
#pragma warning restore CS0618
        var actual = root.DescendantTokens(filter).ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }
}
