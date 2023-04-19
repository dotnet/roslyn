// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.PooledObjects;

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

        public static void AddToAccumulator<TKey, T>(this Dictionary<TKey, object> accumulator, T item, Func<T, TKey> keySelector)
            where TKey : notnull
            where T : notnull
        {
            var key = keySelector(item);
            if (accumulator.TryGetValue(key, out var existingValueOrArray))
            {
                if (existingValueOrArray is not ArrayBuilder<T> arrayBuilder)
                {
                    // Just a single value in the accumulator so far.  Convert to using a builder.
                    arrayBuilder = ArrayBuilder<T>.GetInstance(capacity: 2);
                    arrayBuilder.Add((T)existingValueOrArray);
                    accumulator[key] = arrayBuilder;
                }

                arrayBuilder.Add(item);
            }
            else
            {
                // Nothing in the dictionary so far.  Add the item directly.
                accumulator.Add(key, item);
            }
        }
    }
}
