// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal static class Delta
{
    /// <summary>
    ///  Compares <paramref name="first"/> and <paramref name="second"/> and returns the items in
    ///  <paramref name="second"/> that are not in <paramref name="first"/>.
    /// </summary>
    public static ImmutableArray<T> Compute<T>(ImmutableArray<T> first, ImmutableArray<T> second)
        where T : IEquatable<T>
    {
        // If first is empty, the delta is everything in second.
        if (first.Length == 0)
        {
            return second;
        }

        // If second is empty, the result is an empty array.
        if (second.Length == 0)
        {
            return ImmutableArray<T>.Empty;
        }

        // Fill a hash set containing all of the items in first.
        using var _ = HashSetPool<T>.GetPooledObject(out var set);

        foreach (var item in first)
        {
            set.Add(item);
        }

        using var result = new PooledArrayBuilder<T>();

        // Finally, iterate through the items in second. If an item can
        // be added to the set, it is new and should be added to the result.
        foreach (var item in second)
        {
            if (!set.Contains(item))
            {
                result.Add(item);
            }
        }

        return result.ToImmutableAndClear();
    }
}
