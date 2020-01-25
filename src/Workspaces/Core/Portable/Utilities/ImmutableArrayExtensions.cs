﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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

        internal static ImmutableArray<T> ToImmutableArrayOrEmpty<T>(this ImmutableArray<T> items)
            => items.IsDefault ? ImmutableArray<T>.Empty : items;

        internal static IReadOnlyList<T> ToImmutableReadOnlyListOrEmpty<T>(this IEnumerable<T>? items)
        {
            if (items is ImmutableArray<T> array && !array.IsDefault)
            {
                return (IReadOnlyList<T>)items;
            }
            else
            {
                return items.ToImmutableArrayOrEmpty();
            }
        }

        internal static ConcatImmutableArray<T> ConcatFast<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
        {
            return new ConcatImmutableArray<T>(first, second);
        }
    }
}
