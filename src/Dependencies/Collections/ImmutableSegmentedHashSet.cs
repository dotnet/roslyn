// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class ImmutableSegmentedHashSet
    {
        /// <inheritdoc cref="ImmutableHashSet.Create{T}()"/>
        public static ImmutableSegmentedHashSet<T> Create<T>()
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(T)"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(T item)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(T[])"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(params T[] items)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T})"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T}?, T)"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer, T item)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T}?, T[])"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer, params T[] items)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.CreateBuilder{T}()"/>
        public static ImmutableSegmentedHashSet<T>.Builder CreateBuilder<T>()
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.CreateBuilder{T}(IEqualityComparer{T}?)"/>
        public static ImmutableSegmentedHashSet<T>.Builder CreateBuilder<T>(IEqualityComparer<T>? equalityComparer)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.CreateRange{T}(IEnumerable{T})"/>
        public static ImmutableSegmentedHashSet<T> CreateRange<T>(IEnumerable<T> items)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.CreateRange{T}(IEqualityComparer{T}?, IEnumerable{T})"/>
        public static ImmutableSegmentedHashSet<T> CreateRange<T>(IEqualityComparer<T>? equalityComparer, IEnumerable<T> items)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(IEnumerable{TSource})"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this IEnumerable<TSource> source)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? equalityComparer)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(ImmutableHashSet{TSource}.Builder)"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this ImmutableSegmentedHashSet<TSource>.Builder builder)
            => throw null!;
    }
}
