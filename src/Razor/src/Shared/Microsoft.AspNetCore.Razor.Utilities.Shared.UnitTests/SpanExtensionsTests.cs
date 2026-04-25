// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class SpanExtensionsTests
{
    [Fact]
    public void Reversed_Span_BasicEnumeration()
    {
        // Arrange
        Span<int> span = [1, 2, 3, 4, 5];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([5, 4, 3, 2, 1], result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_BasicEnumeration()
    {
        // Arrange
        ReadOnlySpan<int> span = [1, 2, 3, 4, 5];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([5, 4, 3, 2, 1], result);
    }

    [Fact]
    public void Reversed_Span_EmptySpan_NoIteration()
    {
        // Arrange
        Span<int> span = [];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_EmptySpan_NoIteration()
    {
        // Arrange
        ReadOnlySpan<int> span = [];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Reversed_Span_SingleElement()
    {
        // Arrange
        Span<int> span = [42];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([42], result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_SingleElement()
    {
        // Arrange
        ReadOnlySpan<int> span = [42];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([42], result);
    }

    [Fact]
    public void Reversed_Span_TwoElements()
    {
        // Arrange
        Span<int> span = [1, 2];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([2, 1], result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_TwoElements()
    {
        // Arrange
        ReadOnlySpan<int> span = [1, 2];

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([2, 1], result);
    }

    [Fact]
    public void Reversed_Span_StringElements()
    {
        // Arrange
        Span<string> span = ["first", "second", "third"];

        // Act
        var result = new List<string>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(["third", "second", "first"], result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_StringElements()
    {
        // Arrange
        ReadOnlySpan<string> span = ["first", "second", "third"];

        // Act
        var result = new List<string>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(["third", "second", "first"], result);
    }

    [Fact]
    public void Reversed_Enumerator_Current_ValidAfterMoveNext()
    {
        // Arrange
        Span<int> span = [10, 20, 30];
        var enumerator = span.Reversed.GetEnumerator();

        // Act & Assert
        Assert.True(enumerator.MoveNext());
        Assert.Equal(30, enumerator.Current);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(20, enumerator.Current);

        Assert.True(enumerator.MoveNext());
        Assert.Equal(10, enumerator.Current);

        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_Enumerator_MoveNext_EmptySpan_ReturnsFalse()
    {
        // Arrange
        Span<int> span = [];
        var enumerator = span.Reversed.GetEnumerator();

        // Act & Assert
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_Enumerator_Reset_RestoresInitialState()
    {
        // Arrange
        Span<int> span = [1, 2, 3];
        var enumerator = span.Reversed.GetEnumerator();

        // Move through some elements
        Assert.True(enumerator.MoveNext());
        Assert.Equal(3, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current);

        // Act - Reset
        enumerator.Reset();

        // Assert - Should start over
        Assert.True(enumerator.MoveNext());
        Assert.Equal(3, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(2, enumerator.Current);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(1, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_Enumerator_Reset_EmptySpan()
    {
        // Arrange
        Span<int> span = [];
        var enumerator = span.Reversed.GetEnumerator();

        // Act
        enumerator.Reset();

        // Assert
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_Enumerator_Reset_SingleElement()
    {
        // Arrange
        Span<int> span = [42];
        var enumerator = span.Reversed.GetEnumerator();

        // Move through the element
        Assert.True(enumerator.MoveNext());
        Assert.Equal(42, enumerator.Current);
        Assert.False(enumerator.MoveNext());

        // Act - Reset
        enumerator.Reset();

        // Assert - Should start over
        Assert.True(enumerator.MoveNext());
        Assert.Equal(42, enumerator.Current);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_MultipleEnumerators_Independent()
    {
        // Arrange
        Span<int> span = [1, 2, 3];
        var enumerator1 = span.Reversed.GetEnumerator();
        var enumerator2 = span.Reversed.GetEnumerator();

        // Act & Assert - Enumerators should be independent
        Assert.True(enumerator1.MoveNext());
        Assert.Equal(3, enumerator1.Current);

        Assert.True(enumerator2.MoveNext());
        Assert.Equal(3, enumerator2.Current);

        Assert.True(enumerator1.MoveNext());
        Assert.Equal(2, enumerator1.Current);

        // enumerator2 should still be at the first position
        Assert.True(enumerator2.MoveNext());
        Assert.Equal(2, enumerator2.Current);
    }

    [Fact]
    public void Reversed_Span_LargeSpan()
    {
        // Arrange
        var array = new int[1000];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        Span<int> span = array;

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(1000, result.Count);
        Assert.Equal(999, result[0]);
        Assert.Equal(998, result[1]);
        Assert.Equal(0, result[999]);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_LargeSpan()
    {
        // Arrange
        var array = new int[1000];
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        ReadOnlySpan<int> span = array;

        // Act
        var result = new List<int>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(1000, result.Count);
        Assert.Equal(999, result[0]);
        Assert.Equal(998, result[1]);
        Assert.Equal(0, result[999]);
    }

    [Fact]
    public void Reversed_Span_ReferenceTypes_NullElements()
    {
        // Arrange
        Span<string?> span = ["first", null, "third"];

        // Act
        var result = new List<string?>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(["third", null, "first"], result);
    }

    [Fact]
    public void Reversed_ReadOnlySpan_ReferenceTypes_NullElements()
    {
        // Arrange
        ReadOnlySpan<string?> span = ["first", null, "third"];

        // Act
        var result = new List<string?>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal(["third", null, "first"], result);
    }

    [Fact]
    public void Reversed_NestedForeach_Works()
    {
        // Arrange
        Span<int> span1 = [1, 2];
        Span<int> span2 = [3, 4];

        // Act
        var result = new List<(int, int)>();
        foreach (var item1 in span1.Reversed)
        {
            foreach (var item2 in span2.Reversed)
            {
                result.Add((item1, item2));
            }
        }

        // Assert
        Assert.Equal([(2, 4), (2, 3), (1, 4), (1, 3)], result);
    }

    [Fact]
    public void Reversed_ManualEnumerator_Usage()
    {
        // Arrange
        ReadOnlySpan<char> span = ['a', 'b', 'c'];
        var enumerator = span.Reversed.GetEnumerator();

        // Act & Assert - Manual enumeration
        var result = new List<char>();
        while (enumerator.MoveNext())
        {
            result.Add(enumerator.Current);
        }

        Assert.Equal(['c', 'b', 'a'], result);

        // Trying to move next after completion should return false
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void Reversed_ValueTypes_StructElements()
    {
        // Arrange
        var point1 = new System.Drawing.Point(1, 2);
        var point2 = new System.Drawing.Point(3, 4);
        var point3 = new System.Drawing.Point(5, 6);
        Span<System.Drawing.Point> span = [point1, point2, point3];

        // Act
        var result = new List<System.Drawing.Point>();
        foreach (var item in span.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([point3, point2, point1], result);
    }

    [Fact]
    public void Reversed_ImplicitConversion_SpanToReadOnlySpan()
    {
        // Arrange
        Span<int> span = [1, 2, 3];

        // Act - Implicit conversion to ReadOnlySpan<T>
        ReadOnlySpan<int> readOnlySpan = span;
        var result = new List<int>();
        foreach (var item in readOnlySpan.Reversed)
        {
            result.Add(item);
        }

        // Assert
        Assert.Equal([3, 2, 1], result);
    }
}
