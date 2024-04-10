// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The collection of extension methods for the <see cref="Dictionary{TKey, TValue}"/> type
    /// </summary>
    internal static class DictionaryExtensions
    {
        /// <summary>
        /// If the given key is not found in the dictionary, add it with the given value and return the value.
        /// Otherwise return the existing value associated with that key.
        /// </summary>
        public static TValue GetOrAdd<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
            where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }
            else
            {
                dictionary.Add(key, value);
                return value;
            }
        }

#if !NETCOREAPP
        public static bool TryAdd<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue value)
            where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var _))
            {
                return false;
            }

            dictionary.Add(key, value);
            return true;
        }
#endif

        public static void AddPooled<K, V>(this IDictionary<K, ArrayBuilder<V>> dictionary, K key, V value)
            where K : notnull
        {
            if (!dictionary.TryGetValue(key, out var values))
            {
                values = ArrayBuilder<V>.GetInstance();
                dictionary[key] = values;
            }

            values.Add(value);
        }

        public static ImmutableSegmentedDictionary<K, ImmutableArray<V>> ToImmutableSegmentedDictionaryAndFree<K, V>(this IReadOnlyDictionary<K, ArrayBuilder<V>> builder)
            where K : notnull
        {
            var result = ImmutableSegmentedDictionary.CreateBuilder<K, ImmutableArray<V>>();
            foreach (var (key, values) in builder)
            {
                result.Add(key, values.ToImmutableAndFree());
            }

            return result.ToImmutable();
        }
    }
}
