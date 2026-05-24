// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor;

namespace System.Collections.Generic;

internal static class HashSetExtensions
{
    // On .NET Framework, Enumerable.ToArray() will create a new empty array for any
    // empty IEnumerable<T>. This works around that extra allocation for HashSet<T>.
    public static T[] ToArray<T>(this HashSet<T> set)
        => set.Count == 0
            ? Array.Empty<T>()
            : ((IEnumerable<T>)set).ToArray();

    public static void AddRange<T>(this HashSet<T> set, ImmutableArray<T> array)
    {
        foreach (var item in array)
        {
            set.Add(item);
        }
    }

    /// <summary>
    ///  Copies the contents of the set to a destination <see cref="Span{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the set.</typeparam>
    /// <param name="set">The set to copy items from.</param>
    /// <param name="destination">The span to copy items into.</param>
    /// <exception cref="ArgumentNullException">
    ///  The <paramref name="set"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///  The destination span is shorter than the source set.
    /// </exception>
    public static void CopyTo<T>(this HashSet<T> set, Span<T> destination)
    {
        ArgHelper.ThrowIfNull(set);
        ArgHelper.ThrowIfDestinationTooShort(destination, set.Count);

        var index = 0;

        foreach (var item in set)
        {
            destination[index++] = item;
        }
    }
}
