﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Immutable
{
    internal static class ImmutableArrayExtensions
    {
        /// <summary>
        /// Returns the number of elements in a sequence.
        /// </summary>
        /// <typeparam name="TSource">he type of the elements of <paramref name="source"/>.</typeparam>
        /// <param name="source">A sequence that contains elements to be counted.</param>
        /// <returns>The number of elements in the input sequence.</returns>
        public static int Count<TSource>(this ImmutableArray<TSource> source) => source.Length;

        /// <summary>
        /// Determines whether a sequence contains, exactly, <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="ImmutableArray{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains, exactly, <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        public static bool HasExactly<TSource>(this ImmutableArray<TSource> source, int count) => source.Length == count;

        /// <summary>
        /// Determines whether a sequence contains more than <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="ImmutableArray{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains more than <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        public static bool HasMoreThan<TSource>(this ImmutableArray<TSource> source, int count) => source.Length > count;

        /// <summary>
        /// Determines whether a sequence contains fewer than <paramref name="count"/> elements.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">The <see cref="ImmutableArray{TSource}"/> to check for cardinality.</param>
        /// <param name="count">The number of elements to ensure exists.</param>
        /// <returns><see langword="true" /> the source sequence contains less then <paramref name="count"/> elements; otherwise, <see langword="false" />.</returns>
        public static bool HasFewerThan<TSource>(this ImmutableArray<TSource> source, int count) => source.Length < count;

        /// <summary>
        /// Determines whether a sequence contains any elements.
        /// </summary>
        /// <typeparam name="T">The type of the elements of array.</typeparam>
        /// <typeparam name="TArg">The type of arg.</typeparam>
        /// <param name="array">The <see cref="Immutable.ImmutableArray{T}"/> whose elements to apply the predicate to.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <param name="arg">The argument to pass into the predicate.</param>
        /// <returns> true if any elements in the source sequence pass the test in the specified predicate otherwise, false.</returns>
        public static bool Any<T, TArg>(this ImmutableArray<T> array, Func<T, TArg, bool> predicate, TArg arg)
        {
            foreach (var a in array)
            {
                if (predicate(a, arg))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
