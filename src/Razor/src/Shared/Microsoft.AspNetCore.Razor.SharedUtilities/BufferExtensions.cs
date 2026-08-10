// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;

namespace System.Buffers;

internal static class BufferExtensions
{
    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <remarks>
    ///  The array is guaranteed to be at least <paramref name="minimumLength"/> in length. However,
    ///  it will likely be larger.
    /// </remarks>
    public static PooledArray<T> GetPooledArray<T>(this ArrayPool<T> pool, int minimumLength)
        => pool.GetPooledArray(minimumLength, clearOnReturn: false);

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <param name="clearOnReturn">
    ///  Indicates whether the contents of the array should be cleared before it is returned to the pool.
    /// </param>
    /// <remarks>
    ///  The array is guaranteed to be at least <paramref name="minimumLength"/> in length. However,
    ///  it will likely be larger.
    /// </remarks>
    public static PooledArray<T> GetPooledArray<T>(this ArrayPool<T> pool, int minimumLength, bool clearOnReturn)
        => new(pool, minimumLength, clearOnReturn);

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <param name="array">
    ///  The rented array.
    /// </param>
    /// <remarks>
    ///  The array is guaranteed to be at least <paramref name="minimumLength"/> in length. However,
    ///  it will likely be larger.
    /// </remarks>
    public static PooledArray<T> GetPooledArray<T>(this ArrayPool<T> pool, int minimumLength, out T[] array)
        => pool.GetPooledArray(minimumLength, clearOnReturn: false, out array);

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <param name="clearOnReturn">
    ///  Indicates whether the contents of the array should be cleared before it is returned to the pool.
    /// </param>
    /// <param name="array">
    ///  The rented array.
    /// </param>
    /// <remarks>
    ///  The array is guaranteed to be at least <paramref name="minimumLength"/> in length. However,
    ///  it will likely be larger.
    /// </remarks>
    public static PooledArray<T> GetPooledArray<T>(this ArrayPool<T> pool, int minimumLength, bool clearOnReturn, out T[] array)
    {
        var result = pool.GetPooledArray(minimumLength, clearOnReturn);
        array = result.Array;
        return result;
    }

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    ///  The rented array is provided as a <see cref="Span{T}"/> representing a portion of the rented array
    ///  from its start to the minimum length.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <param name="span">
    ///  The <see cref="Span{T}"/> representing a portion of the rented array from its start to the minimum length.
    /// </param>
    public static PooledArray<T> GetPooledArraySpan<T>(this ArrayPool<T> pool, int minimumLength, out Span<T> span)
        => pool.GetPooledArraySpan(minimumLength, clearOnReturn: false, out span);

    /// <summary>
    ///  Rents an array of the given minimum length from the specified <see cref="ArrayPool{T}"/>.
    ///  The rented array is provided as a <see cref="Span{T}"/> representing a portion of the rented array
    ///  from its start to the minimum length.
    /// </summary>
    /// <param name="pool">
    ///  The <see cref="ArrayPool{T}"/> to use.
    /// </param>
    /// <param name="minimumLength">
    ///  The minimum length of the array.
    /// </param>
    /// <param name="clearOnReturn">
    ///  Indicates whether the contents of the array should be cleared before it is returned to the pool.
    /// </param>
    /// <param name="span">
    ///  The <see cref="Span{T}"/> representing a portion of the rented array from its start to the minimum length.
    /// </param>
    public static PooledArray<T> GetPooledArraySpan<T>(this ArrayPool<T> pool, int minimumLength, bool clearOnReturn, out Span<T> span)
    {
        var result = pool.GetPooledArray(minimumLength, clearOnReturn);
        span = result.Span;
        return result;
    }
}
