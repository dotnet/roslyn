// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test.TestData;

public abstract class OrderingTestBase<TOrderCollection, TOrderByCollection, TCaseConverter>
    where TOrderCollection : IEnumerable<int>
    where TOrderByCollection : IEnumerable<ValueHolder<int>>
    where TCaseConverter : IOrderingCaseConverter<TOrderCollection, TOrderByCollection>, new()
{
    private static readonly TheoryData<TOrderCollection, ImmutableArray<int>> s_orderTestData = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<int>> s_orderTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<int>> s_orderDescendingTestData = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<int>> s_orderDescendingTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> s_orderByTestData = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> s_orderByTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> s_orderByDescendingTestData = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> s_orderByDescendingTestData_OddBeforeEven = [];

    private static readonly TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> s_selectAndOrderTestData = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> s_selectAndOrderTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> s_selectAndOrderDescendingTestData = [];
    private static readonly TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> s_selectAndOrderDescendingTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> s_selectAndOrderByTestData = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> s_selectAndOrderByTestData_OddBeforeEven = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> s_selectAndOrderByDescendingTestData = [];
    private static readonly TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> s_selectAndOrderByDescendingTestData_OddBeforeEven = [];

    private static readonly ImmutableArray<int> s_expectedOrder = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly ImmutableArray<int> s_expectedOrder_OddBeforeEven = [1, 3, 5, 7, 9, 2, 4, 6, 8, 10];
    private static readonly ImmutableArray<int> s_expectedOrderDescending = [10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    private static readonly ImmutableArray<int> s_expectedOrderDescending_OddBeforeEven = [10, 8, 6, 4, 2, 9, 7, 5, 3, 1];

    private static readonly ImmutableArray<ValueHolder<int>> s_expectedOrderBy = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
    private static readonly ImmutableArray<ValueHolder<int>> s_expectedOrderBy_OddBeforeEven = [1, 3, 5, 7, 9, 2, 4, 6, 8, 10];
    private static readonly ImmutableArray<ValueHolder<int>> s_expectedOrderByDescending = [10, 9, 8, 7, 6, 5, 4, 3, 2, 1];
    private static readonly ImmutableArray<ValueHolder<int>> s_expectedOrderByDescending_OddBeforeEven = [10, 8, 6, 4, 2, 9, 7, 5, 3, 1];

    private static readonly ImmutableArray<string> s_expectedSelectAndOrder = ["1", "10", "2", "3", "4", "5", "6", "7", "8", "9"];
    private static readonly ImmutableArray<string> s_expectedSelectAndOrder_OddBeforeEven = ["1", "3", "5", "7", "9", "10", "2", "4", "6", "8"];
    private static readonly ImmutableArray<string> s_expectedSelectAndOrderDescending = ["9", "8", "7", "6", "5", "4", "3", "2", "10", "1"];
    private static readonly ImmutableArray<string> s_expectedSelectAndOrderDescending_OddBeforeEven = ["8", "6", "4", "2", "10", "9", "7", "5", "3", "1"];

    private static readonly ImmutableArray<ValueHolder<string>> s_expectedSelectAndOrderBy = ["1", "10", "2", "3", "4", "5", "6", "7", "8", "9"];
    private static readonly ImmutableArray<ValueHolder<string>> s_expectedSelectAndOrderBy_OddBeforeEven = ["1", "3", "5", "7", "9", "10", "2", "4", "6", "8"];
    private static readonly ImmutableArray<ValueHolder<string>> s_expectedSelectAndOrderByDescending = ["9", "8", "7", "6", "5", "4", "3", "2", "10", "1"];
    private static readonly ImmutableArray<ValueHolder<string>> s_expectedSelectAndOrderByDescending_OddBeforeEven = ["8", "6", "4", "2", "10", "9", "7", "5", "3", "1"];

    private static void AddCase(TOrderCollection collection)
    {
        s_orderTestData.Add(collection, s_expectedOrder);
        s_orderTestData_OddBeforeEven.Add(collection, s_expectedOrder_OddBeforeEven);
        s_orderDescendingTestData.Add(collection, s_expectedOrderDescending);
        s_orderDescendingTestData_OddBeforeEven.Add(collection, s_expectedOrderDescending_OddBeforeEven);

        s_selectAndOrderTestData.Add(collection, s_expectedSelectAndOrder, static x => x.ToString());
        s_selectAndOrderTestData_OddBeforeEven.Add(collection, s_expectedSelectAndOrder_OddBeforeEven, static x => x.ToString());
        s_selectAndOrderDescendingTestData.Add(collection, s_expectedSelectAndOrderDescending, static x => x.ToString());
        s_selectAndOrderDescendingTestData_OddBeforeEven.Add(collection, s_expectedSelectAndOrderDescending_OddBeforeEven, static x => x.ToString());
    }

    private static void AddCase(TOrderByCollection collection)
    {
        s_orderByTestData.Add(collection, s_expectedOrderBy);
        s_orderByTestData_OddBeforeEven.Add(collection, s_expectedOrderBy_OddBeforeEven);
        s_orderByDescendingTestData.Add(collection, s_expectedOrderByDescending);
        s_orderByDescendingTestData_OddBeforeEven.Add(collection, s_expectedOrderByDescending_OddBeforeEven);

        s_selectAndOrderByTestData.Add(collection, s_expectedSelectAndOrderBy, static x => x.Value.ToString());
        s_selectAndOrderByTestData_OddBeforeEven.Add(collection, s_expectedSelectAndOrderBy_OddBeforeEven, static x => x.Value.ToString());
        s_selectAndOrderByDescendingTestData.Add(collection, s_expectedSelectAndOrderByDescending, static x => x.Value.ToString());
        s_selectAndOrderByDescendingTestData_OddBeforeEven.Add(collection, s_expectedSelectAndOrderByDescending_OddBeforeEven, static x => x.Value.ToString());
    }

    static OrderingTestBase()
    {
        var converter = new TCaseConverter();

        AddCase(converter.ConvertOrderCase([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
        AddCase(converter.ConvertOrderCase([10, 9, 8, 7, 6, 5, 4, 3, 2, 1]));
        AddCase(converter.ConvertOrderCase([1, 3, 5, 7, 9, 2, 4, 6, 8, 10]));
        AddCase(converter.ConvertOrderCase([2, 5, 8, 1, 3, 9, 7, 4, 10, 6]));
        AddCase(converter.ConvertOrderCase([6, 10, 4, 7, 9, 3, 1, 8, 5, 2]));

        AddCase(converter.ConvertOrderByCase([1, 2, 3, 4, 5, 6, 7, 8, 9, 10]));
        AddCase(converter.ConvertOrderByCase([10, 9, 8, 7, 6, 5, 4, 3, 2, 1]));
        AddCase(converter.ConvertOrderByCase([1, 3, 5, 7, 9, 2, 4, 6, 8, 10]));
        AddCase(converter.ConvertOrderByCase([2, 5, 8, 1, 3, 9, 7, 4, 10, 6]));
        AddCase(converter.ConvertOrderByCase([6, 10, 4, 7, 9, 3, 1, 8, 5, 2]));
    }

    protected static Comparison<int> OddBeforeEven
        => (x, y) => (x % 2 != 0, y % 2 != 0) switch
        {
            (true, false) => -1,
            (false, true) => 1,
            _ => x.CompareTo(y)
        };

    protected static Comparison<string> OddBeforeEvenString
        => (x, y) => (int.Parse(x) % 2 != 0, int.Parse(y) % 2 != 0) switch
        {
            (true, false) => -1,
            (false, true) => 1,
            _ => x.CompareTo(y)
        };

    public static TheoryData<TOrderCollection, ImmutableArray<int>> OrderTestData => s_orderTestData;
    public static TheoryData<TOrderCollection, ImmutableArray<int>> OrderTestData_OddBeforeEven => s_orderTestData_OddBeforeEven;
    public static TheoryData<TOrderCollection, ImmutableArray<int>> OrderDescendingTestData => s_orderDescendingTestData;
    public static TheoryData<TOrderCollection, ImmutableArray<int>> OrderDescendingTestData_OddBeforeEven => s_orderDescendingTestData_OddBeforeEven;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> OrderByTestData => s_orderByTestData;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> OrderByTestData_OddBeforeEven => s_orderByTestData_OddBeforeEven;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> OrderByDescendingTestData => s_orderByDescendingTestData;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<int>>> OrderByDescendingTestData_OddBeforeEven => s_orderByDescendingTestData_OddBeforeEven;

    public static TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> SelectAndOrderTestData => s_selectAndOrderTestData;
    public static TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> SelectAndOrderTestData_OddBeforeEven => s_selectAndOrderTestData_OddBeforeEven;
    public static TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> SelectAndOrderDescendingTestData => s_selectAndOrderDescendingTestData;
    public static TheoryData<TOrderCollection, ImmutableArray<string>, Func<int, string>> SelectAndOrderDescendingTestData_OddBeforeEven => s_selectAndOrderDescendingTestData_OddBeforeEven;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> SelectAndOrderByTestData => s_selectAndOrderByTestData;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> SelectAndOrderByTestData_OddBeforeEven => s_selectAndOrderByTestData_OddBeforeEven;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> SelectAndOrderByDescendingTestData => s_selectAndOrderByDescendingTestData;
    public static TheoryData<TOrderByCollection, ImmutableArray<ValueHolder<string>>, Func<ValueHolder<int>, ValueHolder<string>>> SelectAndOrderByDescendingTestData_OddBeforeEven => s_selectAndOrderByDescendingTestData_OddBeforeEven;

    protected void AssertEqual<T>(ImmutableArray<T> result, ImmutableArray<T> expected)
    {
        Assert.Equal<T>(result, expected);
    }

    protected void AssertEqual<T>(ImmutableArray<T> result, ImmutableArray<T> expected, IEqualityComparer<T> comparer)
    {
        Assert.Equal(result, expected, comparer);
    }
}
