// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class ImmutableArrayExtensions
    {
        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this T[] items)
        {
            if (items == null)
            {
                return ImmutableArray.Create<T>();
            }

            return ImmutableArray.Create<T>(items);
        }

        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this IEnumerable<T> items)
        {
            if (items == null)
            {
                return ImmutableArray.Create<T>();
            }

            return ImmutableArray.CreateRange<T>(items);
        }

        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this ImmutableArray<T> items)
        {
            if (items.IsDefault)
            {
                return ImmutableArray.Create<T>();
            }

            return items;
        }

        internal static IReadOnlyList<T> ToImmutableReadOnlyListOrEmpty<T>(this IEnumerable<T> items)
        {
            if (items is ImmutableArray<T> && !((ImmutableArray<T>)items).IsDefault)
            {
                return (IReadOnlyList<T>)items;
            }
            else
            {
                return items.ToImmutableArrayOrEmpty();
            }
        }
    }
}
