// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public class ListExtensionsTests
{
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

        Assert.False(new int[0].HasDuplicates());
        Assert.False(new int[0].HasDuplicates(comparer));
        Assert.False(new int[0].HasDuplicates(i => i + 1));

        Assert.False(new[] { 1 }.HasDuplicates());
        Assert.False(new[] { 1 }.HasDuplicates(comparer));
        Assert.False(new[] { 1 }.HasDuplicates(i => i + 1));

        Assert.False(new[] { 1, 2 }.HasDuplicates());
        Assert.False(new[] { 1, 2 }.HasDuplicates(comparer));
        Assert.False(new[] { 1, 2 }.HasDuplicates(i => i + 1));

        Assert.True(new[] { 1, 1 }.HasDuplicates());
        Assert.True(new[] { 11, 1 }.HasDuplicates(comparer));
        Assert.True(new[] { 1, 3 }.HasDuplicates(i => i % 2));
        Assert.True(new[] { 11.0, 1.2 }.HasDuplicates(i => (int)i, comparer));

        Assert.False(new[] { 2, 0, 1, 3 }.HasDuplicates());
        Assert.False(new[] { 2, 0, 1, 13 }.HasDuplicates(comparer));
        Assert.False(new[] { 2, 0, 1, 53 }.HasDuplicates(i => i % 10));
        Assert.False(new[] { 2.3, 0.1, 1.3, 53.4 }.HasDuplicates(i => (int)i, comparer));

        Assert.True(new[] { 2, 0, 1, 2 }.HasDuplicates());
        Assert.True(new[] { 2, 0, 1, 12 }.HasDuplicates(comparer));
        Assert.True(new[] { 2, 0, 1, 52 }.HasDuplicates(i => i % 10));
        Assert.True(new[] { 2.3, 0.1, 1.3, 52.4 }.HasDuplicates(i => (int)i, comparer));
    }
}

