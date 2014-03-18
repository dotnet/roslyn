// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;

namespace Roslyn.Utilities
{
    internal static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// NOTE!!! adding duplicates will result in exceptions. 
        /// Being concurrent only allows accessing the dictionary without takind locks.
        /// Duplicate keys are still not allowed in the hashtable.
        /// If unsure about adding unique items use APIs such as TryAdd, GetOrAdd, etc...
        /// </summary>
        public static void Add<K, V>(this ConcurrentDictionary<K, V> dict, K key, V value)
        {
            if (!dict.TryAdd(key, value))
            {
                throw new System.ArgumentException("adding a duplicate");
            }
        }
    }
}