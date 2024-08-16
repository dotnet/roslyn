// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static partial class ImmutableArrayExtensions
    {
        public static ImmutableArray<T> ToImmutableArray<T>(this HashSet<T> set)
        {
            return [.. set];
        }

        public static bool Contains<T>(this ImmutableArray<T> items, T item, IEqualityComparer<T>? equalityComparer)
            => items.IndexOf(item, 0, equalityComparer) >= 0;

        public static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this T[]? items)
        {
            if (items == null)
            {
                return [];
            }

            return ImmutableArray.Create<T>(items);
        }

        public static ImmutableArray<T> TakeAsArray<T>(this ImmutableArray<T> array, int count)
        {
            var result = new FixedSizeArrayBuilder<T>(count);
            for (var i = 0; i < count; i++)
                result.Add(array[i]);

            return result.MoveToImmutable();
        }

        public static ImmutableArray<T> ToImmutableAndClear<T>(this ImmutableArray<T>.Builder builder)
        {
            if (builder.Count == 0)
                return ImmutableArray<T>.Empty;

            if (builder.Count == builder.Capacity)
                return builder.MoveToImmutable();

            var result = builder.ToImmutable();
            builder.Clear();
            return result;
        }
    }
}
