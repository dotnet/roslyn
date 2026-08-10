// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

public class InlineListTests
{
    private static BasicIntermediateNode CreateNode(string name) => new(name);

    [Fact]
    public void Count_IsZero_WhenEmpty()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();

        // Assert
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Add_SingleItem_StoresInline()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node = CreateNode("Node1");

        // Act
        collection.Add(node);

        // Assert
        Assert.Equal(1, collection.Count);
        Assert.Same(node, collection[0]);
    }

    [Fact]
    public void Add_TwoItems_TransitionsToArray()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");

        // Act
        collection.Add(node1);
        collection.Add(node2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Same(node1, collection[0]);
        Assert.Same(node2, collection[1]);
    }

    [Fact]
    public void Add_MultipleItems_GrowsArray()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var nodes = new[]
        {
            CreateNode("Node1"),
            CreateNode("Node2"),
            CreateNode("Node3"),
            CreateNode("Node4"),
            CreateNode("Node5"),
        };

        // Act
        foreach (var node in nodes)
        {
            collection.Add(node);
        }

        // Assert
        Assert.Equal(5, collection.Count);
        for (int i = 0; i < nodes.Length; i++)
        {
            Assert.Same(nodes[i], collection[i]);
        }
    }

    [Theory]
    [InlineData(0, 0)] // Empty collection, index 0
    [InlineData(1, 1)] // Single item, index 1
    [InlineData(1, -1)] // Single item, negative index
    public void Indexer_Get_ThrowsForInvalidIndex(int itemCount, int invalidIndex)
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        for (int i = 0; i < itemCount; i++)
        {
            collection.Add(CreateNode($"Node{i}"));
        }

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => collection[invalidIndex]);
    }

    [Fact]
    public void Indexer_Set_UpdatesSingleItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        collection.Add(node1);

        // Act
        collection[0] = node2;

        // Assert
        Assert.Same(node2, collection[0]);
    }

    [Fact]
    public void Indexer_Set_UpdatesArrayItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        collection.Add(CreateNode("Node1"));
        collection.Add(CreateNode("Node2"));
        collection.Add(CreateNode("Node3"));
        var replacement = CreateNode("Replacement");

        // Act
        collection[1] = replacement;

        // Assert
        Assert.Same(replacement, collection[1]);
    }

    [Fact]
    public void Insert_AtIndexZero_WhenEmpty()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node = CreateNode("Node1");

        // Act
        collection.Insert(0, node);

        // Assert
        Assert.Equal(1, collection.Count);
        Assert.Same(node, collection[0]);
    }

    [Fact]
    public void Insert_AtIndexZero_BeforeSingleItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        collection.Add(node1);

        // Act
        collection.Insert(0, node2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Same(node2, collection[0]);
        Assert.Same(node1, collection[1]);
    }

    [Fact]
    public void Insert_AtIndexOne_AfterSingleItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        collection.Add(node1);

        // Act
        collection.Insert(1, node2);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Same(node1, collection[0]);
        Assert.Same(node2, collection[1]);
    }

    [Fact]
    public void Insert_InMiddle_ShiftsItems()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        var node3 = CreateNode("Node3");
        collection.Add(node1);
        collection.Add(node3);

        // Act
        collection.Insert(1, node2);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Same(node1, collection[0]);
        Assert.Same(node2, collection[1]);
        Assert.Same(node3, collection[2]);
    }

    [Fact]
    public void Insert_AtEnd_AppendsItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        collection.Add(CreateNode("Node1"));
        collection.Add(CreateNode("Node2"));
        var node3 = CreateNode("Node3");

        // Act
        collection.Insert(2, node3);

        // Assert
        Assert.Equal(3, collection.Count);
        Assert.Same(node3, collection[2]);
    }

    [Fact]
    public void RemoveAt_SingleItem_BecomesEmpty()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        collection.Add(CreateNode("Node1"));

        // Act
        collection.RemoveAt(0);

        // Assert
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void RemoveAt_FromArray_ShiftsItems()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        var node3 = CreateNode("Node3");
        collection.Add(node1);
        collection.Add(node2);
        collection.Add(node3);

        // Act
        collection.RemoveAt(1);

        // Assert
        Assert.Equal(2, collection.Count);
        Assert.Same(node1, collection[0]);
        Assert.Same(node3, collection[1]);
    }

    [Fact]
    public void RemoveAt_LastItemInArray_TransitionsToInline()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        collection.Add(node1);
        collection.Add(node2);

        // Act
        collection.RemoveAt(0);
        collection.RemoveAt(0);

        // Assert
        Assert.Equal(0, collection.Count);
    }

    [Theory]
    [InlineData(0)] // Empty collection
    [InlineData(1)] // Single item (inline mode)
    [InlineData(3)] // Array mode
    public void Clear_BecomesEmpty(int initialItemCount)
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        for (int i = 0; i < initialItemCount; i++)
        {
            collection.Add(CreateNode($"Node{i}"));
        }

        // Act
        collection.Clear();

        // Assert
        Assert.Equal(0, collection.Count);
    }

    [Theory]
    [InlineData(0, -1, -1)] // Empty collection, searching for any item
    [InlineData(1, 0, 0)] // Single item, found at index 0
    [InlineData(1, -1, -1)] // Single item, not found
    [InlineData(3, 1, 1)] // Array mode, found at index 1
    [InlineData(3, -1, -1)] // Array mode, not found
    public void IndexOf_ReturnsCorrectIndex(int collectionSize, int searchIndex, int expectedResult)
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var nodes = new IntermediateNode[collectionSize];
        for (int i = 0; i < collectionSize; i++)
        {
            nodes[i] = CreateNode($"Node{i}");
            collection.Add(nodes[i]);
        }

        var searchNode = searchIndex >= 0 ? nodes[searchIndex] : CreateNode("NotInCollection");

        // Act
        var index = collection.IndexOf(searchNode);

        // Assert
        Assert.Equal(expectedResult, index);
    }

    [Fact]
    public void CopyTo_WhenEmpty_DoesNothing()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var array = new IntermediateNode[5];

        // Act
        collection.CopyTo(array, 0);

        // Assert
        Assert.All(array, item => Assert.Null(item));
    }

    [Fact]
    public void CopyTo_SingleItem_CopiesItem()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node = CreateNode("Node1");
        collection.Add(node);
        var array = new IntermediateNode[5];

        // Act
        collection.CopyTo(array, 2);

        // Assert
        Assert.Null(array[0]);
        Assert.Null(array[1]);
        Assert.Same(node, array[2]);
        Assert.Null(array[3]);
        Assert.Null(array[4]);
    }

    [Fact]
    public void CopyTo_Array_CopiesAllItems()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var node1 = CreateNode("Node1");
        var node2 = CreateNode("Node2");
        var node3 = CreateNode("Node3");
        collection.Add(node1);
        collection.Add(node2);
        collection.Add(node3);
        var array = new IntermediateNode[5];

        // Act
        collection.CopyTo(array, 1);

        // Assert
        Assert.Null(array[0]);
        Assert.Same(node1, array[1]);
        Assert.Same(node2, array[2]);
        Assert.Same(node3, array[3]);
        Assert.Null(array[4]);
    }

    [Fact]
    public void ArrayPooling_GrowsAndReturnsArrays()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();

        // Act - grow beyond initial capacity (4) to trigger Grow
        for (int i = 0; i < 10; i++)
        {
            collection.Add(CreateNode($"Node{i}"));
        }

        // Assert - verify all items are accessible
        Assert.Equal(10, collection.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.NotNull(collection[i]);
        }

        // Act - clear should return array to pool
        collection.Clear();

        // Assert
        Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void StressTest_ManyOperations()
    {
        // Arrange
        var collection = new IntermediateNodeCollection();
        var nodes = new IntermediateNode[100];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = CreateNode($"Node{i}");
        }

        // Act - add all
        foreach (var node in nodes)
        {
            collection.Add(node);
        }

        // Assert
        Assert.Equal(100, collection.Count);

        // Act - remove half
        for (int i = 0; i < 50; i++)
        {
            collection.RemoveAt(0);
        }

        // Assert
        Assert.Equal(50, collection.Count);

        // Act - insert in middle
        var insertNode = CreateNode("InsertedNode");
        collection.Insert(25, insertNode);

        // Assert
        Assert.Equal(51, collection.Count);
        Assert.Same(insertNode, collection[25]);

        // Act - clear all
        collection.Clear();

        // Assert
        Assert.Equal(0, collection.Count);
    }

    private class BasicIntermediateNode : IntermediateNode
    {
        public BasicIntermediateNode(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString() => Name;
    }
}
