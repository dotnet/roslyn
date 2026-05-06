// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
using SR = Microsoft.AspNetCore.Razor.Utilities.Shared.Resources.SR;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class ReadOnlyListExtensionsTest
{
    private static Func<int, bool> IsEven => x => x % 2 == 0;
    private static Func<int, bool> IsOdd => x => x % 2 != 0;

    [Fact]
    public void Any()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        Assert.False(readOnlyList.Any());

        list.Add(19);

        Assert.True(readOnlyList.Any());

        list.Add(23);

        Assert.True(readOnlyList.Any(IsOdd));

        // ... but no even numbers
        Assert.False(readOnlyList.Any(IsEven));
    }

    [Fact]
    public void All()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        Assert.True(readOnlyList.All(IsEven));

        list.Add(19);

        Assert.False(readOnlyList.All(IsEven));

        list.Add(23);

        Assert.True(readOnlyList.All(IsOdd));

        list.Add(42);

        Assert.False(readOnlyList.All(IsOdd));
    }

    [Fact]
    public void FirstAndLast()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        var exception1 = Assert.Throws<InvalidOperationException>(() => readOnlyList.First());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);

        Assert.Equal(default, readOnlyList.FirstOrDefault());

        var exception2 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Last());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);

        Assert.Equal(default, readOnlyList.LastOrDefault());

        list.Add(19);

        Assert.Equal(19, readOnlyList.First());
        Assert.Equal(19, readOnlyList.FirstOrDefault());
        Assert.Equal(19, readOnlyList.Last());
        Assert.Equal(19, readOnlyList.LastOrDefault());

        list.Add(23);

        Assert.Equal(19, readOnlyList.First());
        Assert.Equal(19, readOnlyList.FirstOrDefault());
        Assert.Equal(23, readOnlyList.Last());
        Assert.Equal(23, readOnlyList.LastOrDefault());
    }

    [Fact]
    public void FirstAndLastWithPredicate()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        var exception1 = Assert.Throws<InvalidOperationException>(() => readOnlyList.First(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception1.Message);

        Assert.Equal(default, readOnlyList.FirstOrDefault(IsOdd));

        var exception2 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Last(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception2.Message);

        Assert.Equal(default, readOnlyList.LastOrDefault(IsOdd));

        list.Add(19);

        Assert.Equal(19, readOnlyList.First(IsOdd));
        Assert.Equal(19, readOnlyList.FirstOrDefault(IsOdd));
        Assert.Equal(19, readOnlyList.Last(IsOdd));
        Assert.Equal(19, readOnlyList.LastOrDefault(IsOdd));

        list.Add(23);

        Assert.Equal(19, readOnlyList.First(IsOdd));
        Assert.Equal(19, readOnlyList.FirstOrDefault(IsOdd));
        Assert.Equal(23, readOnlyList.Last(IsOdd));
        Assert.Equal(23, readOnlyList.LastOrDefault(IsOdd));

        var exception3 = Assert.Throws<InvalidOperationException>(() => readOnlyList.First(IsEven));
        Assert.Equal(SR.Contains_no_matching_elements, exception3.Message);

        Assert.Equal(default, readOnlyList.FirstOrDefault(IsEven));

        var exception4 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Last(IsEven));
        Assert.Equal(SR.Contains_no_matching_elements, exception4.Message);

        Assert.Equal(default, readOnlyList.LastOrDefault(IsEven));

        list.Add(42);

        Assert.Equal(42, readOnlyList.First(IsEven));
        Assert.Equal(42, readOnlyList.FirstOrDefault(IsEven));
        Assert.Equal(42, readOnlyList.Last(IsEven));
        Assert.Equal(42, readOnlyList.LastOrDefault(IsEven));
    }

    [Fact]
    public void Single()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        var exception1 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Single());
        Assert.Equal(SR.Contains_no_elements, exception1.Message);
        Assert.Equal(default, readOnlyList.SingleOrDefault());

        list.Add(19);

        Assert.Equal(19, readOnlyList.Single());
        Assert.Equal(19, readOnlyList.SingleOrDefault());

        list.Add(23);

        var exception2 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Single());
        Assert.Equal(SR.Contains_more_than_one_element, exception2.Message);
        var exception3 = Assert.Throws<InvalidOperationException>(() => readOnlyList.SingleOrDefault());
        Assert.Equal(SR.Contains_more_than_one_element, exception2.Message);
    }

    [Fact]
    public void SingleWithPredicate()
    {
        var list = new List<int>();
        var readOnlyList = (IReadOnlyList<int>)list;

        var exception1 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Single(IsOdd));
        Assert.Equal(SR.Contains_no_matching_elements, exception1.Message);
        Assert.Equal(default, readOnlyList.SingleOrDefault(IsOdd));

        list.Add(19);

        Assert.Equal(19, readOnlyList.Single(IsOdd));
        Assert.Equal(19, readOnlyList.SingleOrDefault(IsOdd));

        list.Add(23);

        var exception2 = Assert.Throws<InvalidOperationException>(() => readOnlyList.Single(IsOdd));
        Assert.Equal(SR.Contains_more_than_one_matching_element, exception2.Message);
        var exception3 = Assert.Throws<InvalidOperationException>(() => readOnlyList.SingleOrDefault(IsOdd));
        Assert.Equal(SR.Contains_more_than_one_matching_element, exception2.Message);

        list.Add(42);

        Assert.Equal(42, readOnlyList.Single(IsEven));
        Assert.Equal(42, readOnlyList.SingleOrDefault(IsEven));
    }

    [Fact]
    public void CopyTo_ImmutableArray()
    {
        Span<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var immutableArray = ImmutableArray.Create(source);

        AssertCopyToCore(immutableArray);
    }

    [Fact]
    public void CopyTo_ImmutableArrayBuilder()
    {
        Span<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.AddRange(source);

        AssertCopyToCore(builder);
    }

    [Fact]
    public void CopyTo_List()
    {
        IEnumerable<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var list = new List<int>(source);

        AssertCopyToCore(list);
    }

    [Fact]
    public void CopyTo_Array()
    {
        int[] array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        AssertCopyToCore(array);
    }

    [Fact]
    public void CopyTo_CustomReadOnlyList()
    {
        CustomReadOnlyList custom = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        AssertCopyToCore(custom);
    }

    private static void AssertCopyToCore(IReadOnlyList<int> list)
    {
        var destination1 = new int[list.Count - 1];
        var exception = Assert.Throws<ArgumentException>(() => list.CopyTo(destination1.AsSpan()));
        Assert.StartsWith(SR.Destination_is_too_short, exception.Message);

        Span<int> destination2 = stackalloc int[list.Count];
        list.CopyTo(destination2);
        AssertElementsEqual(list, destination2);

        Span<int> destination3 = stackalloc int[list.Count + 1];
        list.CopyTo(destination3);
        AssertElementsEqual(list, destination3);

        static void AssertElementsEqual<T>(IReadOnlyList<T> list, ReadOnlySpan<T> span)
        {
            var count = list.Count;
            for (var i = 0; i < count; i++)
            {
                Assert.Equal(list[i], span[i]);
            }
        }
    }

    [CollectionBuilder(typeof(CustomReadOnlyList), "Create")]
    private sealed class CustomReadOnlyList(params ReadOnlySpan<int> values) : IReadOnlyList<int>
    {
        private readonly int[] _values = values.ToArray();

        public int this[int index] => _values[index];
        public int Count => _values.Length;

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var value in _values)
            {
                yield return value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public static CustomReadOnlyList Create(ReadOnlySpan<int> span)
            => new(span);
    }

    [Fact]
    public void SelectAsArray()
    {
        IReadOnlyList<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var actual = data.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Enumerable()
    {
        IReadOnlyList<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var enumerable = (IEnumerable<int>)data;

        var actual = enumerable.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index()
    {
        IReadOnlyList<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var actual = data.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index_Enumerable()
    {
        IReadOnlyList<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var enumerable = (IEnumerable<int>)data;

        var actual = enumerable.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void AsEnumerable_Basic()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable());

        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void AsEnumerable_Basic_EmptyList()
    {
        IReadOnlyList<int> list = [];

        var result = Enumerate(list.AsEnumerable());

        Assert.Empty(result);
    }

    [Fact]
    public void AsEnumerable_WithStart()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 2));

        Assert.Equal([3, 4, 5], result);
    }

    [Fact]
    public void AsEnumerable_WithStart_StartAtEnd()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 5));

        Assert.Empty(result);
    }

    [Fact]
    public void AsEnumerable_WithStart_StartAtZero()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 0));

        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void AsEnumerable_WithStartIndex_FromStart()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(^3)); // Last 3 elements

        Assert.Equal([3, 4, 5], result);
    }

    [Fact]
    public void AsEnumerable_WithStartIndex_FromEnd()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(^1)); // Last element

        Assert.Equal([5], result);
    }

    [Fact]
    public void AsEnumerable_WithRange()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(1..4)); // Elements at indices 1, 2, 3

        Assert.Equal([2, 3, 4], result);
    }

    [Fact]
    public void AsEnumerable_WithRange_FromEnd()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(^3..^1)); // Last 3 elements excluding the very last

        Assert.Equal([3, 4], result);
    }

    [Fact]
    public void AsEnumerable_WithRange_EntireRange()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(..)); // Entire range

        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void AsEnumerable_WithStartAndCount()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 1, count: 3));

        Assert.Equal([2, 3, 4], result);
    }

    [Fact]
    public void AsEnumerable_WithStartAndCount_ZeroCount()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 2, count: 0));

        Assert.Empty(result);
    }

    [Fact]
    public void AsEnumerable_WithStartAndCount_SingleElement()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 3, count: 1));

        Assert.Equal([4], result);
    }

    [Fact]
    public void AsEnumerable_Reversed()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable().Reversed());

        Assert.Equal([5, 4, 3, 2, 1], result);
    }

    [Fact]
    public void AsEnumerable_Reversed_WithRange()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        var result = Enumerate(list.AsEnumerable(start: 1, count: 3).Reversed());

        Assert.Equal([4, 3, 2], result);
    }

    [Fact]
    public void AsEnumerable_Reversed_EmptyList()
    {
        IReadOnlyList<int> list = [];

        var result = Enumerate(list.AsEnumerable().Reversed());

        Assert.Empty(result);
    }

    [Fact]
    public void AsEnumerable_EnumeratorReset()
    {
        IReadOnlyList<int> list = [1, 2, 3];
        var enumerable = list.AsEnumerable();
        var enumerator = enumerable.GetEnumerator();

        // First enumeration
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
        {
            firstPass.Add(enumerator.Current);
        }

        Assert.Equal([1, 2, 3], firstPass);

        // Reset and enumerate again
        enumerator.Reset();

        var secondPass = new List<int>();
        while (enumerator.MoveNext())
        {
            secondPass.Add(enumerator.Current);
        }

        Assert.Equal([1, 2, 3], secondPass);
    }

    [Fact]
    public void AsEnumerable_ReverseEnumeratorReset()
    {
        IReadOnlyList<int> list = [1, 2, 3];
        var enumerable = list.AsEnumerable().Reversed();
        var enumerator = enumerable.GetEnumerator();

        // First enumeration
        var firstPass = new List<int>();
        while (enumerator.MoveNext())
        {
            firstPass.Add(enumerator.Current);
        }

        Assert.Equal([3, 2, 1], firstPass);

        // Reset and enumerate again
        enumerator.Reset();

        var secondPass = new List<int>();
        while (enumerator.MoveNext())
        {
            secondPass.Add(enumerator.Current);
        }

        Assert.Equal([3, 2, 1], secondPass);
    }

    [Fact]
    public void AsEnumerable_ImmutableArray()
    {
        ImmutableArray<int> array = [1, 2, 3, 4, 5];
        IReadOnlyList<int> list = array;

        var result = Enumerate(list.AsEnumerable(start: 1, count: 3));

        Assert.Equal([2, 3, 4], result);
    }

    [Fact]
    public void AsEnumerable_CustomReadOnlyList()
    {
        CustomReadOnlyList custom = [1, 2, 3, 4, 5];

        var result = Enumerate(custom.AsEnumerable(start: 1, count: 3));

        Assert.Equal([2, 3, 4], result);
    }

    [Fact]
    public void AsEnumerable_NullList_ThrowsArgumentNullException()
    {
        IReadOnlyList<int> list = null!;

        Assert.Throws<ArgumentNullException>(() => list.AsEnumerable());
        Assert.Throws<ArgumentNullException>(() => list.AsEnumerable(start: 0));
        Assert.Throws<ArgumentNullException>(() => list.AsEnumerable(^1));
        Assert.Throws<ArgumentNullException>(() => list.AsEnumerable(0..3));
        Assert.Throws<ArgumentNullException>(() => list.AsEnumerable(start: 0, count: 1));
    }

    [Fact]
    public void AsEnumerable_NegativeStart_ThrowsArgumentOutOfRangeException()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: -5, count: 2));
    }

    [Fact]
    public void AsEnumerable_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 0, count: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 2, count: -3));
    }

    [Fact]
    public void AsEnumerable_StartOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 6));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 10, count: 1));
    }

    [Fact]
    public void AsEnumerable_StartPlusCountExceedsLength_ThrowsArgumentOutOfRangeException()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 3, count: 3)); // 3 + 3 = 6 > 5
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 0, count: 6)); // 0 + 6 = 6 > 5
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 5, count: 1)); // 5 + 1 = 6 > 5
    }

    [Fact]
    public void AsEnumerable_ValidRangeAtBoundaries_Succeeds()
    {
        IReadOnlyList<int> list = [1, 2, 3, 4, 5];

        // Valid boundary cases that should not throw
        var result1 = list.AsEnumerable(start: 0, count: 0); // Empty range at start
        Assert.Empty(Enumerate(result1));

        var result2 = list.AsEnumerable(start: 5, count: 0); // Empty range at end
        Assert.Empty(Enumerate(result2));

        var result3 = list.AsEnumerable(start: 0, count: 5); // Full range
        Assert.Equal([1, 2, 3, 4, 5], Enumerate(result3));

        var result4 = list.AsEnumerable(start: 4, count: 1); // Last element
        Assert.Equal([5], Enumerate(result4));
    }

    [Fact]
    public void AsEnumerable_EmptyList_ValidArguments_Succeeds()
    {
        IReadOnlyList<int> list = [];

        // These should not throw on empty list
        var result1 = list.AsEnumerable();
        Assert.Empty(Enumerate(result1));

        var result2 = list.AsEnumerable(start: 0);
        Assert.Empty(Enumerate(result2));

        var result3 = list.AsEnumerable(start: 0, count: 0);
        Assert.Empty(Enumerate(result3));

        var result4 = list.AsEnumerable(..);
        Assert.Empty(Enumerate(result4));
    }

    [Fact]
    public void AsEnumerable_EmptyList_InvalidArguments_ThrowsArgumentOutOfRangeException()
    {
        IReadOnlyList<int> list = [];

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 0, count: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.AsEnumerable(start: 1, count: 0));
    }

    private static T[] Enumerate<T>(ReadOnlyListExtensions.Enumerable<T> enumerable)
    {
        var result = new List<T>();

        foreach (var item in enumerable)
        {
            result.Add(item);
        }

        return [.. result];
    }

    private static T[] Enumerate<T>(ReadOnlyListExtensions.Enumerable<T>.ReversedEnumerable enumerable)
    {
        var result = new List<T>();

        foreach (var item in enumerable)
        {
            result.Add(item);
        }

        return [.. result];
    }
}
