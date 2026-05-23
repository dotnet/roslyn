// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.PooledObjects;

public class PooledArrayBuilderOrderingTests : ImmutableArrayOrderingTestBase
{
    [Theory]
    [MemberData(nameof(OrderTestData))]
    public void ToImmutableOrdered(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrdered();
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderTestData_OddBeforeEven))]
    public void ToImmutableOrdered_OddBeforeEven(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrdered(OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData))]
    public void ToImmutableOrderedDescending(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedDescending();
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData_OddBeforeEven))]
    public void ToImmutableOrderedDescending_OddBeforeEven(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedDescending(OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData))]
    public void ToImmutableOrderedBy(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedBy(static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData_OddBeforeEven))]
    public void ToImmutableOrderedBy_OddBeforeEven(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedBy(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData))]
    public void ToImmutableOrderedByDescending(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByDescending(static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData_OddBeforeEven))]
    public void ToImmutableOrderedByDescending_OddBeforeEven(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByDescending(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderTestData))]
    public void ToImmutableOrderedAndClear(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedAndClear();
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderTestData_OddBeforeEven))]
    public void ToImmutableOrderedAndClear_OddBeforeEven(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedAndClear(OddBeforeEven);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData))]
    public void ToImmutableOrderedDescendingAndClear(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedDescendingAndClear();
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData_OddBeforeEven))]
    public void ToImmutableOrderedDescendingAndClear_OddBeforeEven(ImmutableArray<int> data, ImmutableArray<int> expected)
    {
        var builderPool = TestArrayBuilderPool<int>.Create();
        using var builder = new PooledArrayBuilder<int>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedDescendingAndClear(OddBeforeEven);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData))]
    public void ToImmutableOrderedByAndClear(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var builderPool = TestArrayBuilderPool<ValueHolder<int>>.Create();
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByAndClear(static x => x.Value);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData_OddBeforeEven))]
    public void ToImmutableOrderedByAndClear_OddBeforeEven(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var builderPool = TestArrayBuilderPool<ValueHolder<int>>.Create();
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByAndClear(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData))]
    public void ToImmutableOrderedByDescendingAndClear(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var builderPool = TestArrayBuilderPool<ValueHolder<int>>.Create();
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByDescendingAndClear(static x => x.Value);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData_OddBeforeEven))]
    public void ToImmutableOrderedByDescendingAndClear_OddBeforeEven(ImmutableArray<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var builderPool = TestArrayBuilderPool<ValueHolder<int>>.Create();
        using var builder = new PooledArrayBuilder<ValueHolder<int>>(capacity: data.Length, builderPool);
        builder.AddRange(data);

        var sorted = builder.ToImmutableOrderedByDescendingAndClear(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
        AssertIsDrained(in builder);
    }

    private static void AssertIsDrained<T>(ref readonly PooledArrayBuilder<T> builder)
    {
        builder.Validate(static t =>
        {
            Assert.NotNull(t.InnerArrayBuilder);
            Assert.Empty(t.InnerArrayBuilder);

            // After draining, the capacity of the inner array builder should be 0.
            Assert.Equal(0, t.InnerArrayBuilder.Capacity);
        });
    }
}
