// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Syntax;

public class SyntaxListTests
{
    private static MarkupTextLiteralSyntax CreateMarkupTextLiteral(params ReadOnlySpan<SyntaxToken> tokens)
    {
        using var builder = new PooledArrayBuilder<SyntaxToken>(tokens.Length);
        builder.AddRange(tokens);

        return SyntaxFactory.MarkupTextLiteral(builder.ToList());
    }

    private static readonly SyntaxToken s_openAngle = SyntaxFactory.Token(SyntaxKind.OpenAngle, "<");
    private static readonly SyntaxToken s_closeAngle = SyntaxFactory.Token(SyntaxKind.CloseAngle, ">");
    private static readonly SyntaxToken s_leftBrace = SyntaxFactory.Token(SyntaxKind.LeftBrace, "{");
    private static readonly SyntaxToken s_rightBrace = SyntaxFactory.Token(SyntaxKind.RightBrace, "}");
    private static readonly SyntaxToken s_forwardSlash = SyntaxFactory.Token(SyntaxKind.ForwardSlash, "/");

    [Fact]
    public void Add_WhenListIsEmpty_AddsNodeAtEnd()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        var node = CreateMarkupTextLiteral(s_openAngle);

        // Act
        var newList = emptyList.Add(node);

        // Assert
        Assert.Empty(emptyList);
        Assert.Single(newList);
        Assert.True(node.IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void Add_WhenListHasNodes_AddsNodeAtEnd()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];
        var nodeToAdd = CreateMarkupTextLiteral(s_closeAngle);

        // Act
        var newList = originalList.Add(nodeToAdd);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(2, newList.Count);
        Assert.True(existingNode.IsEquivalentTo(newList[0]));
        Assert.True(nodeToAdd.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void Add_WithMultipleNodes_AddsNodesInOrder()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        var node3 = CreateMarkupTextLiteral(s_leftBrace);

        // Act
        var list = SyntaxList<SyntaxNode>.Empty;
        var list1 = list.Add(node1);
        var list2 = list1.Add(node2);
        var list3 = list2.Add(node3);

        // Assert
        Assert.Empty(list);
        Assert.Single(list1);
        Assert.Equal(2, list2.Count);
        Assert.Equal(3, list3.Count);

        Assert.True(node1.IsEquivalentTo(list1[0]));
        Assert.True(node1.IsEquivalentTo(list2[0]));
        Assert.True(node2.IsEquivalentTo(list2[1]));
        Assert.True(node1.IsEquivalentTo(list3[0]));
        Assert.True(node2.IsEquivalentTo(list3[1]));
        Assert.True(node3.IsEquivalentTo(list3[2]));
    }

