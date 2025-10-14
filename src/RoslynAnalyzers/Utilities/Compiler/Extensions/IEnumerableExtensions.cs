// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Analyzer.Utilities.Extensions
{
    internal static class IEnumerableExtensions
    {
        public static ISet<T> ToSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source as ISet<T> ?? new HashSet<T>(source);
        }

        public static void Dispose<T>(this IEnumerable<T?> collection)
            where T : class, IDisposable
        {
            foreach (var item in collection)
            {
                item?.Dispose();
            }
        }

        /// <summary>
        /// Determines whether a sequence contains, exactly, <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains, exactly, <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static bool HasExactly<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ICollection<TSource> collectionoft)
            {
                return collectionoft.Count == count;
            }

            if (source is ICollection collection)
            {
                return collection.Count == count;
            }

            using var enumerator = source.GetEnumerator();
            while (count-- > 0)
            {
                if (!enumerator.MoveNext())
                {
                    return false;
                }
            }

            return !enumerator.MoveNext();
        }

        /// <summary>
        /// Determines whether a sequence contains more than <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains more than <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static bool HasMoreThan<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ICollection<TSource> collectionoft)
            {
                return collectionoft.Count > count;
            }

            if (source is ICollection collection)
            {
                return collection.Count > count;
            }

            using var enumerator = source.GetEnumerator();
            while (count-- > 0)
            {
                if (!enumerator.MoveNext())
                {
                    return false;
                }
            }

            return enumerator.MoveNext();
        }

        /// <summary>
        /// Determines whether a sequence contains fewer than <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">The <see cref="IEnumerable{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains less than <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        public static bool HasFewerThan<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (source is ICollection<TSource> collectionoft)
            {
                return collectionoft.Count < count;
            }

            if (source is ICollection collection)
            {
                return collection.Count < count;
            }

            using var enumerator = source.GetEnumerator();
            while (count > 0 && enumerator.MoveNext())
            {
                count--;
            }

            return count > 0;
        }
    }
}
