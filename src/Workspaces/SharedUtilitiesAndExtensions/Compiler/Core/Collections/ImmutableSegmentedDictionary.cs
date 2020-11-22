// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Collections
{
    internal static class ImmutableSegmentedDictionary
    {
        public static ImmutableSegmentedDictionary<TKey, TValue> Create<TKey, TValue>()
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty;

        public static ImmutableSegmentedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey>? keyComparer)
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer);

        public static ImmutableSegmentedDictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer);

        public static ImmutableSegmentedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>()
            where TKey : notnull
            => Create<TKey, TValue>().ToBuilder();

        public static ImmutableSegmentedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IEqualityComparer<TKey>? keyComparer)
            where TKey : notnull
            => Create<TKey, TValue>(keyComparer).ToBuilder();

        public static ImmutableSegmentedDictionary<TKey, TValue>.Builder CreateBuilder<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
            where TKey : notnull
            => Create<TKey, TValue>(keyComparer, valueComparer).ToBuilder();

        public static ImmutableSegmentedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty.AddRange(items);

        public static ImmutableSegmentedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer).AddRange(items);

        public static ImmutableSegmentedDictionary<TKey, TValue> CreateRange<TKey, TValue>(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer, IEnumerable<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
            => ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(items);

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(items, keyComparer: null, valueComparer: null);

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items, IEqualityComparer<TKey>? keyComparer)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(items, keyComparer, valueComparer: null);

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> items, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
            where TKey : notnull
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            if (items is ImmutableSegmentedDictionary<TKey, TValue> existingDictionary)
                return existingDictionary.WithComparers(keyComparer, valueComparer);

            return ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer).AddRange(items);
        }

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(source, keySelector, elementSelector, keyComparer: null, valueComparer: null);

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey>? keyComparer)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(source, keySelector, elementSelector, keyComparer, valueComparer: null);

        public static ImmutableSegmentedDictionary<TKey, TValue> ToImmutableSegmentedDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
            where TKey : notnull
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));
            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));
            if (elementSelector is null)
                throw new ArgumentNullException(nameof(elementSelector));

            return ImmutableSegmentedDictionary<TKey, TValue>.Empty.WithComparers(keyComparer, valueComparer)
                .AddRange(source.Select(element => new KeyValuePair<TKey, TValue>(keySelector(element), elementSelector(element))));
        }

        public static ImmutableSegmentedDictionary<TKey, TSource> ToImmutableSegmentedDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(source, keySelector, elementSelector: Functions<TSource>.Identity, keyComparer: null, valueComparer: null);

        public static ImmutableSegmentedDictionary<TKey, TSource> ToImmutableSegmentedDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? keyComparer)
            where TKey : notnull
            => ToImmutableSegmentedDictionary(source, keySelector, elementSelector: Functions<TSource>.Identity, keyComparer, valueComparer: null);
    }
}