    [Fact]
    public void Add_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1];

        // Act
        var newList = originalList.Add(node2);

        // Assert
        Assert.Single(originalList);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.Equal(2, newList.Count);
    }

    [Fact]
    public void Add_DelegatesToInsert_WithCorrectIndex()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1];

        // Act
        var addResult = originalList.Add(node2);
        var insertResult = originalList.Insert(originalList.Count, node2);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);

        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void Add_ThrowsArgumentNullException_WhenNodeIsNull()
    {
        // Arrange
        var list = SyntaxList<SyntaxNode>.Empty;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.Add(null!));
    }
    [Fact]
    public void AddRange_WithReadOnlySpan_WhenListIsEmpty_AddsAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.AddRange(nodesToAdd.AsSpan());

        // Assert
        Assert.Empty(emptyList);
        Assert.Equal(nodesToAdd.Length, newList.Count);

        for (var i = 0; i < nodesToAdd.Length; i++)
        {
            Assert.True(nodesToAdd[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void AddRange_WithReadOnlySpan_WhenListHasNodes_AddsNodesToEnd()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.AddRange(nodesToAdd.AsSpan());

        // Assert
        Assert.Single(originalList);
        Assert.Equal(1 + nodesToAdd.Length, newList.Count);

        Assert.True(existingNode.IsEquivalentTo(newList[0]));
        Assert.True(nodesToAdd[0].IsEquivalentTo(newList[1]));
        Assert.True(nodesToAdd[1].IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void AddRange_WithReadOnlySpan_WithEmptySpan_ReturnsSameList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];
        var emptySpan = ReadOnlySpan<SyntaxNode>.Empty;

        // Act
        var newList = originalList.AddRange(emptySpan);

        // Assert
        Assert.Equal(originalList.Count, newList.Count);
        Assert.True(originalList[0].IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void AddRange_WithReadOnlySpan_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.AddRange(nodesToAdd.AsSpan());

        // Assert
        Assert.Single(originalList);
        Assert.True(existingNode.IsEquivalentTo(originalList[0]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void AddRange_WithReadOnlySpan_DelegatesToInsertRange_WithCorrectIndex()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        var span = nodesToAdd.AsSpan();

        // Act
        var addResult = originalList.AddRange(span);
        var insertResult = originalList.InsertRange(originalList.Count, span);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);
        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void AddRange_WhenListIsEmpty_AddsAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.AddRange(nodesToAdd);

        // Assert
        Assert.Empty(emptyList);
        Assert.Equal(nodesToAdd.Length, newList.Count);

        for (var i = 0; i < nodesToAdd.Length; i++)
        {
            Assert.True(nodesToAdd[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void AddRange_WhenListHasNodes_AddsNodesToEnd()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.AddRange(nodesToAdd);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(1 + nodesToAdd.Length, newList.Count);

        Assert.True(existingNode.IsEquivalentTo(newList[0]));
        Assert.True(nodesToAdd[0].IsEquivalentTo(newList[1]));
        Assert.True(nodesToAdd[1].IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void AddRange_WithEmptyCollection_ReturnsSameList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];
        var emptyNodes = Array.Empty<SyntaxNode>();

        // Act
        var newList = originalList.AddRange(emptyNodes);

        // Assert
        Assert.Equal(originalList.Count, newList.Count);
        Assert.True(originalList[0].IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void AddRange_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.AddRange(nodesToAdd);

        // Assert
        Assert.Single(originalList);
        Assert.True(existingNode.IsEquivalentTo(originalList[0]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void AddRange_DelegatesToInsertRange_WithCorrectIndex()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToAdd = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var addResult = originalList.AddRange(nodesToAdd);
        var insertResult = originalList.InsertRange(originalList.Count, nodesToAdd);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);
        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void AddRange_ThrowsArgumentNullException_WhenNodesIsNull()
    {
        // Arrange
        var list = SyntaxList<SyntaxNode>.Empty;
        IEnumerable<SyntaxNode> newNodes = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.AddRange(newNodes));
    }

    [Fact]
    public void AddRange_WithDifferentNodeTypes_AddsAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;

        // Create different types of syntax nodes
        SyntaxNode[] mixedNodes = [
            CreateMarkupTextLiteral(s_openAngle),
            SyntaxFactory.MarkupBlock(),
            SyntaxFactory.MarkupTagHelperAttributeValue()
        ];

        // Act
        var newList = emptyList.AddRange(mixedNodes);

        // Assert
        Assert.Equal(mixedNodes.Length, newList.Count);

        for (var i = 0; i < mixedNodes.Length; i++)
        {
            Assert.True(mixedNodes[i].IsEquivalentTo(newList[i]));
            Assert.Equal(mixedNodes[i].Kind, newList[i].Kind);
        }
    }

    [Fact]
    public void AddRange_WithListAsSource_AddsAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        List<SyntaxNode> nodesList = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.AddRange(nodesList);

        // Assert
        Assert.Equal(nodesList.Count, newList.Count);

        for (var i = 0; i < nodesList.Count; i++)
        {
            Assert.True(nodesList[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void AddRange_WithIEnumerableAsSource_AddsAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;

        IEnumerable<SyntaxNode> nodes = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.AddRange(nodes);

        // Assert
        Assert.Equal(3, newList.Count);

        var index = 0;
        foreach (var node in nodes)
        {
            Assert.True(node.IsEquivalentTo(newList[index++]));
        }
    }

    [Fact]
    public void Insert_AtBeginning_InsertsNodeAtStart()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_closeAngle);
        var node2 = CreateMarkupTextLiteral(s_rightBrace);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var nodeToInsert = CreateMarkupTextLiteral(s_openAngle);

        // Act
        var newList = originalList.Insert(0, nodeToInsert);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.Equal(3, newList.Count);
        Assert.True(nodeToInsert.IsEquivalentTo(newList[0]));
        Assert.True(node1.IsEquivalentTo(newList[1]));
        Assert.True(node2.IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void Insert_AtMiddle_InsertsNodeAtCorrectPosition()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var nodeToInsert = CreateMarkupTextLiteral(s_forwardSlash);

        // Act
        var newList = originalList.Insert(1, nodeToInsert);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.Equal(3, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(nodeToInsert.IsEquivalentTo(newList[1]));
        Assert.True(node2.IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void Insert_AtEnd_SameAsAdd()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var nodeToInsert = CreateMarkupTextLiteral(s_closeAngle);

        // Act
        var insertResult = originalList.Insert(originalList.Count, nodeToInsert);
        var addResult = originalList.Add(nodeToInsert);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);
        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void Insert_IntoEmptyList_CreatesSingleItemList()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        var node = CreateMarkupTextLiteral(s_openAngle);

        // Act
        var newList = emptyList.Insert(0, node);

        // Assert
        Assert.Empty(emptyList);
        Assert.Single(newList);
        Assert.True(node.IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void Insert_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node1];
        var nodeToInsert = CreateMarkupTextLiteral(s_forwardSlash);

        // Act
        var newList = originalList.Insert(0, nodeToInsert);

        // Assert
        Assert.Single(originalList);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.Equal(2, newList.Count);
    }

    [Fact]
    public void Insert_ThrowsArgumentNullException_WhenNodeIsNull()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.Insert(0, null!));
    }

    [Fact]
    public void Insert_ThrowsArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        var node = CreateMarkupTextLiteral(s_closeAngle);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, node));
    }

    [Fact]
    public void Insert_ThrowsArgumentOutOfRangeException_WhenIndexExceedsCount()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        var node = CreateMarkupTextLiteral(s_closeAngle);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(list.Count + 1, node));
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_AtBeginning_InsertsNodesAtStart()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert.AsSpan());

        // Assert
        Assert.Single(originalList);
        Assert.Equal(nodesToInsert.Length + 1, newList.Count);
        Assert.True(nodesToInsert[0].IsEquivalentTo(newList[0]));
        Assert.True(nodesToInsert[1].IsEquivalentTo(newList[1]));
        Assert.True(existingNode.IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_AtMiddle_InsertsNodesAtCorrectPosition()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_rightBrace)
        ];

        // Act
        var newList = originalList.InsertRange(1, nodesToInsert.AsSpan());

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.Equal(originalList.Count + nodesToInsert.Length, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(nodesToInsert[0].IsEquivalentTo(newList[1]));
        Assert.True(nodesToInsert[1].IsEquivalentTo(newList[2]));
        Assert.True(node2.IsEquivalentTo(newList[3]));
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_AtEnd_SameAsAddRange()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node1];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];
        var span = nodesToInsert.AsSpan();

        // Act
        var insertResult = originalList.InsertRange(originalList.Count, span);
        var addResult = originalList.AddRange(span);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);
        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_WithEmptySpan_ReturnsSameList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];
        var emptySpan = ReadOnlySpan<SyntaxNode>.Empty;

        // Act
        var newList = originalList.InsertRange(0, emptySpan);

        // Assert
        Assert.Equal(originalList.Count, newList.Count);
        Assert.True(originalList[0].IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_IntoEmptyList_CreatesListWithAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.InsertRange(0, nodesToInsert.AsSpan());

        // Assert
        Assert.Empty(emptyList);
        Assert.Equal(nodesToInsert.Length, newList.Count);

        for (var i = 0; i < nodesToInsert.Length; i++)
        {
            Assert.True(nodesToInsert[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert.AsSpan());

        // Assert
        Assert.Single(originalList);
        Assert.True(existingNode.IsEquivalentTo(originalList[0]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_ThrowsArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        SyntaxNode[] nodes = [CreateMarkupTextLiteral(s_closeAngle)];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, nodes.AsSpan()));
    }

    [Fact]
    public void InsertRange_WithReadOnlySpan_ThrowsArgumentOutOfRangeException_WhenIndexExceedsCount()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        SyntaxNode[] nodes = [CreateMarkupTextLiteral(s_closeAngle)];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, nodes.AsSpan()));
    }

    [Fact]
    public void InsertRange_AtBeginning_InsertsNodesAtStart()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(nodesToInsert.Length + 1, newList.Count);
        Assert.True(nodesToInsert[0].IsEquivalentTo(newList[0]));
        Assert.True(nodesToInsert[1].IsEquivalentTo(newList[1]));
        Assert.True(existingNode.IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void InsertRange_AtMiddle_InsertsNodesAtCorrectPosition()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_rightBrace)
        ];

        // Act
        var newList = originalList.InsertRange(1, nodesToInsert);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.Equal(originalList.Count + nodesToInsert.Length, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(nodesToInsert[0].IsEquivalentTo(newList[1]));
        Assert.True(nodesToInsert[1].IsEquivalentTo(newList[2]));
        Assert.True(node2.IsEquivalentTo(newList[3]));
    }

    [Fact]
    public void InsertRange_AtEnd_SameAsAddRange()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node1];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var insertResult = originalList.InsertRange(originalList.Count, nodesToInsert);
        var addResult = originalList.AddRange(nodesToInsert);

        // Assert
        Assert.Equal(addResult.Count, insertResult.Count);
        for (var i = 0; i < addResult.Count; i++)
        {
            Assert.True(addResult[i].IsEquivalentTo(insertResult[i]));
        }
    }

    [Fact]
    public void InsertRange_WithEmptyCollection_ReturnsSameList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];
        var emptyNodes = Array.Empty<SyntaxNode>();

        // Act
        var newList = originalList.InsertRange(0, emptyNodes);

        // Assert
        Assert.Equal(originalList.Count, newList.Count);
        Assert.True(originalList[0].IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void InsertRange_IntoEmptyList_CreatesListWithAllNodes()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        
        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = emptyList.InsertRange(0, nodesToInsert);

        // Assert
        Assert.Empty(emptyList);
        Assert.Equal(nodesToInsert.Length, newList.Count);
        
        for (var i = 0; i < nodesToInsert.Length; i++)
        {
            Assert.True(nodesToInsert[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void InsertRange_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        SyntaxNode[] nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert);

        // Assert
        Assert.Single(originalList);
        Assert.True(existingNode.IsEquivalentTo(originalList[0]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void InsertRange_ThrowsArgumentNullException_WhenNodesIsNull()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        IEnumerable<SyntaxNode> newNodes = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.InsertRange(0, newNodes));
    }

    [Fact]
    public void InsertRange_ThrowsArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        var nodes = new[] { CreateMarkupTextLiteral(s_closeAngle) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, nodes));
    }

    [Fact]
    public void InsertRange_ThrowsArgumentOutOfRangeException_WhenIndexExceedsCount()
    {
        // Arrange
        SyntaxList<SyntaxNode> list = [CreateMarkupTextLiteral(s_openAngle)];
        var nodes = new[] { CreateMarkupTextLiteral(s_closeAngle) };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(list.Count + 1, nodes));
    }

    [Fact]
    public void InsertRange_WithListAsSource_InsertsAllNodes()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        List<SyntaxNode> nodesToInsert = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(nodesToInsert.Count + 1, newList.Count);
        
        for (var i = 0; i < nodesToInsert.Count; i++)
        {
            Assert.True(nodesToInsert[i].IsEquivalentTo(newList[i]));
        }
        Assert.True(existingNode.IsEquivalentTo(newList[^1]));
    }

    [Fact]
    public void InsertRange_WithIEnumerableAsSource_InsertsAllNodes()
    {
        // Arrange
        var existingNode = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [existingNode];

        IEnumerable<SyntaxNode> nodesToInsert = [
            CreateMarkupTextLiteral(s_openAngle),
            CreateMarkupTextLiteral(s_forwardSlash)
        ];

        // Act
        var newList = originalList.InsertRange(0, nodesToInsert);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(3, newList.Count);
        
        var index = 0;
        foreach (var node in nodesToInsert)
        {
            Assert.True(node.IsEquivalentTo(newList[index++]));
        }
        Assert.True(existingNode.IsEquivalentTo(newList[^1]));
    }

    [Fact]
    public void RemoveAt_SingleElementList_ReturnsEmptyList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];

        // Act
        var newList = originalList.RemoveAt(0);

        // Assert
        Assert.Single(originalList);
        Assert.Empty(newList);
    }

    [Fact]
    public void RemoveAt_FromBeginning_RemovesCorrectNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.RemoveAt(0);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node2.IsEquivalentTo(newList[0]));
        Assert.True(node3.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void RemoveAt_FromMiddle_RemovesCorrectNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.RemoveAt(1);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node3.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void RemoveAt_FromEnd_RemovesCorrectNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.RemoveAt(2);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node2.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void RemoveAt_ThrowsArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
    }

    [Fact]
    public void RemoveAt_ThrowsArgumentOutOfRangeException_WhenIndexExceedsOrEqualsCount()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(list.Count));
    }

    [Fact]
    public void RemoveAt_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        // Act
        var newList = originalList.RemoveAt(0);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.True(node2.IsEquivalentTo(originalList[1]));
        Assert.Single(newList);
    }

    [Fact]
    public void RemoveAt_DelegatesToRemove_WithCorrectNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        // Act
        var removeAtResult = originalList.RemoveAt(0);
        var removeResult = originalList.Remove(originalList[0]);

        // Assert
        Assert.Equal(removeResult.Count, removeAtResult.Count);
        for (var i = 0; i < removeResult.Count; i++)
        {
            Assert.True(removeResult[i].IsEquivalentTo(removeAtResult[i]));
        }
    }

    [Fact]
    public void Remove_SingleElementList_ReturnsEmptyList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> originalList = [node];

        // Act
        var newList = originalList.Remove(originalList[0]);

        // Assert
        Assert.Single(originalList);
        Assert.Empty(newList);
    }

    [Fact]
    public void Remove_FromList_RemovesMatchingNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.Remove(originalList[1]);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node3.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void Remove_NodeNotInList_ReturnsSameList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var nodeNotInList = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        // Act
        var newList = originalList.Remove(nodeNotInList);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node2.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void Remove_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];

        // Act
        var newList = originalList.Remove(originalList[0]);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.True(node2.IsEquivalentTo(originalList[1]));
        Assert.Single(newList);
    }

    [Fact]
    public void Remove_FromEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var emptyList = SyntaxList<SyntaxNode>.Empty;
        var node = CreateMarkupTextLiteral(s_openAngle);

        // Act
        var newList = emptyList.Remove(node);

        // Assert
        Assert.Empty(emptyList);
        Assert.Empty(newList);
    }

    [Fact]
    public void Replace_SingleNodeList_ReplacesNode()
    {
        // Arrange
        var original = CreateMarkupTextLiteral(s_openAngle);
        var replacement = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [original];

        // Act
        var newList = originalList.Replace(originalList[0], replacement);

        // Assert
        Assert.Single(originalList);
        Assert.Single(newList);
        Assert.True(original.IsEquivalentTo(originalList[0]));
        Assert.True(replacement.IsEquivalentTo(newList[0]));
    }

    [Fact]
    public void Replace_MultiNodeList_ReplacesCorrectNode()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];
        var replacement = CreateMarkupTextLiteral(s_leftBrace);

        // Act
        var newList = originalList.Replace(originalList[1], replacement);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(3, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(replacement.IsEquivalentTo(newList[1]));
        Assert.True(node3.IsEquivalentTo(newList[2]));
    }

    [Fact]
    public void Replace_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var replacement = CreateMarkupTextLiteral(s_closeAngle);

        // Act
        var newList = originalList.Replace(originalList[0], replacement);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.True(node2.IsEquivalentTo(originalList[1]));
        Assert.Equal(2, newList.Count);
    }

    [Fact]
    public void Replace_ThrowsArgumentNullException_WhenNodeInListIsNull()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];
        var replacement = CreateMarkupTextLiteral(s_closeAngle);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.Replace(null!, replacement));
    }

    [Fact]
    public void Replace_ThrowsArgumentNullException_WhenNewNodeIsNull()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.Replace(list[0], null!));
    }

    [Fact]
    public void Replace_ThrowsArgumentOutOfRangeException_WhenNodeNotInList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        var nodeNotInList = CreateMarkupTextLiteral(s_forwardSlash);
        var replacement = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Replace(nodeNotInList, replacement));
    }

    [Fact]
    public void Replace_DelegatesToReplaceRange_WithSingleElementArray()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var replacement = CreateMarkupTextLiteral(s_closeAngle);

        // Act
        var replaceResult = originalList.Replace(originalList[0], replacement);
        var replaceRangeResult = originalList.ReplaceRange(originalList[0], [replacement]);

        // Assert
        Assert.Equal(replaceResult.Count, replaceRangeResult.Count);
        for (var i = 0; i < replaceResult.Count; i++)
        {
            Assert.True(replaceResult[i].IsEquivalentTo(replaceRangeResult[i]));
        }
    }

    [Fact]
    public void ReplaceRange_WithReadOnlySpan_SingleNodeList_ReplacesNodeWithMultipleNodes()
    {
        // Arrange
        var original = CreateMarkupTextLiteral(s_openAngle);
        SyntaxNode[] replacements = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];
        SyntaxList<SyntaxNode> originalList = [original];

        // Act
        var newList = originalList.ReplaceRange(originalList[0], replacements.AsSpan());

        // Assert
        Assert.Single(originalList);
        Assert.Equal(2, newList.Count);
        Assert.True(replacements[0].IsEquivalentTo(newList[0]));
        Assert.True(replacements[1].IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void ReplaceRange_WithReadOnlySpan_MultiNodeList_ReplacesCorrectNodeWithMultipleNodes()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];
        SyntaxNode[] replacements = [
            CreateMarkupTextLiteral(s_leftBrace),
            CreateMarkupTextLiteral(s_rightBrace)
        ];

        // Act
        var newList = originalList.ReplaceRange(originalList[1], replacements.AsSpan());

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(4, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(replacements[0].IsEquivalentTo(newList[1]));
        Assert.True(replacements[1].IsEquivalentTo(newList[2]));
        Assert.True(node3.IsEquivalentTo(newList[3]));
    }

    [Fact]
    public void ReplaceRange_WithReadOnlySpan_MultiNodeList_ReplacesNodeWithEmptySpan()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.ReplaceRange(originalList[1], []);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node3.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void ReplaceRange_WithReadOnlySpan_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        SyntaxNode[] replacements = [
            CreateMarkupTextLiteral(s_leftBrace),
            CreateMarkupTextLiteral(s_rightBrace)
        ];

        // Act
        var newList = originalList.ReplaceRange(originalList[0], replacements.AsSpan());

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.True(node2.IsEquivalentTo(originalList[1]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void ReplaceRange_WithReadOnlySpan_ThrowsArgumentOutOfRangeException_WhenNodeNotInList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        var nodeNotInList = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxNode[] replacements = [CreateMarkupTextLiteral(s_closeAngle)];
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(nodeNotInList, replacements.AsSpan()));
    }

    [Fact]
    public void ReplaceRange_SingleNodeList_ReplacesNodeWithMultipleNodes()
    {
        // Arrange
        var original = CreateMarkupTextLiteral(s_openAngle);
        var replacement1 = CreateMarkupTextLiteral(s_forwardSlash);
        var replacement2 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [original];

        // Act
        var newList = originalList.ReplaceRange(originalList[0], [replacement1, replacement2]);

        // Assert
        Assert.Single(originalList);
        Assert.Equal(2, newList.Count);
        Assert.True(replacement1.IsEquivalentTo(newList[0]));
        Assert.True(replacement2.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void ReplaceRange_MultiNodeList_ReplacesCorrectNodeWithMultipleNodes()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];
        var replacement1 = CreateMarkupTextLiteral(s_leftBrace);
        var replacement2 = CreateMarkupTextLiteral(s_rightBrace);

        // Act
        var newList = originalList.ReplaceRange(originalList[1], [replacement1, replacement2]);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(4, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(replacement1.IsEquivalentTo(newList[1]));
        Assert.True(replacement2.IsEquivalentTo(newList[2]));
        Assert.True(node3.IsEquivalentTo(newList[3]));
    }

    [Fact]
    public void ReplaceRange_MultiNodeList_ReplacesNodeWithEmptyList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        var node3 = CreateMarkupTextLiteral(s_closeAngle);
        SyntaxList<SyntaxNode> originalList = [node1, node2, node3];

        // Act
        var newList = originalList.ReplaceRange(originalList[1], []);

        // Assert
        Assert.Equal(3, originalList.Count);
        Assert.Equal(2, newList.Count);
        Assert.True(node1.IsEquivalentTo(newList[0]));
        Assert.True(node3.IsEquivalentTo(newList[1]));
    }

    [Fact]
    public void ReplaceRange_ImplementsImmutability_DoesNotModifyOriginalList()
    {
        // Arrange
        var node1 = CreateMarkupTextLiteral(s_openAngle);
        var node2 = CreateMarkupTextLiteral(s_forwardSlash);
        SyntaxList<SyntaxNode> originalList = [node1, node2];
        var replacement1 = CreateMarkupTextLiteral(s_leftBrace);
        var replacement2 = CreateMarkupTextLiteral(s_rightBrace);

        // Act
        var newList = originalList.ReplaceRange(originalList[0], [replacement1, replacement2]);

        // Assert
        Assert.Equal(2, originalList.Count);
        Assert.True(node1.IsEquivalentTo(originalList[0]));
        Assert.True(node2.IsEquivalentTo(originalList[1]));
        Assert.Equal(3, newList.Count);
    }

    [Fact]
    public void ReplaceRange_ThrowsArgumentNullException_WhenNodeInListIsNull()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];
        var replacements = new[] { CreateMarkupTextLiteral(s_closeAngle) };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(null!, replacements));
    }

    [Fact]
    public void ReplaceRange_ThrowsArgumentNullException_WhenNewNodesIsNull()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];
        IEnumerable<SyntaxNode> newNodes = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => list.ReplaceRange(node, newNodes));
    }

    [Fact]
    public void ReplaceRange_ThrowsArgumentOutOfRangeException_WhenNodeNotInList()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        var nodeNotInList = CreateMarkupTextLiteral(s_forwardSlash);
        var replacements = new[] { CreateMarkupTextLiteral(s_closeAngle) };
        SyntaxList<SyntaxNode> list = [node];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => list.ReplaceRange(nodeNotInList, replacements));
    }

    [Fact]
    public void ReplaceRange_WithListAsSource_ReplacesNodeCorrectly()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];
        List<SyntaxNode> replacements = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = list.ReplaceRange(list[0], replacements);

        // Assert
        Assert.Equal(replacements.Count, newList.Count);
        for (var i = 0; i < replacements.Count; i++)
        {
            Assert.True(replacements[i].IsEquivalentTo(newList[i]));
        }
    }

    [Fact]
    public void ReplaceRange_WithIEnumerableAsSource_ReplacesNodeCorrectly()
    {
        // Arrange
        var node = CreateMarkupTextLiteral(s_openAngle);
        SyntaxList<SyntaxNode> list = [node];
        IEnumerable<SyntaxNode> replacements = [
            CreateMarkupTextLiteral(s_forwardSlash),
            CreateMarkupTextLiteral(s_closeAngle)
        ];

        // Act
        var newList = list.ReplaceRange(list[0], replacements);

        // Assert
        Assert.Equal(2, newList.Count);
        var index = 0;
        foreach (var replacement in replacements)
        {
            Assert.True(replacement.IsEquivalentTo(newList[index++]));
        }
    }
}
