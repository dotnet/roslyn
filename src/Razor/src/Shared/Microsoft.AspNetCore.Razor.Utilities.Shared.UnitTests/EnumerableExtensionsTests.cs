// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit;
using SR = Microsoft.AspNetCore.Razor.Utilities.Shared.Resources.SR;

namespace Microsoft.AspNetCore.Razor.Utilities.Shared.Test;

public class EnumerableExtensionsTests
{
    [Fact]
    public void CopyTo_ImmutableArray()
    {
        Span<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var immutableArray = ImmutableArray.Create(source);

        AssertCopyToCore(immutableArray, immutableArray.Length);
    }

    [Fact]
    public void CopyTo_ImmutableArrayBuilder()
    {
        Span<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var builder = ImmutableArray.CreateBuilder<int>();
        builder.AddRange(source);

        AssertCopyToCore(builder, builder.Count);
    }

    [Fact]
    public void CopyTo_List()
    {
        IEnumerable<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var list = new List<int>(source);

        AssertCopyToCore(list, list.Count);
    }

    [Fact]
    public void CopyTo_HashSet()
    {
        IEnumerable<int> source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var set = new HashSet<int>(source);

        AssertCopyToCore(set, set.Count);
    }

    [Fact]
    public void CopyTo_Array()
    {
        int[] array = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        AssertCopyToCore(array, array.Length);
    }

    [Fact]
    public void CopyTo_CustomEnumerable()
    {
        CustomEnumerable custom = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        AssertCopyToCore(custom, 10);
    }

    [Fact]
    public void CopyTo_CustomReadOnlyCollection()
    {
        CustomReadOnlyCollection custom = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

        AssertCopyToCore(custom, 10);
    }

    private static void AssertCopyToCore(IEnumerable<int> sequence, int count)
    {
        var destination1 = new int[count - 1];
        var exception = Assert.Throws<ArgumentException>(() => sequence.CopyTo(destination1.AsSpan()));
        Assert.StartsWith(SR.Destination_is_too_short, exception.Message);

        Span<int> destination2 = stackalloc int[count];
        sequence.CopyTo(destination2);
        AssertElementsEqual(sequence, destination2);

        Span<int> destination3 = stackalloc int[count + 1];
        sequence.CopyTo(destination3);
        AssertElementsEqual(sequence, destination3);

        static void AssertElementsEqual<T>(IEnumerable<T> sequence, ReadOnlySpan<T> span)
        {
            var index = 0;

            foreach (var item in sequence)
            {
                Assert.Equal(item, span[index++]);
            }
        }
    }

    [CollectionBuilder(typeof(CustomEnumerable), "Create")]
    private sealed class CustomEnumerable(params ReadOnlySpan<int> values) : IEnumerable<int>
    {
        private readonly int[] _values = values.ToArray();

        public IEnumerator<int> GetEnumerator()
        {
            foreach (var value in _values)
            {
                yield return value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public static CustomEnumerable Create(ReadOnlySpan<int> span)
            => new(span);
    }

    [CollectionBuilder(typeof(CustomReadOnlyCollection), "Create")]
    private sealed class CustomReadOnlyCollection(params ReadOnlySpan<int> values) : IReadOnlyCollection<int>
    {
        private readonly int[] _values = values.ToArray();

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

        public static CustomReadOnlyCollection Create(ReadOnlySpan<int> span)
            => new(span);
    }

    [Fact]
    public void SelectAsArray()
    {
        IEnumerable<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [2, 4, 6, 8, 10, 12, 14, 16, 18, 20];

        var actual = data.SelectAsArray(static x => x * 2);
        Assert.Equal<int>(expected, actual);
    }

    [Fact]
    public void SelectAsArray_Index()
    {
        IEnumerable<int> data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        ImmutableArray<int> expected = [1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

        var actual = data.SelectAsArray(static (x, index) => x + index);
        Assert.Equal<int>(expected, actual);
    }
}
