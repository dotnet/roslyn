// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

public class EnumerableExtensionTests
{
    private static IEnumerable<T> Enumerable<T>(params T[] values)
        => values;

    [Fact]
    public void TestDo()
    {
        var elements = Enumerable(1, 2, 3);
        var result = new List<int>();

        elements.Do(a => result.Add(a));

        Assert.True(elements.SequenceEqual(result));
    }

    [Fact]
    public void TestConcat()
    {
        var elements = Enumerable(1, 2, 3);
        Assert.True(Enumerable(1, 2, 3, 4).SequenceEqual(elements.Concat(4)));
    }

    [Fact]
    public void TestSetEquals()
        => Assert.True(Enumerable(1, 2, 3, 4).SetEquals(Enumerable(4, 2, 3, 1)));

    [Fact]
    public void TestIsEmpty()
    {
        Assert.True(Enumerable<int>().IsEmpty());
        Assert.False(Enumerable(0).IsEmpty());
    }

    [Fact]
    public void TestAll()
    {
        Assert.True(Enumerable<bool>().All());
        Assert.True(Enumerable(true).All());
        Assert.True(Enumerable(true, true).All());

        Assert.False(Enumerable(false).All());
        Assert.False(Enumerable(false, false).All());
        Assert.False(Enumerable(true, false).All());
        Assert.False(Enumerable(false, true).All());
    }

    [Fact]
    public void TestJoin()
    {
        Assert.Equal(string.Empty, Enumerable<string>().Join(", "));
        Assert.Equal("a", Enumerable("a").Join(", "));
        Assert.Equal("a, b", Enumerable("a", "b").Join(", "));
        Assert.Equal("a, b, c", Enumerable("a", "b", "c").Join(", "));
    }

    [Fact]
    public void TestFlatten()
    {
        var sequence = Enumerable(Enumerable("a", "b"), Enumerable("c", "d"), Enumerable("e", "f"));
        Assert.True(sequence.Flatten().SequenceEqual(Enumerable("a", "b", "c", "d", "e", "f")));
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
}
