// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Internal;

internal ref struct HashCodeCombiner
{
    private long _combinedHash64;

    public int CombinedHash
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { return _combinedHash64.GetHashCode(); }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HashCodeCombiner(long seed)
    {
        _combinedHash64 = seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator int(HashCodeCombiner self)
    {
        return self.CombinedHash;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(int i)
    {
        _combinedHash64 = ((_combinedHash64 << 5) + _combinedHash64) ^ i;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(T? o)
    {
        Add(o?.GetHashCode() ?? 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<TValue>(TValue value, IEqualityComparer<TValue> comparer)
    {
        var hashCode = value != null ? comparer.GetHashCode(value) : 0;
        Add(hashCode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(ImmutableArray<T> array, IEqualityComparer<T> comparer)
    {
        if (array.IsDefault)
        {
            return;
        }

        foreach (var item in array)
        {
            Add(item, comparer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add<T>(ImmutableArray<T> array)
    {
        Add(array, EqualityComparer<T>.Default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashCodeCombiner Start()
    {
        return new HashCodeCombiner(0x1505L);
    }
}
