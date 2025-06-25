// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Immutable
{
    internal static class ImmutableArrayExtensions
    {
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
    }
}
