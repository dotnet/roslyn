// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Analyzer.Utilities.Extensions
{
    internal static class IEnumerableExtensions
    {
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T value)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            foreach (T v in source)
            {
                yield return v;
            }

            yield return value;
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new HashSet<T>(source, comparer);
        }

        public static ISet<T> ToSet<T>(this IEnumerable<T> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return source as ISet<T> ?? new HashSet<T>(source);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, IComparer<T> comparer)
        {
            return source.OrderBy(t => t, comparer);
        }

        public static IEnumerable<T> OrderBy<T>(this IEnumerable<T> source, Comparison<T> compare)
        {
            return source.OrderBy(new ComparisonComparer<T>(compare));
        }

        public static IEnumerable<T> Order<T>(this IEnumerable<T> source) where T : IComparable<T>
        {
            return source.OrderBy((t1, t2) => t1.CompareTo(t2));
        }

        private static readonly Func<object?, bool> s_notNullTest = x => x != null;

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : class
        {
            if (source == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return source.Where((Func<T?, bool>)s_notNullTest)!;
        }

        public static ImmutableArray<TSource> WhereAsArray<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selector)
        {
            var builder = ImmutableArray.CreateBuilder<TSource>();
            bool any = false;
            foreach (var element in source)
            {
                if (selector(element))
                {
                    any = true;
                    builder.Add(element);
                }
            }

            if (any)
            {
                return builder.ToImmutable();
            }
            else
            {
                return ImmutableArray<TSource>.Empty;
            }
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
            while (count > 0 && enumerator.MoveNext()) { count--; }

            return count > 0;
        }

        private class ComparisonComparer<T> : Comparer<T>
        {
            private readonly Comparison<T> _compare;

            public ComparisonComparer(Comparison<T> compare)
            {
                _compare = compare;
            }

            public override int Compare(T x, T y)
            {
                return _compare(x, y);
            }
        }
    }
}
