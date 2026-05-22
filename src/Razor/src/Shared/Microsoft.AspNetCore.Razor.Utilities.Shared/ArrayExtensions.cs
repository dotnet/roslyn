// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor;

namespace Microsoft.AspNetCore.Razor;

internal static class ArrayExtensions
{
    /// <summary>
    ///  Creates a new span over the portion of the target array defined by an <see cref="Index"/> value.
    /// </summary>
    /// <param name="array">
    ///  The array to convert.
    /// </param>
    /// <param name="startIndex">
    ///  The starting index.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Index"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than 0 or greater than <paramref name="array"/>.Length.
    /// </exception>
    /// <exception cref="ArrayTypeMismatchException">
    ///  <paramref name="array"/> is covariant, and the array's type is not exactly <typeparamref name="T"/>[].
    /// </exception>
    public static Span<T> AsSpan<T>(this T[]? array, Index startIndex)
    {
#if NET
        return MemoryExtensions.AsSpan(array, startIndex);
#else
        if (array is null)
        {
            if (!startIndex.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startIndex));
            }

            return default;
        }

        return MemoryExtensions.AsSpan(array, startIndex.GetOffset(array.Length));
#endif
    }

    /// <summary>
    ///  Creates a new span over the portion of the target array defined by a <see cref="Range"/> value.
    /// </summary>
    /// <param name="array">
    ///  The array to convert.
    /// </param>
    /// <param name="range">
    ///  The range of the array to convert.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Range"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    /// <exception cref="ArrayTypeMismatchException">
    ///  <paramref name="array"/> is covariant, and the array's type is not exactly <typeparamref name="T"/>[].
    /// </exception>
    public static Span<T> AsSpan<T>(this T[]? array, Range range)
    {
#if NET
        return MemoryExtensions.AsSpan(array, range);
#else
        if (array is null)
        {
            if (!range.Start.Equals(Index.Start) || !range.End.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentNullException(nameof(array));
            }

            return default;
        }

        var (start, length) = range.GetOffsetAndLength(array.Length);
        return MemoryExtensions.AsSpan(array, start, length);
#endif
    }

    /// <summary>
    ///  Creates a new memory region over the portion of the target starting at the specified index
    ///  to the end of the array.
    /// </summary>
    /// <param name="array">
    ///  The array to convert.
    /// </param>
    /// <param name="startIndex">
    ///  The first position of the array.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Index"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="startIndex"/> is less than 0 or greater than <paramref name="array"/>.Length.
    /// </exception>
    /// <exception cref="ArrayTypeMismatchException">
    ///  <paramref name="array"/> is covariant, and the array's type is not exactly <typeparamref name="T"/>[].
    /// </exception>
    public static Memory<T> AsMemory<T>(this T[]? array, Index startIndex)
    {
#if NET
        return MemoryExtensions.AsMemory(array, startIndex);
#else
        if (array is null)
        {
            if (!startIndex.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(nameof(startIndex));
            }

            return default;
        }

        return MemoryExtensions.AsMemory(array, startIndex.GetOffset(array.Length));
#endif
    }

    /// <summary>
    ///  Creates a new memory region over the portion of the target array beginning at
    ///  inclusive start index of the range and ending at the exclusive end index of the range.
    /// </summary>
    /// <param name="array">
    ///  The array to convert.
    /// </param>
    /// <param name="range">
    ///  The range of the array to convert.
    /// </param>
    /// <remarks>
    ///  This uses Razor's <see cref="Range"/> type, which is type-forwarded on .NET.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///  <paramref name="array"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start or end index is not within the bounds of the string.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///  <paramref name="range"/>'s start index is greater than its end index.
    /// </exception>
    /// <exception cref="ArrayTypeMismatchException">
    ///  <paramref name="array"/> is covariant, and the array's type is not exactly <typeparamref name="T"/>[].
    /// </exception>
    public static Memory<T> AsMemory<T>(this T[]? array, Range range)
    {
#if NET
        return MemoryExtensions.AsMemory(array, range);
#else
        if (array is null)
        {
            if (!range.Start.Equals(Index.Start) || !range.End.Equals(Index.Start))
            {
                ThrowHelper.ThrowArgumentNullException(nameof(array));
            }

            return default;
        }

        var (start, length) = range.GetOffsetAndLength(array.Length);
        return MemoryExtensions.AsMemory(array, start, length);
#endif
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
        this (TKey key, TValue value)[] array, IEqualityComparer<TKey> keyComparer)
        where TKey : notnull
        => array.ToImmutableDictionary(keySelector: t => t.key, elementSelector: t => t.value, keyComparer);
}
