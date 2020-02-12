﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static class IDictionaryExtensions
    {
        // Copied from ConcurrentDictionary since IDictionary doesn't have this useful method
        public static V GetOrAdd<K, V>(this IDictionary<K, V> dictionary, K key, Func<K, V> function)
            where K : notnull
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = function(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        [return: MaybeNull]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return default!;
        }

        public static void MultiAdd<TKey, TValue, TCollection>(this IDictionary<TKey, TCollection> dictionary, TKey key, TValue value)
            where TKey : notnull
            where TCollection : ICollection<TValue>, new()
        {
            if (!dictionary.TryGetValue(key, out var collection))
            {
                collection = new TCollection();
                dictionary.Add(key, collection);
            }

            collection.Add(value);
        }

        public static void MultiAdd<TKey, TValue>(this IDictionary<TKey, ArrayBuilder<TValue>> dictionary, TKey key, TValue value)
            where TKey : notnull
        {
            if (!dictionary.TryGetValue(key, out var builder))
            {
                builder = ArrayBuilder<TValue>.GetInstance();
                dictionary.Add(key, builder);
            }

            builder.Add(value);
        }

        public static void MultiAdd<TKey, TValue>(this IDictionary<TKey, ImmutableArray<TValue>> dictionary, TKey key, TValue value, ImmutableArray<TValue> defaultArray)
            where TKey : notnull
            where TValue : IEquatable<TValue>
        {
            if (!dictionary.TryGetValue(key, out var collection))
            {
                collection = ImmutableArray<TValue>.Empty;
            }

            dictionary[key] = collection.IsEmpty && value.Equals(defaultArray[0]) ? defaultArray : collection.Add(value);
        }

        public static void MultiRemove<TKey, TValue, TCollection>(this IDictionary<TKey, TCollection> dictionary, TKey key, TValue value)
            where TKey : notnull
            where TCollection : ICollection<TValue>
        {
            if (dictionary.TryGetValue(key, out var collection))
            {
                collection.Remove(value);

                if (collection.Count == 0)
                {
                    dictionary.Remove(key);
                }
            }
        }

        public static void MultiRemove<TKey, TValue>(this IDictionary<TKey, ImmutableArray<TValue>> dictionary, TKey key, TValue value)
            where TKey : notnull
        {
            if (dictionary.TryGetValue(key, out var collection))
            {
                if (collection.Length == 1)
                {
                    dictionary.Remove(key);
                }
                else
                {
                    dictionary[key] = collection.Remove(value);
                }
            }
        }
    }
}
