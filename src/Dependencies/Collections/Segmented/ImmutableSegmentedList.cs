// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Collections
{
    internal static class ImmutableSegmentedList
    {
        /// <inheritdoc cref="ImmutableList.Create{T}()"/>
        public static ImmutableSegmentedList<T> Create<T>()
            => ImmutableSegmentedList<T>.Empty;

        /// <inheritdoc cref="ImmutableList.Create{T}(T)"/>
        public static ImmutableSegmentedList<T> Create<T>(T item)
            => ImmutableSegmentedList<T>.Empty.Add(item);

        /// <inheritdoc cref="ImmutableList.Create{T}(T[])"/>
        public static ImmutableSegmentedList<T> Create<T>(params T[] items)
            => ImmutableSegmentedList<T>.Empty.AddRange(items);

        /// <inheritdoc cref="ImmutableList.CreateBuilder{T}()"/>
        public static ImmutableSegmentedList<T>.Builder CreateBuilder<T>()
            => ImmutableSegmentedList<T>.Empty.ToBuilder();

        /// <inheritdoc cref="ImmutableList.CreateRange{T}(IEnumerable{T})"/>
        public static ImmutableSegmentedList<T> CreateRange<T>(IEnumerable<T> items)
            => ImmutableSegmentedList<T>.Empty.AddRange(items);

        /// <inheritdoc cref="ImmutableList.ToImmutableList{TSource}(IEnumerable{TSource})"/>
        public static ImmutableSegmentedList<T> ToImmutableSegmentedList<T>(this IEnumerable<T> source)
        {
            if (source is ImmutableSegmentedList<T> existingList)
                return existingList;

            return ImmutableSegmentedList<T>.Empty.AddRange(source);
        }

        /// <inheritdoc cref="ImmutableList.ToImmutableList{TSource}(ImmutableList{TSource}.Builder)"/>
        public static ImmutableSegmentedList<T> ToImmutableSegmentedList<T>(this ImmutableSegmentedList<T>.Builder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.ToImmutable();
        }
    }
}
