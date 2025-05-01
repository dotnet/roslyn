// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class ImmutableSegmentedHashSet
    {
        /// <inheritdoc cref="ImmutableHashSet.Create{T}()"/>
        public static ImmutableSegmentedHashSet<T> Create<T>()
            => ImmutableSegmentedHashSet<T>.Empty;

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(T)"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(T item)
            => ImmutableSegmentedHashSet<T>.Empty.Add(item);

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(T[])"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(params T[] items)
            => ImmutableSegmentedHashSet<T>.Empty.Union(items);

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T})"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer)
            => ImmutableSegmentedHashSet<T>.Empty.WithComparer(equalityComparer);

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T}?, T)"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer, T item)
            => ImmutableSegmentedHashSet<T>.Empty.WithComparer(equalityComparer).Add(item);

        /// <inheritdoc cref="ImmutableHashSet.Create{T}(IEqualityComparer{T}?, T[])"/>
        public static ImmutableSegmentedHashSet<T> Create<T>(IEqualityComparer<T>? equalityComparer, params T[] items)
            => ImmutableSegmentedHashSet<T>.Empty.WithComparer(equalityComparer).Union(items);

        /// <inheritdoc cref="ImmutableHashSet.CreateBuilder{T}()"/>
        public static ImmutableSegmentedHashSet<T>.Builder CreateBuilder<T>()
            => ImmutableSegmentedHashSet<T>.Empty.ToBuilder();

        /// <inheritdoc cref="ImmutableHashSet.CreateBuilder{T}(IEqualityComparer{T}?)"/>
        public static ImmutableSegmentedHashSet<T>.Builder CreateBuilder<T>(IEqualityComparer<T>? equalityComparer)
            => ImmutableSegmentedHashSet<T>.Empty.WithComparer(equalityComparer).ToBuilder();

        /// <inheritdoc cref="ImmutableHashSet.CreateRange{T}(IEnumerable{T})"/>
        public static ImmutableSegmentedHashSet<T> CreateRange<T>(IEnumerable<T> items)
        {
            if (items is ImmutableSegmentedHashSet<T> existingSet)
                return existingSet.WithComparer(null);

            return ImmutableSegmentedHashSet<T>.Empty.Union(items);
        }

        /// <inheritdoc cref="ImmutableHashSet.CreateRange{T}(IEqualityComparer{T}?, IEnumerable{T})"/>
        public static ImmutableSegmentedHashSet<T> CreateRange<T>(IEqualityComparer<T>? equalityComparer, IEnumerable<T> items)
        {
            if (items is ImmutableSegmentedHashSet<T> existingSet)
                return existingSet.WithComparer(equalityComparer);

            return ImmutableSegmentedHashSet<T>.Empty.WithComparer(equalityComparer).Union(items);
        }

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(IEnumerable{TSource})"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this IEnumerable<TSource> source)
        {
            if (source is ImmutableSegmentedHashSet<TSource> existingSet)
                return existingSet.WithComparer(null);

            return ImmutableSegmentedHashSet<TSource>.Empty.Union(source);
        }

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource>? equalityComparer)
        {
            if (source is ImmutableSegmentedHashSet<TSource> existingSet)
                return existingSet.WithComparer(equalityComparer);

            return ImmutableSegmentedHashSet<TSource>.Empty.WithComparer(equalityComparer).Union(source);
        }

        /// <inheritdoc cref="ImmutableHashSet.ToImmutableHashSet{TSource}(ImmutableHashSet{TSource}.Builder)"/>
        public static ImmutableSegmentedHashSet<TSource> ToImmutableSegmentedHashSet<TSource>(this ImmutableSegmentedHashSet<TSource>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.ToImmutable();
        }
    }
}
