// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;

namespace Microsoft.AspNetCore.Razor.PooledObjects;

internal struct PooledArray<T> : IDisposable
{
    private readonly ArrayPool<T> _pool;
    private readonly int _minimumLength;
    private readonly bool _clearOnReturn;
    private T[]? _array;

    /// <summary>
    ///  Returns the array that was rented from <see cref="ArrayPool{T}.Shared"/>.
    /// </summary>
    /// <remarks>
    ///  Returns a non-null array until <see cref="PooledArray{T}"/> is disposed.
    /// </remarks>
    public readonly T[] Array => _array!;

    /// <summary>
    ///  Returns a <see cref="Span{T}"/> representing a portion of the rented array
    ///  from its start to the minimum length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  The array has been returned to the pool.
    /// </exception>
    public readonly Span<T> Span => _array!.AsSpan(0, _minimumLength);

    public PooledArray(ArrayPool<T> pool, int minimumLength, bool clearOnReturn)
        : this()
    {
        _pool = pool;
        _minimumLength = minimumLength;
        _clearOnReturn = clearOnReturn;
        _array = pool.Rent(minimumLength);
    }

    public void Dispose()
    {
        if (_array is T[] array)
        {
            _pool.Return(array, clearArray: _clearOnReturn);
            _array = null;
        }
    }
}
