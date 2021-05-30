﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// NOTE!!! adding duplicates will result in exceptions. 
        /// Being concurrent only allows accessing the dictionary without taking locks.
        /// Duplicate keys are still not allowed in the hashtable.
        /// If unsure about adding unique items use APIs such as TryAdd, GetOrAdd, etc...
        /// </summary>
        public static void Add<K, V>(this ConcurrentDictionary<K, V> dict, K key, V value)
            where K : notnull
        {
            if (!dict.TryAdd(key, value))
            {
                throw new ArgumentException("adding a duplicate", nameof(key));
            }
        }

        public static TValue GetOrAdd<TKey, TArg, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
            where TKey : notnull
        {
#if NETCOREAPP
            return dictionary.GetOrAdd(key, valueFactory, factoryArgument);
#else
            if (dictionary.TryGetValue(key, out var value))
                return value;

            using var _ = PooledDelegates.GetPooledFunction(valueFactory, factoryArgument, out var boundFunction);
            return dictionary.GetOrAdd(key, boundFunction);
#endif
        }
    }
}
