// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Roslyn.Collections.Immutable;

namespace Roslyn.Utilities
{
    internal static class ImmutableHashMapExtensions
    {
        /// <summary>
        /// Obtains the value for the specified key from a dictionary, or adds a new value to the dictionary where the key did not previously exist.
        /// </summary>
        /// <typeparam name="TKey">The type of key stored by the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of value stored by the dictionary.</typeparam>
        /// <typeparam name="TArg">The type of argument supplied to the value factory.</typeparam>
        /// <param name="location">The variable or field to atomically update if the specified <paramref name="key" /> is not in the dictionary.</param>
        /// <param name="key">The key for the value to retrieve or add.</param>
        /// <param name="valueFactory">The function to execute to obtain the value to insert into the dictionary if the key is not found.</param>
        /// <param name="factoryArgument">The argument to pass to the value factory.</param>
        /// <returns>The value obtained from the dictionary or <paramref name="valueFactory" /> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableHashMap<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            Contract.ThrowIfNull(valueFactory);

            var map = Volatile.Read(ref location);
            Contract.ThrowIfNull(map);
            if (map.TryGetValue(key, out var existingValue))
            {
                return existingValue;
            }

            var newValue = valueFactory(key, factoryArgument);

            do
            {
                var augmentedMap = map.Add(key, newValue);
                var replacedMap = Interlocked.CompareExchange(ref location, augmentedMap, map);
                if (replacedMap == map)
                {
                    return newValue;
                }

                map = replacedMap;
            }
            while (!map.TryGetValue(key, out existingValue));

            return existingValue;
        }
    }
}
