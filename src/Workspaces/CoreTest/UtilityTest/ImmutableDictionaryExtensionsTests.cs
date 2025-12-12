// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public sealed class ImmutableDictionaryExtensionsTests
{
    [Fact]
    public void KeysEqual()
    {
        var empty = ImmutableDictionary<int, int>.Empty;

        Assert.True(empty.KeysEqual(empty));
        Assert.False(empty.KeysEqual(empty.Add(1, 1)));
        Assert.False(empty.Add(1, 1).KeysEqual(empty));

        Assert.True(empty.Add(1, 1).KeysEqual(empty.Add(1, 1)));
        Assert.True(empty.Add(1, 2).KeysEqual(empty.Add(1, 1)));

        Assert.True(empty.Add(2, 0).Add(1, 0).KeysEqual(empty.Add(1, 1).Add(2, 1)));
        Assert.False(empty.Add(2, 0).Add(3, 0).KeysEqual(empty.Add(1, 1).Add(2, 1)));
    }

    [Fact]
    public void KeysEqual_Comparer()
    {
        var emptyOrdinal = ImmutableDictionary<string, int>.Empty.WithComparers(keyComparer: StringComparer.Ordinal);
        var emptyIgnoreCase = ImmutableDictionary<string, int>.Empty.WithComparers(keyComparer: StringComparer.OrdinalIgnoreCase);

        Assert.True(emptyIgnoreCase.Add("A", 1).KeysEqual(emptyIgnoreCase.Add("a", 1)));
        Assert.False(emptyIgnoreCase.Add("A", 1).KeysEqual(emptyOrdinal.Add("a", 1)));
        Assert.False(emptyOrdinal.Add("A", 1).KeysEqual(emptyIgnoreCase.Add("a", 1)));
        Assert.False(emptyOrdinal.Add("A", 1).KeysEqual(emptyOrdinal.Add("a", 1)));

        Assert.True(emptyIgnoreCase.Add("A", 1).KeysEqual(emptyOrdinal.Add("A", 1)));
        Assert.True(emptyOrdinal.Add("A", 1).KeysEqual(emptyIgnoreCase.Add("A", 1)));
    }
}
