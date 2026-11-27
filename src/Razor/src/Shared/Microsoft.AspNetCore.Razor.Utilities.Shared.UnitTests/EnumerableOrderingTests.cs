// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class EnumerableOrderingTests : EnumerableOrderingTestBase
{
    [Theory]
    [MemberData(nameof(OrderTestData))]
    public void OrderAsArray(IEnumerable<int> data, ImmutableArray<int> expected)
    {
        var sorted = data.OrderAsArray();
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderTestData_OddBeforeEven))]
    public void OrderAsArray_OddBeforeEven(IEnumerable<int> data, ImmutableArray<int> expected)
    {
        var sorted = data.OrderAsArray(OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData))]
    public void OrderDescendingAsArray(IEnumerable<int> data, ImmutableArray<int> expected)
    {
        var sorted = data.OrderDescendingAsArray();
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderDescendingTestData_OddBeforeEven))]
    public void OrderDescendingAsArray_OddBeforeEven(IEnumerable<int> data, ImmutableArray<int> expected)
    {
        var sorted = data.OrderDescendingAsArray(OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData))]
    public void OrderByAsArray(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var sorted = data.OrderByAsArray(static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByTestData_OddBeforeEven))]
    public void OrderByAsArray_OddBeforeEven(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var sorted = data.OrderByAsArray(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData))]
    public void OrderByDescendingAsArray(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var sorted = data.OrderByDescendingAsArray(static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(OrderByDescendingTestData_OddBeforeEven))]
    public void OrderByDescendingAsArray_OddBeforeEven(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<int>> expected)
    {
        var sorted = data.OrderByDescendingAsArray(static x => x.Value, OddBeforeEven);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderTestData))]
    public void SelectAndOrderAsArray(IEnumerable<int> data, ImmutableArray<string> expected, Func<int, string> selector)
    {
        var sorted = data.SelectAndOrderAsArray(selector);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderTestData_OddBeforeEven))]
    public void SelectAndOrderAsArray_OddBeforeEven(IEnumerable<int> data, ImmutableArray<string> expected, Func<int, string> selector)
    {
        var sorted = data.SelectAndOrderAsArray(selector, OddBeforeEvenString);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderDescendingTestData))]
    public void SelectAndOrderDescendingAsArray(IEnumerable<int> data, ImmutableArray<string> expected, Func<int, string> selector)
    {
        var sorted = data.SelectAndOrderDescendingAsArray(selector);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderDescendingTestData_OddBeforeEven))]
    public void SelectAndOrderDescendingAsArray_OddBeforeEven(IEnumerable<int> data, ImmutableArray<string> expected, Func<int, string> selector)
    {
        var sorted = data.SelectAndOrderDescendingAsArray(selector, OddBeforeEvenString);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderByTestData))]
    public void SelectAndOrderByAsArray(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<string>> expected, Func<ValueHolder<int>, ValueHolder<string>> selector)
    {
        var sorted = data.SelectAndOrderByAsArray(selector, static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderByTestData_OddBeforeEven))]
    public void SelectAndOrderByAsArray_OddBeforeEven(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<string>> expected, Func<ValueHolder<int>, ValueHolder<string>> selector)
    {
        var sorted = data.SelectAndOrderByAsArray(selector, static x => x.Value, OddBeforeEvenString);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderByDescendingTestData))]
    public void SelectAndOrderByDescendingAsArray(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<string>> expected, Func<ValueHolder<int>, ValueHolder<string>> selector)
    {
        var sorted = data.SelectAndOrderByDescendingAsArray(selector, static x => x.Value);
        AssertEqual(expected, sorted);
    }

    [Theory]
    [MemberData(nameof(SelectAndOrderByDescendingTestData_OddBeforeEven))]
    public void SelectAndOrderByDescendingAsArray_OddBeforeEven(IEnumerable<ValueHolder<int>> data, ImmutableArray<ValueHolder<string>> expected, Func<ValueHolder<int>, ValueHolder<string>> selector)
    {
        var sorted = data.SelectAndOrderByDescendingAsArray(selector, static x => x.Value, OddBeforeEvenString);
        AssertEqual(expected, sorted);
    }

#if NET // Enumerable.Order(...) and Enumerable.OrderDescending(...) were introduced in .NET 7

    [Fact]
    public void OrderAsArray_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.Order(),
            testFunction: data => data.OrderAsArray());
    }

    [Fact]
    public void OrderAsArray_Comparer_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.Order(StringHolder.Comparer.Ordinal),
            testFunction: data => data.OrderAsArray(StringHolder.Comparer.Ordinal));

        OrderAndAssertStableSort(
            linqFunction: data => data.Order(StringHolder.Comparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderAsArray(StringHolder.Comparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void OrderDescendingAsArray_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderDescending(),
            testFunction: data => data.OrderDescendingAsArray());
    }

    [Fact]
    public void OrderDescendingAsArray_Comparer_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderDescending(StringHolder.Comparer.Ordinal),
            testFunction: data => data.OrderDescendingAsArray(StringHolder.Comparer.Ordinal));

        OrderAndAssertStableSort(
            linqFunction: data => data.OrderDescending(StringHolder.Comparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderDescendingAsArray(StringHolder.Comparer.OrdinalIgnoreCase));
    }

#endif

    [Fact]
    public void OrderByAsArray_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderBy(static x => x?.Text),
            testFunction: data => data.OrderByAsArray(static x => x?.Text));
    }

    [Fact]
    public void OrderByAsArray_Comparer_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderBy(static x => x?.Text, StringComparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderByAsArray(static x => x?.Text, StringComparer.OrdinalIgnoreCase));

        OrderAndAssertStableSort(
            linqFunction: data => data.OrderBy(static x => x?.Text, StringComparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderByAsArray(static x => x?.Text, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void OrderByDescendingAsArray_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderByDescending(static x => x?.Text),
            testFunction: data => data.OrderByDescendingAsArray(static x => x?.Text));
    }

    [Fact]
    public void OrderByDescendingAsArray_Comparer_IsStable()
    {
        OrderAndAssertStableSort(
            linqFunction: data => data.OrderByDescending(static x => x?.Text, StringComparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderByDescendingAsArray(static x => x?.Text, StringComparer.OrdinalIgnoreCase));

        OrderAndAssertStableSort(
            linqFunction: data => data.OrderByDescending(static x => x?.Text, StringComparer.OrdinalIgnoreCase),
            testFunction: data => data.OrderByDescendingAsArray(static x => x?.Text, StringComparer.OrdinalIgnoreCase));
    }

    private static void OrderAndAssertStableSort(
        Func<IEnumerable<StringHolder?>, IEnumerable<StringHolder?>> linqFunction,
        Func<IEnumerable<StringHolder?>, ImmutableArray<StringHolder?>> testFunction)
    {
        IEnumerable<StringHolder?> data = [
            "All", "Your", "Base", "Are", "belong", null, "To", "Us",
            "all", "your", null, "Base", "are", "belong", "to", "us"];

        var expected = linqFunction(data);
        var actual = testFunction(data);

        Assert.Equal<StringHolder?>(expected, actual, ReferenceEquals);
    }
}
