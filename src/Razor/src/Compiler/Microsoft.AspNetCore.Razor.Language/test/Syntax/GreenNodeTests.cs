// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

public class GreenNodeTests
{
    [Fact]
    public void GetEnumerator_EmptyNode_ReturnsNodeAndToken()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var enumerator = node.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Equal(2, elements.Count);
        Assert.Same(node, elements[0]);  // Node first
        Assert.Same(token, elements[1]); // Then token
    }

    [Fact]
    public void GetEnumerator_SingleNode_ReturnsNodeAndToken()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Hello" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var enumerator = node.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Equal(2, elements.Count);
        Assert.Same(node, elements[0]);  // Node first
        Assert.Same(token, elements[1]); // Then token
    }

    [Fact]
    public void GetEnumerator_NodeWithChildren_PerformsDepthFirstTraversal()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   ├── MarkupTextLiteral (child2)
        //   │   └── Whitespace: " " (token2)
        //   └── GenericBlock (child3)
        //       └── MarkupTextLiteral (grandchild)
        //           └── Text: "World" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, " ");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "World");
        var grandchild = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var child3 = InternalSyntax.SyntaxFactory.GenericBlock(grandchild);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var enumerator = root.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Equal(8, elements.Count);
        Assert.Same(root, elements[0]);       // Root visited first
        Assert.Same(child1, elements[1]);     // First child
        Assert.Same(token1, elements[2]);     // First child's token
        Assert.Same(child2, elements[3]);     // Second child
        Assert.Same(token2, elements[4]);     // Second child's token
        Assert.Same(child3, elements[5]);     // Third child (parent of grandchild)
        Assert.Same(grandchild, elements[6]); // Grandchild visited after its parent
        Assert.Same(token3, elements[7]);     // Grandchild's token
    }

    [Fact]
    public void GetEnumerator_ComplexTree_MaintainsDepthFirstOrder()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Whitespace: " " (token1)
        //   └── GenericBlock (child2)
        //       ├── MarkupTextLiteral (grandchild1)
        //       │   └── Text: "A" (token2)
        //       └── MarkupTextLiteral (grandchild2)
        //           └── Text: "B" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, " ");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "A");
        var grandchild1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "B");
        var grandchild2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var child2 = InternalSyntax.SyntaxFactory.GenericBlock([grandchild1, grandchild2]);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var enumerator = root.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Equal(8, elements.Count);
        Assert.Same(root, elements[0]);
        Assert.Same(child1, elements[1]);
        Assert.Same(token1, elements[2]);     // child1's token
        Assert.Same(child2, elements[3]);
        Assert.Same(grandchild1, elements[4]);
        Assert.Same(token2, elements[5]);     // grandchild1's token
        Assert.Same(grandchild2, elements[6]);
        Assert.Same(token3, elements[7]);     // grandchild2's token
    }

    [Fact]
    public void GetEnumerator_CanBeUsedInForeachLoop()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: "World" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "World");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var elements = new List<GreenNode>();
        foreach (var node in root)
        {
            elements.Add(node);
        }

        // Assert
        Assert.Equal(5, elements.Count);
        Assert.Same(root, elements[0]);
        Assert.Same(child1, elements[1]);
        Assert.Same(token1, elements[2]);     // child1's token
        Assert.Same(child2, elements[3]);
        Assert.Same(token2, elements[4]);     // child2's token
    }

    [Fact]
    public void GetEnumerator_MultipleEnumerators_AreIndependent()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   └── MarkupTextLiteral (child)
        //       └── Text: "Test" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Test");
        var child = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        var root = InternalSyntax.SyntaxFactory.GenericBlock(child);

        // Act
        var enumerator1 = root.GetEnumerator();
        var enumerator2 = root.GetEnumerator();

        var hasNext1 = enumerator1.MoveNext();
        var hasNext2 = enumerator2.MoveNext();

        // Assert
        Assert.True(hasNext1);
        Assert.True(hasNext2);
        Assert.Same(root, enumerator1.Current);
        Assert.Same(root, enumerator2.Current);
    }

    [Fact]
    public void GetEnumerator_TokenNode_ReturnsSelfOnly()
    {
        // Tree structure:
        //   Text: "Hello" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");

        // Act
        var enumerator = token.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Single(elements);
        Assert.Same(token, elements[0]);
    }

    [Fact]
    public void GetEnumerator_MixedMarkupAndCode_PerformsDepthFirstTraversal()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (htmlNode)
        //   │   └── Text: "<div>" (htmlToken)
        //   ├── CSharpTransition (transitionNode)
        //   │   └── Transition: "@" (transitionToken)
        //   └── CSharpExpressionLiteral (codeNode)
        //       └── Identifier: "Model" (codeToken)

        // Arrange
        var htmlToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "<div>");
        var htmlNode = InternalSyntax.SyntaxFactory.MarkupTextLiteral(htmlToken);

        var transitionToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Transition, "@");
        var transitionNode = InternalSyntax.SyntaxFactory.CSharpTransition(transitionToken);

        var codeToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Identifier, "Model");
        var codeNode = InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(codeToken);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([htmlNode, transitionNode, codeNode]);

        // Act
        var enumerator = root.GetEnumerator();
        var elements = new List<GreenNode>();

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
        }

        // Assert
        Assert.Equal(7, elements.Count);
        Assert.Same(root, elements[0]);
        Assert.Same(htmlNode, elements[1]);
        Assert.Same(htmlToken, elements[2]);        // htmlNode's token
        Assert.Same(transitionNode, elements[3]);
        Assert.Same(transitionToken, elements[4]);  // transitionNode's token
        Assert.Same(codeNode, elements[5]);
        Assert.Same(codeToken, elements[6]);        // codeNode's token
    }

    [Fact]
    public void GetEnumerator_EnumeratesTokensAndNodes_InDepthFirstOrder()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Test" (token)
        //
        // Note: This test demonstrates that both nodes and tokens are enumerated

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Test");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var enumerator = node.GetEnumerator();
        var elements = new List<GreenNode>();
        var nodeTypes = new List<bool>(); // true for node, false for token

        while (enumerator.MoveNext())
        {
            elements.Add(enumerator.Current);
            nodeTypes.Add(!enumerator.Current.IsToken);
        }

        // Assert
        Assert.Equal(2, elements.Count);
        Assert.Same(node, elements[0]);
        Assert.Same(token, elements[1]);
        Assert.True(nodeTypes[0]);   // First element is a node
        Assert.False(nodeTypes[1]);  // Second element is a token
    }

    [Fact]
    public void Tokens_SingleToken_ReturnsOnlyToken()
    {
        // Tree structure:
        //   Text: "Hello" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var t in token.Tokens())
        {
            tokens.Add(t);
        }

        // Assert
        Assert.Single(tokens);
        Assert.Same(token, tokens[0]);
    }

    [Fact]
    public void Tokens_NodeWithSingleToken_ReturnsOnlyToken()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Hello" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var t in node.Tokens())
        {
            tokens.Add(t);
        }

        // Assert
        Assert.Single(tokens);
        Assert.Same(token, tokens[0]);
    }

    [Fact]
    public void Tokens_ComplexTree_ReturnsOnlyTokensInOrder()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   ├── MarkupTextLiteral (child2)
        //   │   └── Whitespace: " " (token2)
        //   └── GenericBlock (child3)
        //       └── MarkupTextLiteral (grandchild)
        //           └── Text: "World" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, " ");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "World");
        var grandchild = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var child3 = InternalSyntax.SyntaxFactory.GenericBlock(grandchild);
        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var token in root.Tokens())
        {
            tokens.Add(token);
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Same(token1, tokens[0]);  // "Hello"
        Assert.Same(token2, tokens[1]);  // " "
        Assert.Same(token3, tokens[2]);  // "World"
    }

    [Fact]
    public void Tokens_MixedMarkupAndCode_ReturnsAllTokensInDepthFirstOrder()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (htmlNode)
        //   │   └── Text: "<div>" (htmlToken)
        //   ├── CSharpTransition (transitionNode)
        //   │   └── Transition: "@" (transitionToken)
        //   └── CSharpExpressionLiteral (codeNode)
        //       └── Identifier: "Model" (codeToken)

        // Arrange
        var htmlToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "<div>");
        var htmlNode = InternalSyntax.SyntaxFactory.MarkupTextLiteral(htmlToken);

        var transitionToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Transition, "@");
        var transitionNode = InternalSyntax.SyntaxFactory.CSharpTransition(transitionToken);

        var codeToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Identifier, "Model");
        var codeNode = InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(codeToken);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([htmlNode, transitionNode, codeNode]);

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var token in root.Tokens())
        {
            tokens.Add(token);
        }

        // Assert
        Assert.Equal(3, tokens.Count);
        Assert.Same(htmlToken, tokens[0]);      // "<div>"
        Assert.Same(transitionToken, tokens[1]); // "@"
        Assert.Same(codeToken, tokens[2]);      // "Model"
    }

    [Fact]
    public void Tokens_EmptyToken_ReturnsEmptyToken()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var t in node.Tokens())
        {
            tokens.Add(t);
        }

        // Assert
        Assert.Single(tokens);
        Assert.Same(token, tokens[0]);
        Assert.Equal("", tokens[0].Content);
    }

    [Fact]
    public void Tokens_CanBeEnumeratedMultipleTimes()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "A" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: "B" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "A");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "B");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act - enumerate twice
        var firstEnumeration = new List<InternalSyntax.SyntaxToken>();
        foreach (var token in root.Tokens())
        {
            firstEnumeration.Add(token);
        }

        var secondEnumeration = new List<InternalSyntax.SyntaxToken>();
        foreach (var token in root.Tokens())
        {
            secondEnumeration.Add(token);
        }

        // Assert
        Assert.Equal(2, firstEnumeration.Count);
        Assert.Equal(2, secondEnumeration.Count);
        
        Assert.Same(token1, firstEnumeration[0]);
        Assert.Same(token2, firstEnumeration[1]);
        
        Assert.Same(token1, secondEnumeration[0]);
        Assert.Same(token2, secondEnumeration[1]);
    }

    [Fact]
    public void Tokens_WithManualEnumerator_WorksCorrectly()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Test" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Test");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var tokenEnumerable = node.Tokens();
        var enumerator = tokenEnumerable.GetEnumerator();
        
        var tokens = new List<InternalSyntax.SyntaxToken>();
        while (enumerator.MoveNext())
        {
            tokens.Add(enumerator.Current);
        }

        // Assert
        Assert.Single(tokens);
        Assert.Same(token, tokens[0]);
    }

    [Fact]
    public void Tokens_FilterOutNodesAndKeepOnlyTokens()
    {
        // Tree structure:
        //   GenericBlock (root) <- filtered out
        //   └── MarkupTextLiteral (child) <- filtered out
        //       └── Text: "OnlyThis" (token) <- kept

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "OnlyThis");
        var child = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);
        var root = InternalSyntax.SyntaxFactory.GenericBlock(child);

        // Act
        var tokens = new List<InternalSyntax.SyntaxToken>();
        foreach (var t in root.Tokens())
        {
            tokens.Add(t);
        }

        // Assert
        Assert.Single(tokens);
        Assert.Same(token, tokens[0]);
        Assert.Equal("OnlyThis", tokens[0].Content);
        Assert.True(tokens[0].IsToken);
    }

    [Fact]
    public void TokenEnumerable_Enumerator_Current_ThrowsBeforeFirstMoveNext()
    {
        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Test");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);
        var enumerator = node.Tokens().GetEnumerator();

        // Act & Assert
        try
        {
            _ = enumerator.Current;
        }
        catch (Exception ex)
        {
            // Note: We can't use Assert.Throws because enumerator is a ref-struct
            // and can't be captured in a lambda.
            Assert.IsType<NullReferenceException>(ex);
        }
    }

    [Fact]
    public void TokenEnumerable_Enumerator_Current_ReturnsCorrectToken()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "First" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: "Second" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "First");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Second");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);
        var enumerator = root.Tokens().GetEnumerator();

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Same(token1, enumerator.Current);
        Assert.Equal("First", enumerator.Current.Content);

        Assert.True(enumerator.MoveNext());
        Assert.Same(token2, enumerator.Current);
        Assert.Equal("Second", enumerator.Current.Content);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void ToString_SingleToken_ReturnsTokenContent()
    {
        // Tree structure:
        //   Text: "Hello" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");

        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ToString_EmptyToken_ReturnsEmptyString()
    {
        // Tree structure:
        //   Text: "" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");

        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToString_NodeWithSingleToken_ReturnsTokenContent()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Hello World" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello World");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ToString_ComplexTree_ConcatenatesAllTokensInDepthFirstOrder()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   ├── MarkupTextLiteral (child2)
        //   │   └── Whitespace: " " (token2)
        //   └── GenericBlock (child3)
        //       └── MarkupTextLiteral (grandchild)
        //           └── Text: "World" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, " ");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "World");
        var grandchild = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var child3 = InternalSyntax.SyntaxFactory.GenericBlock(grandchild);
        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ToString_MixedMarkupAndCode_ConcatenatesAllTokenContent()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (htmlNode)
        //   │   └── Text: "<div>" (htmlToken)
        //   ├── CSharpTransition (transitionNode)
        //   │   └── Transition: "@" (transitionToken)
        //   └── CSharpExpressionLiteral (codeNode)
        //       └── Identifier: "Model" (codeToken)

        // Arrange
        var htmlToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "<div>");
        var htmlNode = InternalSyntax.SyntaxFactory.MarkupTextLiteral(htmlToken);

        var transitionToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Transition, "@");
        var transitionNode = InternalSyntax.SyntaxFactory.CSharpTransition(transitionToken);

        var codeToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Identifier, "Model");
        var codeNode = InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(codeToken);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([htmlNode, transitionNode, codeNode]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("<div>@Model", result);
    }

    [Fact]
    public void ToString_MultipleNestedNodes_ConcatenatesInCorrectOrder()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Start" (token1)
        //   └── GenericBlock (child2)
        //       ├── MarkupTextLiteral (grandchild1)
        //       │   └── Text: "Middle" (token2)
        //       └── MarkupTextLiteral (grandchild2)
        //           └── Text: "End" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Start");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Middle");
        var grandchild1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "End");
        var grandchild2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var child2 = InternalSyntax.SyntaxFactory.GenericBlock([grandchild1, grandchild2]);
        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("StartMiddleEnd", result);
    }

    [Fact]
    public void ToString_WithWhitespaceTokens_PreservesWhitespace()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   ├── MarkupTextLiteral (child2)
        //   │   └── Whitespace: "   " (token2)
        //   └── MarkupTextLiteral (child3)
        //       └── Text: "World" (token3)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, "   ");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "World");
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("Hello   World", result);
    }

    [Fact]
    public void ToString_WithSpecialCharacters_PreservesAllCharacters()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Line1\n" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: "Line2\t\r" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Line1\n");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Line2\t\r");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("Line1\nLine2\t\r", result);
    }

    [Fact]
    public void ToString_WithUnicodeCharacters_PreservesUnicode()
    {
        // Tree structure:
        //   MarkupTextLiteral (node)
        //   └── Text: "Hello 🌍 World! ñáéíóú" (token)

        // Arrange
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello 🌍 World! ñáéíóú");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal("Hello 🌍 World! ñáéíóú", result);
    }

    [Fact]
    public void ToString_EmptyNodeWithEmptyTokens_ReturnsEmptyString()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: "" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToString_ComplexRazorExample_ConcatenatesCorrectly()
    {
        // Tree structure representing something like: "if (condition) { @Model.Name }"
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral
        //   │   └── Text: "if (condition) { " (token1)
        //   ├── CSharpTransition
        //   │   └── Transition: "@" (token2)
        //   ├── CSharpExpressionLiteral
        //   │   └── Identifier: "Model" (token3)
        //   ├── MarkupTextLiteral
        //   │   └── Text: "." (token4)
        //   ├── CSharpExpressionLiteral
        //   │   └── Identifier: "Name" (token5)
        //   └── MarkupTextLiteral
        //       └── Text: " }" (token6)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "if (condition) { ");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Transition, "@");
        var child2 = InternalSyntax.SyntaxFactory.CSharpTransition(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Identifier, "Model");
        var child3 = InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(token3);

        var token4 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, ".");
        var child4 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token4);

        var token5 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Identifier, "Name");
        var child5 = InternalSyntax.SyntaxFactory.CSharpExpressionLiteral(token5);

        var token6 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, " }");
        var child6 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token6);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3, child4, child5, child6]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("if (condition) { @Model.Name }", result);
    }

    [Fact]
    public void ToString_WidthMatchesStringLength()
    {
        // Tree structure:
        //   GenericBlock (root)
        //   ├── MarkupTextLiteral (child1)
        //   │   └── Text: "Hello" (token1)
        //   └── MarkupTextLiteral (child2)
        //       └── Text: " World!" (token2)

        // Arrange
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Hello");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, " World!");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("Hello World!", result);
        Assert.Equal(result.Length, root.Width);
    }

    [Fact]
    public void ToString_ZeroWidth_ReturnsEmptyString()
    {
        // This test verifies the first optimization: if _width == 0, return string.Empty

        // Arrange - Create a node with zero width (empty token)
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        
        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Same(string.Empty, result); // Verify it's the same instance, not a new allocation
        Assert.Equal(0, token.Width);
    }

    [Fact]
    public void ToString_SingleTokenOptimization_ReturnsSameTokenContent()
    {
        // This test verifies the second optimization: single token returns token.Content directly

        // Arrange - Create a node with exactly one token
        var tokenContent = "Hello World";
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, tokenContent);
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal(tokenContent, result);
        // Verify it's the same string instance (optimization check)
        Assert.Same(tokenContent, result);
    }

    [Fact]
    public void ToString_SingleTokenDirectCall_ReturnsSameTokenContent()
    {
        // Test the optimization when calling ToString() directly on a token

        // Arrange
        var tokenContent = "Direct token call";
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, tokenContent);

        // Act
        var result = token.ToString();

        // Assert
        Assert.Equal(tokenContent, result);
        Assert.Same(tokenContent, result); // Should be the same instance
    }

    [Fact]
    public void ToString_MultipleTokens_AllocatesNewString()
    {
        // This test verifies that when there are multiple tokens, a new string is allocated

        // Arrange - Create a node with multiple tokens
        var token1Content = "Hello";
        var token2Content = " World";
        
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, token1Content);
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, token2Content);
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("Hello World", result);
        // Verify it's NOT the same instance as either token (new allocation)
        Assert.NotSame(token1Content, result);
        Assert.NotSame(token2Content, result);
    }

    [Fact]
    public void ToString_EmptyTokensInComplexTree_ReturnsEmptyString()
    {
        // Test zero width optimization with a more complex tree structure

        // Arrange - Create a tree with only empty tokens
        var emptyToken1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken1);

        var emptyToken2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Same(string.Empty, result);
        Assert.Equal(0, root.Width);
    }

    [Fact]
    public void ToString_SingleEmptyTokenInNode_ReturnsEmptyString()
    {
        // Test single token optimization when that token is empty

        // Arrange
        var emptyToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
        Assert.Same(string.Empty, result); // Should return string.Empty directly
    }

    [Fact]
    public void ToString_SingleTokenWithLongContent_ReturnsTokenContent()
    {
        // Test single token optimization with longer content

        // Arrange
        var longContent = "This is a much longer piece of content that would normally require allocation but should be optimized";
        var token = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, longContent);
        var node = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token);

        // Act
        var result = node.ToString();

        // Assert
        Assert.Equal(longContent, result);
        Assert.Same(longContent, result); // Should be the same instance
    }

    [Fact]
    public void ToString_OptimizationPathVsAllocationPath_ProduceSameResult()
    {
        // Verify that both code paths produce the same result

        // Arrange - Single token (optimization path)
        var content = "Test Content";
        var singleToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, content);
        var singleNode = InternalSyntax.SyntaxFactory.MarkupTextLiteral(singleToken);

        // Arrange - Multiple tokens that concatenate to same content (allocation path)
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Test");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var token2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, " ");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token2);

        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "Content");
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var multiNode = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var singleResult = singleNode.ToString();
        var multiResult = multiNode.ToString();

        // Assert
        Assert.Equal(singleResult, multiResult);
        Assert.Same(content, singleResult); // Single token should be optimized
        Assert.NotSame(content, multiResult); // Multiple tokens should allocate new string
    }

    [Fact]
    public void ToString_SingleTokenOptimization_SkipsZeroWidthTokens()
    {
        // This test verifies the optimization when there are zero-width tokens followed by a single non-zero-width token
        // The algorithm should skip the zero-width tokens and use the single token optimization for the non-zero-width token

        // Arrange - Create a tree with zero-width tokens followed by one non-zero-width token
        var zeroWidthToken1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(zeroWidthToken1);

        var zeroWidthToken2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(zeroWidthToken2);

        var nonZeroTokenContent = "ActualContent";
        var nonZeroToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, nonZeroTokenContent);
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(nonZeroToken);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal(nonZeroTokenContent, result);
        // Verify it uses the single token optimization (same string instance)
        Assert.Same(nonZeroTokenContent, result);
        Assert.Equal(nonZeroTokenContent.Length, root.Width);
    }

    [Fact]
    public void ToString_SingleTokenOptimization_WithMultipleZeroWidthTokensAndOneNonZero()
    {
        // Test edge case with many zero-width tokens and one content token

        // Arrange - Create multiple zero-width tokens followed by one with content
        var emptyToken1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken1);

        var emptyToken2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken2);

        var emptyToken3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken3);

        var contentTokenValue = "OnlyRealToken";
        var contentToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, contentTokenValue);
        var child4 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(contentToken);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3, child4]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal(contentTokenValue, result);
        Assert.Same(contentTokenValue, result); // Should use single token optimization
    }

    [Fact]
    public void ToString_NoOptimization_WithZeroWidthTokenBetweenNonZeroTokens()
    {
        // Test that optimization is NOT used when there are multiple non-zero-width tokens,
        // even if separated by zero-width tokens

        // Arrange
        var token1Content = "First";
        var token1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, token1Content);
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token1);

        var zeroWidthToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(zeroWidthToken);

        var token3Content = "Second";
        var token3 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, token3Content);
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(token3);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal("FirstSecond", result);
        // Should NOT use optimization (new string allocation)
        Assert.NotSame(token1Content, result);
        Assert.NotSame(token3Content, result);
    }

    [Fact]
    public void ToString_SingleTokenOptimization_WithTrailingZeroWidthTokens()
    {
        // Test optimization when non-zero-width token is followed by zero-width tokens

        // Arrange
        var contentTokenValue = "MainContent";
        var contentToken = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, contentTokenValue);
        var child1 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(contentToken);

        var emptyToken1 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Text, "");
        var child2 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken1);

        var emptyToken2 = InternalSyntax.SyntaxFactory.Token(SyntaxKind.Whitespace, "");
        var child3 = InternalSyntax.SyntaxFactory.MarkupTextLiteral(emptyToken2);

        var root = InternalSyntax.SyntaxFactory.GenericBlock([child1, child2, child3]);

        // Act
        var result = root.ToString();

        // Assert
        Assert.Equal(contentTokenValue, result);
        Assert.Same(contentTokenValue, result); // Should use single token optimization
    }
}
