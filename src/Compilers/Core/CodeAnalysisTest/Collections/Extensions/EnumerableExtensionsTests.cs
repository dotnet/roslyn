// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class EnumerableExtensionsTests
{
    private static IEnumerable<T> MakeEnumerable<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }
    }

    [Fact]
    public void SequenceEqual()
    {
        bool comparer(int x, int y) => x == y;
        Assert.True(RoslynEnumerableExtensions.SequenceEqual((IEnumerable<int>)null, null, comparer));
        Assert.False(RoslynEnumerableExtensions.SequenceEqual(new[] { 1 }, null, comparer));
        Assert.False(RoslynEnumerableExtensions.SequenceEqual(null, new[] { 1 }, comparer));

        Assert.True(RoslynEnumerableExtensions.SequenceEqual(new[] { 1 }, new[] { 1 }, comparer));
        Assert.False(RoslynEnumerableExtensions.SequenceEqual(new int[0], new[] { 1 }, comparer));
        Assert.False(RoslynEnumerableExtensions.SequenceEqual(new[] { 1 }, new int[0], comparer));
        Assert.False(RoslynEnumerableExtensions.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 3, 2 }, comparer));
        Assert.True(RoslynEnumerableExtensions.SequenceEqual(new[] { 1, 2, 3 }, new[] { 1, 2, 3 }, comparer));
    }

    [Fact]
    public void AsSingleton()
    {
        Assert.Equal(0, new int[] { }.AsSingleton());
        Assert.Equal(1, new int[] { 1 }.AsSingleton());
        Assert.Equal(0, new int[] { 1, 2 }.AsSingleton());

        Assert.Equal(0, Enumerable.Range(1, 0).AsSingleton());
        Assert.Equal(1, Enumerable.Range(1, 1).AsSingleton());
        Assert.Equal(0, Enumerable.Range(1, 2).AsSingleton());
    }

    private class ReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly T[] _items;

        public ReadOnlyList(params T[] items)
        {
            _items = items;
        }

        public T this[int index] => _items[index];
        public int Count => _items.Length;
        public IEnumerator<T> GetEnumerator() => throw new NotImplementedException();
        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    private class SignlessEqualityComparer : IEqualityComparer<int>
    {
        public bool Equals(int x, int y) => Math.Abs(x) == Math.Abs(y);
        public int GetHashCode(int obj) => throw new NotImplementedException();
    }

    [Fact]
    public void IndexOf()
    {
        Assert.Equal(-1, SpecializedCollections.SingletonList(5).IndexOf(6));
        Assert.Equal(0, SpecializedCollections.SingletonList(5).IndexOf(5));

        Assert.Equal(-1, new ReadOnlyList<int>(5).IndexOf(6));
        Assert.Equal(0, new ReadOnlyList<int>(5).IndexOf(5));
    }

    [Fact]
    public void IndexOf_EqualityComparer()
    {
        var comparer = new SignlessEqualityComparer();

        Assert.Equal(-1, SpecializedCollections.SingletonList(5).IndexOf(-6, comparer));
        Assert.Equal(0, SpecializedCollections.SingletonList(5).IndexOf(-5, comparer));

        Assert.Equal(-1, new ReadOnlyList<int>(5).IndexOf(-6, comparer));
        Assert.Equal(0, new ReadOnlyList<int>(5).IndexOf(-5, comparer));
    }

    [Fact]
    public void TestDo()
    {
        var elements = MakeEnumerable(1, 2, 3);
        var result = new List<int>();

        elements.Do(a => result.Add(a));

        Assert.True(elements.SequenceEqual(result));
    }

    [Fact]
    public void TestConcat()
    {
        var elements = MakeEnumerable(1, 2, 3);
        Assert.True(MakeEnumerable(1, 2, 3, 4).SequenceEqual(elements.Concat(4)));
    }

    [Fact]
    public void TestSetEquals()
        => Assert.True(MakeEnumerable(1, 2, 3, 4).SetEquals(MakeEnumerable(4, 2, 3, 1)));

    [Fact]
    public void TestIsEmpty()
    {
        Assert.True(MakeEnumerable<int>().IsEmpty());
        Assert.False(MakeEnumerable(0).IsEmpty());
    }

    [Fact]
    public void TestJoin()
    {
        Assert.Equal(string.Empty, MakeEnumerable<string>().Join(", "));
        Assert.Equal("a", MakeEnumerable("a").Join(", "));
        Assert.Equal("a, b", MakeEnumerable("a", "b").Join(", "));
        Assert.Equal("a, b, c", MakeEnumerable("a", "b", "c").Join(", "));
    }

    [Fact]
    public void TestFlatten()
    {
        var sequence = MakeEnumerable(MakeEnumerable("a", "b"), MakeEnumerable("c", "d"), MakeEnumerable("e", "f"));
        Assert.True(sequence.Flatten().SequenceEqual(MakeEnumerable("a", "b", "c", "d", "e", "f")));
    }

    [Fact]
    public void TestSequenceEqualWithFunction()
    {
        static bool equality(int a, int b) => a == b;
        var seq = new List<int>() { 1, 2, 3 };

        // same object reference
        Assert.True(seq.SequenceEqual(seq, equality));

        // matching values, matching lengths
        Assert.True(seq.SequenceEqual(new int[] { 1, 2, 3 }, equality));

        // matching values, different lengths
        Assert.False(seq.SequenceEqual(new int[] { 1, 2, 3, 4 }, equality));
        Assert.False(seq.SequenceEqual(new int[] { 1, 2 }, equality));

        // different values, matching lengths
        Assert.False(seq.SequenceEqual(new int[] { 1, 2, 6 }, equality));
    }

    public sealed class Comparer<T>(Func<T, T, bool> equals, Func<T, int> hashCode) : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _equals = equals;
        private readonly Func<T, int> _hashCode = hashCode;

        public bool Equals(T x, T y) => _equals(x, y);
        public int GetHashCode(T obj) => _hashCode(obj);
    }

    [Fact]
    public void HasDuplicates()
    {
        var comparer = new Comparer<int>((x, y) => x % 10 == y % 10, x => (x % 10).GetHashCode());

        Assert.False(MakeEnumerable<int>().HasDuplicates());
        Assert.False(MakeEnumerable<int>().HasDuplicates(comparer));
        Assert.False(MakeEnumerable<int>().HasDuplicates(i => i + 1));

        Assert.False(MakeEnumerable(1).HasDuplicates());
        Assert.False(MakeEnumerable(1).HasDuplicates(comparer));
        Assert.False(MakeEnumerable(1).HasDuplicates(i => i + 1));

        Assert.False(MakeEnumerable(1, 2).HasDuplicates());
        Assert.False(MakeEnumerable(1, 2).HasDuplicates(comparer));
        Assert.False(MakeEnumerable(1, 2).HasDuplicates(i => i + 1));

        Assert.True(MakeEnumerable(1, 1).HasDuplicates());
        Assert.True(MakeEnumerable(11, 1).HasDuplicates(comparer));
        Assert.True(MakeEnumerable(1, 3).HasDuplicates(i => i % 2));
        Assert.True(MakeEnumerable(11.0, 1.2).HasDuplicates(i => (int)i, comparer));

        Assert.False(MakeEnumerable(2, 0, 1, 3).HasDuplicates());
        Assert.False(MakeEnumerable(2, 0, 1, 13).HasDuplicates(comparer));
        Assert.False(MakeEnumerable(2, 0, 1, 53).HasDuplicates(i => i % 10));
        Assert.False(MakeEnumerable(2.3, 0.1, 1.3, 53.4).HasDuplicates(i => (int)i, comparer));

        Assert.True(MakeEnumerable(2, 0, 1, 2).HasDuplicates());
        Assert.True(MakeEnumerable(2, 0, 1, 12).HasDuplicates(comparer));
        Assert.True(MakeEnumerable(2, 0, 1, 52).HasDuplicates(i => i % 10));
        Assert.True(MakeEnumerable(2.3, 0.1, 1.3, 52.4).HasDuplicates(i => (int)i, comparer));
    }
}

