// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Concurrent;

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
                throw new System.ArgumentException("adding a duplicate");
            }
        }
    }
}
