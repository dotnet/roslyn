// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.PooledObjects;

public class PooledHashSetTests
{
    [Fact]
    public void Constructor_Default_CreatesEmptySet()
    {
        using var set = new PooledHashSet<int>();

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Constructor_WithCapacity_CreatesEmptySet()
    {
        using var set = new PooledHashSet<int>(10);

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Constructor_WithComparer_CreatesEmptySet()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Constructor_WithComparerAndCapacity_CreatesEmptySet()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase, 10);

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Constructor_WithPool_CreatesEmptySet()
    {
        var pool = HashSetPool<int>.Default;
        using var set = new PooledHashSet<int>(pool);

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Constructor_WithPoolAndCapacity_CreatesEmptySet()
    {
        var pool = HashSetPool<int>.Default;
        using var set = new PooledHashSet<int>(pool, 10);

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Add_SingleItem_ReturnsTrue()
    {
        using var set = new PooledHashSet<int>();

        var result = set.Add(42);

        Assert.True(result);
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void Add_DuplicateSingleItem_ReturnsFalse()
    {
        using var set = new PooledHashSet<int>();

        set.Add(42);
        var result = set.Add(42);

        Assert.False(result);
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void Add_TwoItems_CreatesHashSet()
    {
        using var set = new PooledHashSet<int>();

        var result1 = set.Add(42);
        var result2 = set.Add(24);

        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void Add_DuplicateInHashSet_ReturnsFalse()
    {
        using var set = new PooledHashSet<int>();

        set.Add(42);
        set.Add(24);
        var result = set.Add(42);

        Assert.False(result);
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void Add_WithCustomComparer_UsesSameComparerForSingleItem()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);

        set.Add("Hello");
        var result = set.Add("HELLO");

        Assert.False(result);
        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void Add_WithCustomComparer_UsesSameComparerForHashSet()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);

        set.Add("Hello");
        set.Add("World");
        var result = set.Add("HELLO");

        Assert.False(result);
        Assert.Equal(2, set.Count);
    }

    [Fact]
    public void Contains_EmptySet_ReturnsFalse()
    {
        using var set = new PooledHashSet<int>();

        var result = set.Contains(42);

        Assert.False(result);
    }

    [Fact]
    public void Contains_SingleItem_Exists_ReturnsTrue()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        var result = set.Contains(42);

        Assert.True(result);
    }

    [Fact]
    public void Contains_SingleItem_DoesNotExist_ReturnsFalse()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        var result = set.Contains(24);

        Assert.False(result);
    }

    [Fact]
    public void Contains_HashSet_Exists_ReturnsTrue()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);

        var result = set.Contains(42);

        Assert.True(result);
    }

