// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    /// <summary>
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> that can be recycled via an object pool.
    /// </summary>
    internal sealed class PooledConcurrentDictionary<K, V> : ConcurrentDictionary<K, V>, IDisposable
    {
        private readonly ObjectPool<PooledConcurrentDictionary<K, V>>? _pool;

        private PooledConcurrentDictionary(ObjectPool<PooledConcurrentDictionary<K, V>>? pool)
        {
            _pool = pool;
        }

        private PooledConcurrentDictionary(ObjectPool<PooledConcurrentDictionary<K, V>>? pool, IEqualityComparer<K> keyComparer)
            : base(keyComparer)
        {
            _pool = pool;
        }

        public void Dispose() => Free();

        public void Free()
        {
            this.Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledConcurrentDictionary<K, V>> s_poolInstance = CreatePool();
        private static readonly ConcurrentDictionary<IEqualityComparer<K>, ObjectPool<PooledConcurrentDictionary<K, V>>> s_poolInstancesByComparer
            = new ConcurrentDictionary<IEqualityComparer<K>, ObjectPool<PooledConcurrentDictionary<K, V>>>();

        // if someone needs to create a pool;
        public static ObjectPool<PooledConcurrentDictionary<K, V>> CreatePool(IEqualityComparer<K>? keyComparer = null)
        {
            ObjectPool<PooledConcurrentDictionary<K, V>>? pool = null;
            pool = new ObjectPool<PooledConcurrentDictionary<K, V>>(() =>
                keyComparer != null ?
                    new PooledConcurrentDictionary<K, V>(pool, keyComparer) :
                    new PooledConcurrentDictionary<K, V>(pool),
                128);
            return pool;
        }

        public static PooledConcurrentDictionary<K, V> GetInstance(IEqualityComparer<K>? keyComparer = null)
        {
            var pool = keyComparer == null ?
                s_poolInstance :
                s_poolInstancesByComparer.GetOrAdd(keyComparer, c => CreatePool(c));
            var instance = pool.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        public static PooledConcurrentDictionary<K, V> GetInstance(IEnumerable<KeyValuePair<K, V>> initializer, IEqualityComparer<K>? keyComparer = null)
        {
            var instance = GetInstance(keyComparer);
            foreach (var kvp in initializer)
            {
                var added = instance.TryAdd(kvp.Key, kvp.Value);
                Debug.Assert(added);
            }

            return instance;
        }
    }
}
