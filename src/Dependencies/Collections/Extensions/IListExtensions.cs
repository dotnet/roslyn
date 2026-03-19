// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis;

internal static class IListExtensions
{
    public static bool HasDuplicates<T>(this IReadOnlyList<T> list)
        => list.HasDuplicates(EqualityComparer<T>.Default);

    public static bool HasDuplicates<T>(this IReadOnlyList<T> list, IEqualityComparer<T> comparer)
        => list.HasDuplicates(static x => x, comparer);

    public static bool HasDuplicates<TItem, TValue>(this IReadOnlyList<TItem> list, Func<TItem, TValue> selector)
        => list.HasDuplicates(selector, EqualityComparer<TValue>.Default);

    /// <summary>
    /// Determines whether duplicates exist using given equality comparer.
    /// </summary>
    /// <param name="list">Array to search for duplicates</param>
    /// <returns>Whether duplicates were found</returns>
    /// <remarks>
    /// API proposal: https://github.com/dotnet/runtime/issues/30582.
    /// <seealso cref="ImmutableArrayExtensions.HasDuplicates{TItem, TValue}(System.Collections.Immutable.ImmutableArray{TItem}, Func{TItem, TValue}, IEqualityComparer{TValue})"/>
    /// <seealso cref="Roslyn.Utilities.EnumerableExtensions.HasDuplicates{TItem, TValue}(IEnumerable{TItem}, Func{TItem, TValue}, IEqualityComparer{TValue})"/>
    /// </remarks>
    internal static bool HasDuplicates<TItem, TValue>(this IReadOnlyList<TItem> list, Func<TItem, TValue> selector, IEqualityComparer<TValue> comparer)
    {
        switch (list.Count)
        {
            case 0:
            case 1:
                return false;

            case 2:
                return comparer.Equals(selector(list[0]), selector(list[1]));

            default:
                var set = comparer == EqualityComparer<TValue>.Default ? PooledHashSet<TValue>.GetInstance() : new HashSet<TValue>(comparer);
                var result = false;

                // index to avoid allocating enumerator
                for (int i = 0, n = list.Count; i < n; i++)
                {
                    if (!set.Add(selector(list[i])))
                    {
                        result = true;
                        break;
                    }
                }

                (set as PooledHashSet<TValue>)?.Free();
                return result;
        }
    }
}
