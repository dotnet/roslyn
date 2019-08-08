// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Determines whether a sequence containsmore than <paramref name="count"/> elements.
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
