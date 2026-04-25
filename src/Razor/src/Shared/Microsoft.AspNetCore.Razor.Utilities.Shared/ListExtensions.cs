// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor;

internal static class ListExtensions
{
    /// <summary>
    ///  Set the <paramref name="list"/>'s capacity if it is less than <paramref name="newCapacity"/>.
    /// </summary>
    public static void SetCapacityIfLarger<T>(this List<T> list, int newCapacity)
    {
        if (list.Capacity < newCapacity)
        {
            list.Capacity = newCapacity;
        }
    }

    /// <summary>
    ///  Copies the elements of the <see cref="List{T}"/> to a new array, or returns an
    ///  empty array if the <see cref="List{T}"/> is null.
    /// </summary>
    /// <remarks>
    ///  On .NET Framework, <see cref="List{T}.ToArray()"/> will create a new empty array for any
    ///  empty <see cref="List{T}"/>. This method avoids that extra allocation.
    /// </remarks>
    public static T[] ToArrayOrEmpty<T>(this List<T>? list)
        => list?.Count > 0
            ? list.ToArray()
            : [];

    public static bool Any<T>(this List<T> list)
        => list.Count > 0;

    /// <summary>
    ///  Copies the contents of the list to a destination <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="list">The list to copy items from.</param>
    /// <param name="destination">The span to copy items into.</param>
    /// <exception cref="ArgumentNullException">
    ///  The <paramref name="list"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///  The destination span is shorter than the source list.
    /// </exception>
    public static void CopyTo<T>(this List<T> list, Span<T> destination)
    {
#if NET8_0_OR_GREATER
        CollectionExtensions.CopyTo(list, destination);
#else
        ArgHelper.ThrowIfNull(list);
        ArgHelper.ThrowIfDestinationTooShort(destination, list.Count);

        var index = 0;

        foreach (var item in list)
        {
            destination[index++] = item;
        }
#endif
    }
}