    [Fact]
    public void Contains_HashSet_DoesNotExist_ReturnsFalse()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);

        var result = set.Contains(99);

        Assert.False(result);
    }

    [Fact]
    public void Contains_WithCustomComparer_UsesSameComparerForSingleItem()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");

        var result = set.Contains("HELLO");

        Assert.True(result);
    }

    [Fact]
    public void Contains_WithCustomComparer_UsesSameComparerForHashSet()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);
        set.Add("Hello");
        set.Add("World");

        var result = set.Contains("HELLO");

        Assert.True(result);
    }

    [Fact]
    public void Count_EmptySet_ReturnsZero()
    {
        using var set = new PooledHashSet<int>();

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void Count_SingleItem_ReturnsOne()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        Assert.Equal(1, set.Count);
    }

    [Fact]
    public void Count_MultipleItems_ReturnsCorrectCount()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);
        set.Add(99);

        Assert.Equal(3, set.Count);
    }

    [Fact]
    public void ToArray_EmptySet_ReturnsEmptyArray()
    {
        using var set = new PooledHashSet<int>();

        var result = set.ToArray();

        Assert.Empty(result);
    }

    [Fact]
    public void ToArray_SingleItem_ReturnsArrayWithSingleItem()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        var result = set.ToArray();

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }

    [Fact]
    public void ToArray_MultipleItems_ReturnsArrayWithAllItems()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);
        set.Add(99);

        var result = set.ToArray();

        Assert.Equal(3, result.Length);
        Assert.Contains(42, result);
        Assert.Contains(24, result);
        Assert.Contains(99, result);
    }

    [Fact]
    public void ToImmutableArray_EmptySet_ReturnsEmptyImmutableArray()
    {
        using var set = new PooledHashSet<int>();

        var result = set.ToImmutableArray();

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void ToImmutableArray_SingleItem_ReturnsImmutableArrayWithSingleItem()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        var result = set.ToImmutableArray();

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }

    [Fact]
    public void ToImmutableArray_MultipleItems_ReturnsImmutableArrayWithAllItems()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);
        set.Add(99);

        var result = set.ToImmutableArray();

        Assert.Equal(3, result.Length);
        Assert.Contains(42, result);
        Assert.Contains(24, result);
        Assert.Contains(99, result);
    }

    [Fact]
    public void OrderByAsArray_EmptySet_ReturnsEmptyImmutableArray()
    {
        using var set = new PooledHashSet<int>();

        var result = set.OrderByAsArray(x => x);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void OrderByAsArray_SingleItem_ReturnsImmutableArrayWithSingleItem()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        var result = set.OrderByAsArray(x => x);

        Assert.Single(result);
        Assert.Equal(42, result[0]);
    }

    [Fact]
    public void OrderByAsArray_MultipleItems_ReturnsOrderedImmutableArray()
    {
        using var set = new PooledHashSet<int>();
        set.Add(99);
        set.Add(24);
        set.Add(42);

        var result = set.OrderByAsArray(x => x);

        Assert.Equal(3, result.Length);
        Assert.Equal(24, result[0]);
        Assert.Equal(42, result[1]);
        Assert.Equal(99, result[2]);
    }

    [Fact]
    public void UnionWith_ImmutableArray_Empty_NoChange()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        ImmutableArray<int> other = [];
        set.UnionWith(other);

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ImmutableArray_Default_NoChange()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        ImmutableArray<int> other = default;
        set.UnionWith(other);

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ImmutableArray_SingleItem_AddsItem()
    {
        using var set = new PooledHashSet<int>();

        ImmutableArray<int> other = [42];
        set.UnionWith(other);

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ImmutableArray_MultipleItems_AddsAllItems()
    {
        using var set = new PooledHashSet<int>();

        ImmutableArray<int> other = [42, 24, 99];
        set.UnionWith(other);

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(42));
        Assert.True(set.Contains(24));
        Assert.True(set.Contains(99));
    }

    [Fact]
    public void UnionWith_ImmutableArray_WithExistingItems_UnionCorrectly()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        ImmutableArray<int> other = [24, 99, 42];
        set.UnionWith(other);

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(42));
        Assert.True(set.Contains(24));
        Assert.True(set.Contains(99));
    }

    [Fact]
    public void UnionWith_ReadOnlyList_Null_NoChange()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        IReadOnlyList<int>? other = null;
        set.UnionWith(other);

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ReadOnlyList_Empty_NoChange()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        set.UnionWith(Array.Empty<int>());

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ReadOnlyList_SingleItem_AddsItem()
    {
        using var set = new PooledHashSet<int>();

        int[] other = [42];
        set.UnionWith(other);

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains(42));
    }

    [Fact]
    public void UnionWith_ReadOnlyList_MultipleItems_AddsAllItems()
    {
        using var set = new PooledHashSet<int>();

        int[] other = [42, 24, 99];
        set.UnionWith(other);

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(42));
        Assert.True(set.Contains(24));
        Assert.True(set.Contains(99));
    }

    [Fact]
    public void UnionWith_ReadOnlyList_WithExistingItems_UnionCorrectly()
    {
        using var set = new PooledHashSet<int>();
        set.Add(42);

        int[] other = [24, 99, 42];
        set.UnionWith(other);

        Assert.Equal(3, set.Count);
        Assert.True(set.Contains(42));
        Assert.True(set.Contains(24));
        Assert.True(set.Contains(99));
    }

    [Fact]
    public void ClearAndFree_EmptySet_DoesNotThrow()
    {
        var set = new PooledHashSet<int>();

        set.ClearAndFree();

        Assert.Equal(0, set.Count);
    }

    [Fact]
    public void ClearAndFree_SingleItem_ClearsSet()
    {
        var set = new PooledHashSet<int>();
        set.Add(42);

        set.ClearAndFree();

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(42));
    }

    [Fact]
    public void ClearAndFree_MultipleItems_ClearsSet()
    {
        var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);

        set.ClearAndFree();

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(42));
        Assert.False(set.Contains(24));
    }

    [Fact]
    public void Dispose_CallsClearAndFree()
    {
        var set = new PooledHashSet<int>();
        set.Add(42);
        set.Add(24);

        set.Dispose();

        Assert.Equal(0, set.Count);
        Assert.False(set.Contains(42));
        Assert.False(set.Contains(24));
    }

    [Fact]
    public void UsingStatement_AutomaticallyDisposesSet()
    {
        PooledHashSet<int> capturedSet;

        using (var set = new PooledHashSet<int>())
        {
            set.Add(42);
            set.Add(24);
            capturedSet = set;
            Assert.Equal(2, capturedSet.Count);
        }

        // After using statement, set should be disposed
        Assert.Equal(0, capturedSet.Count);
    }

    [Fact]
    public void MultipleOperations_WorkCorrectly()
    {
        using var set = new PooledHashSet<string>();

        // Start with single item optimization
        Assert.True(set.Add("first"));
        Assert.Equal(1, set.Count);
        Assert.True(set.Contains("first"));

        // Transition to HashSet
        Assert.True(set.Add("second"));
        Assert.Equal(2, set.Count);
        Assert.True(set.Contains("first"));
        Assert.True(set.Contains("second"));

        // Add more items
        Assert.True(set.Add("third"));
        Assert.False(set.Add("first")); // Duplicate
        Assert.Equal(3, set.Count);

        // Union with array
        string[] other = ["fourth", "first", "fifth"];
        set.UnionWith(other);
        Assert.Equal(5, set.Count);

        // Check final state
        var array = set.ToArray();
        Assert.Equal(5, array.Length);
        Assert.Contains("first", array);
        Assert.Contains("second", array);
        Assert.Contains("third", array);
        Assert.Contains("fourth", array);
        Assert.Contains("fifth", array);
    }

    [Fact]
    public void StringComparer_Ordinal_WorksCorrectly()
    {
        using var set = new PooledHashSet<string>(StringComparer.Ordinal);

        set.Add("Hello");
        set.Add("hello");

        Assert.Equal(2, set.Count);
        Assert.True(set.Contains("Hello"));
        Assert.True(set.Contains("hello"));
        Assert.False(set.Contains("HELLO"));
    }

    [Fact]
    public void StringComparer_OrdinalIgnoreCase_WorksCorrectly()
    {
        using var set = new PooledHashSet<string>(StringComparer.OrdinalIgnoreCase);

        set.Add("Hello");
        Assert.False(set.Add("hello"));
        Assert.False(set.Add("HELLO"));

        Assert.Equal(1, set.Count);
        Assert.True(set.Contains("Hello"));
        Assert.True(set.Contains("hello"));
        Assert.True(set.Contains("HELLO"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void Capacity_Constructor_DoesNotAffectFunctionality(int capacity)
    {
        using var set = new PooledHashSet<int>(capacity);

        for (var i = 0; i < 50; i++)
        {
            set.Add(i);
        }

        Assert.Equal(50, set.Count);

        for (var i = 0; i < 50; i++)
        {
            Assert.True(set.Contains(i));
        }
    }

    [Fact]
    public void OrderByAsArray_WithComplexKeySelector_WorksCorrectly()
    {
        using var set = new PooledHashSet<string>();
        set.Add("apple");
        set.Add("banana");
        set.Add("cherry");

        var result = set.OrderByAsArray(s => s.Length);

        Assert.Equal(3, result.Length);
        Assert.Equal("apple", result[0]);  // Length 5
        Assert.Equal("banana", result[1]); // Length 6
        Assert.Equal("cherry", result[2]); // Length 6 (stable sort)
    }
}
