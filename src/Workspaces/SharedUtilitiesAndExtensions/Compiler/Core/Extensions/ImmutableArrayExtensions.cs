// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static class ImmutableArrayExtensions
    {
        internal static bool Contains<T>(this ImmutableArray<T> items, T item, IEqualityComparer<T>? equalityComparer)
            => items.IndexOf(item, 0, equalityComparer) >= 0;

        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this T[]? items)
        {
            if (items == null)
            {
                return ImmutableArray.Create<T>();
            }

            return ImmutableArray.Create<T>(items);
        }

        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T>? items)
        {
            if (items == null)
            {
                return ImmutableArray.Create<T>();
            }

            if (items is ImmutableArray<T> array)
            {
                return array.NullToEmpty();
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        internal static IReadOnlyList<T> ToBoxedImmutableArray<T>(this IEnumerable<T>? items)
        {
            if (items is null)
            {
                return SpecializedCollections.EmptyBoxedImmutableArray<T>();
            }

            if (items is ImmutableArray<T> array)
            {
                return array.IsDefaultOrEmpty ? SpecializedCollections.EmptyBoxedImmutableArray<T>() : (IReadOnlyList<T>)items;
            }

            if (items is ICollection<T> collection && collection.Count == 0)
            {
                return SpecializedCollections.EmptyBoxedImmutableArray<T>();
            }

            return ImmutableArray.CreateRange(items);
        }

        internal static ConcatImmutableArray<T> ConcatFast<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
            => new ConcatImmutableArray<T>(first, second);

        internal static bool TryCastArray<T, TOther>(this ImmutableArray<T> array, out ImmutableArray<TOther> result)
            where T : class
            where TOther : class
        {
            var builder = ArrayBuilder<TOther>.GetInstance(array.Length);
            foreach (var item in array)
            {
                if (item is TOther other)
                {
                    builder.Add(other);
                    continue;
                }

                builder.Free();
                result = default;
                return false;
            }

            result = builder.ToImmutableAndFree();
            return true;
        }

        internal static bool AreEquivalent<T, V>(this ImmutableArray<T> array, Func<T, V> selector, IEqualityComparer<V>? comparer = null)
        {
            comparer ??= EqualityComparer<V>.Default;
            if (array.Length > 0)
            {
                var first = selector(array[0]);
                for (var i = 1; i < array.Length; i++)
                {
                    if (!comparer.Equals(first, selector(array[i])))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
