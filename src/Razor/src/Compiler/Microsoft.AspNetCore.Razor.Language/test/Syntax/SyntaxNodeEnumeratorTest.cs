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
    public void EnumerateDescendantNodes_MatchesDescendantNodes()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
        var expected = root.DescendantNodes().ToArray();
        var actual = root.EnumerateDescendantNodes().ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumerateDescendantNodes_WithDescendIntoChildren_MatchesDescendantNodes()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;
        bool filter(SyntaxNode n) => n is RazorDocumentSyntax or MarkupBlockSyntax or MarkupElementSyntax;

        // Act
        var expected = root.DescendantNodes(filter).ToArray();
        var actual = root.EnumerateDescendantNodes(filter).ToImmutableArray();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumerateDescendantNodes_EmptyDocument_MatchesDescendantNodes()
    {
        // Arrange
        var tree = ParseDocument("");
        var root = tree.Root;

        // Act
        var expected = root.DescendantNodes().ToArray();
        var actual = root.EnumerateDescendantNodes().ToImmutableArray();

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
        var expected = root.DescendantNodes().Where(n => n is MarkupElementSyntax).ToArray();
        var actual = root.EnumerateDescendantNodes()
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
        var expected = root.DescendantNodes().OfType<MarkupElementSyntax>().ToArray();
        var actual = root.EnumerateDescendantNodes()
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
        var expected = root.DescendantNodes()
            .Where(n => n is MarkupElementSyntax)
            .Select(n => n.Parent)
            .ToArray();
        var actual = root.EnumerateDescendantNodes()
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
        var results = root.EnumerateDescendantNodes()
            .Select(selector)
            .Where(n => n is MarkupBlockSyntax)
            .ToImmutableArray();

        // Assert - selector should be called exactly once per node visited (not twice)
        var totalNodes = root.DescendantNodes().Count();
        Assert.Equal(totalNodes, callCount);
    }

    [Fact]
    public void FirstOrDefault_ReturnsFirstMatch()
    {
        // Arrange
        var tree = ParseDocument("@if (true) { <div>Hello</div> }");
        var root = tree.Root;

        // Act
        var expected = root.DescendantNodes().FirstOrDefault(n => n is MarkupElementSyntax);
        var actual = root.EnumerateDescendantNodes()
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
        var actual = root.EnumerateDescendantNodes()
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
        var expected = root.DescendantNodes().LastOrDefault(n => n is MarkupElementSyntax);
        var actual = root.EnumerateDescendantNodes()
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
        Assert.True(root.EnumerateDescendantNodes().Any(static n => n is MarkupElementSyntax));
        Assert.False(root.EnumerateDescendantNodes().Any(static n => n is RazorCommentBlockSyntax));
    }
}
