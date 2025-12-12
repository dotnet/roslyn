// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Microsoft.CodeAnalysis.PooledObjects
{
    // Dictionary that can be recycled via an object pool
    // NOTE: these dictionaries always have the default comparer.
    internal sealed partial class PooledDictionary<K, V>
    {
        public ImmutableDictionary<TKey, TValue> ToImmutableDictionaryAndFree<TKey, TValue>(
           Func<KeyValuePair<K, V>, TKey> keySelector, Func<KeyValuePair<K, V>, TValue> elementSelector, IEqualityComparer<TKey> comparer)
            where TKey : notnull
        {
            ImmutableDictionary<TKey, TValue> result;
            if (Count == 0)
            {
                result = ImmutableDictionary<TKey, TValue>.Empty;
            }
            else
            {
                result = this.ToImmutableDictionary(keySelector, elementSelector, comparer);
                this.Clear();
            }

            _pool?.Free(this);
            return result;
        }

        private static readonly ConcurrentDictionary<IEqualityComparer<K>, ObjectPool<PooledDictionary<K, V>>> s_poolInstancesByComparer
            = new();

        public static PooledDictionary<K, V> GetInstance(IEqualityComparer<K>? keyComparer = null)
        {
            var pool = keyComparer == null ?
                s_poolInstance :
                s_poolInstancesByComparer.GetOrAdd(keyComparer, CreatePool);
            var instance = pool.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        public static PooledDictionary<K, V> GetInstance(IEnumerable<KeyValuePair<K, V>> initializer, IEqualityComparer<K>? keyComparer = null)
        {
            var instance = GetInstance(keyComparer);
            foreach (var kvp in initializer)
            {
                instance.Add(kvp.Key, kvp.Value);
            }

            return instance;
        }
    }
}
