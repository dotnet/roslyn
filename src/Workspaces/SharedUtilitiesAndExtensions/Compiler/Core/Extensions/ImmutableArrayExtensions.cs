// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Utilities;

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

        /// <summary>
        /// Use to validate public API input for properties that are exposed as <see cref="IReadOnlyList{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IReadOnlyList<T> ToBoxedImmutableArrayWithNonNullItems<T>(this IEnumerable<T>? sequence, string argumentName) where T : class
        {
            var list = sequence.ToBoxedImmutableArray();

            if (list.Contains(default(T)))
            {
                ThrowArgumentItemNullException(list, argumentName);
            }

            return list;
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentItemNullException<T>(IReadOnlyList<T> list, string argumentName) where T : class
        {
            throw new ArgumentNullException($"{argumentName}[{list.IndexOf(null!)}]");
        }

        internal static ConcatImmutableArray<T> ConcatFast<T>(this ImmutableArray<T> first, ImmutableArray<T> second)
        {
            return new ConcatImmutableArray<T>(first, second);
        }
    }
}
