// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Collections;

#pragma warning disable RS0001 // Use 'SpecializedCollections.EmptyEnumerable()'

namespace System.Linq
{
    /// <seealso cref="ImmutableArrayExtensions"/>
    internal static class ImmutableSegmentedListExtensions
    {
        public static bool All<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in immutableList)
            {
                if (!predicate(item))
                    return false;
            }

            return true;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T> immutableList)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));

            return !immutableList.IsEmpty;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Count > 0;
        }

        public static bool Any<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            foreach (var item in immutableList)
            {
                if (predicate(item))
                    return true;
            }

            return false;
        }

        public static T Last<T>(this ImmutableSegmentedList<T> immutableList)
        {
            // In the event of an empty list, generate the same exception
            // that the linq extension method would.
            return immutableList.Count > 0
                ? immutableList[immutableList.Count - 1]
                : Enumerable.Last(immutableList);
        }

        public static T Last<T>(this ImmutableSegmentedList<T>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            // In the event of an empty list, generate the same exception
            // that the linq extension method would.
            return builder.Count > 0
                ? builder[builder.Count - 1]
                : Enumerable.Last(builder);
        }

        public static T Last<T>(this ImmutableSegmentedList<T> immutableList, Func<T, bool> predicate)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (predicate is null)
                throw new ArgumentNullException(nameof(predicate));

            for (var i = immutableList.Count - 1; i >= 0; i--)
            {
                if (predicate(immutableList[i]))
                    return immutableList[i];
            }

            // Throw the same exception that LINQ would.
            return Enumerable.Empty<T>().Last();
        }

        public static IEnumerable<TResult> Select<T, TResult>(this ImmutableSegmentedList<T> immutableList, Func<T, TResult> selector)
        {
            if (immutableList.IsDefault)
                throw new ArgumentNullException(nameof(immutableList));
            if (selector is null)
                throw new ArgumentNullException(nameof(selector));

            if (immutableList.IsEmpty)
            {
                return Enumerable.Empty<TResult>();
            }
            else
            {
                return Enumerable.Select(immutableList, selector);
            }
        }
    }
}
