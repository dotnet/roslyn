// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

internal sealed class FixedSizeArrayBuilder<T>
{
    private static readonly ObjectPool<FixedSizeArrayBuilder<T>> s_pool = new(() => new());

    private T[] _values = Array.Empty<T>();
    private int _index;

    private FixedSizeArrayBuilder()
    {
    }

    public static PooledFixedSizeArrayBuilder GetInstance(int capacity, out FixedSizeArrayBuilder<T> builder)
    {
        builder = s_pool.Allocate();
        Contract.ThrowIfTrue(builder._values != Array.Empty<T>());
        Contract.ThrowIfTrue(builder._index != 0);
        builder._values = new T[capacity];

        return new(builder);
    }

    public void Add(T value)
        => _values[_index++] = value;

    public T this[int index]
    {
        get => _values[index];
        set => _values[index] = value;
    }

    public ImmutableArray<T> MoveToImmutable()
    {
        Contract.ThrowIfTrue(_index != _values.Length);
        var result = ImmutableCollectionsMarshal.AsImmutableArray(_values);
        _values = Array.Empty<T>();
        _index = 0;
        return result;
    }

    public struct PooledFixedSizeArrayBuilder(FixedSizeArrayBuilder<T> builder) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            Contract.ThrowIfTrue(_disposed);
            _disposed = true;

            // Put the builder back in the pool.  If we were in the middle of creating hte array, but never moved it
            // out, this will leak the array (can happen during things like cancellation).  That's acceptable as that
            // should not be the mainline path.  And, in that event, it's not like we can use that array anyways as it
            // won't be the right size for the next caller that needs a FixedSizeArrayBuilder of a different size.
            builder._values = Array.Empty<T>();
            builder._index = 0;
            s_pool.Free(builder);
        }
    }
}
