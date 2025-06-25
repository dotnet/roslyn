// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    /// <summary>
    /// <see cref="ConcurrentDictionary{TKey, TValue}"/> that can be recycled via an object pool.
    /// </summary>
    internal sealed class PooledConcurrentDictionary<K, V> : ConcurrentDictionary<K, V>, IDisposable
        where K : notnull
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

        public void Dispose() => Free(CancellationToken.None);

        public void Free(CancellationToken cancellationToken)
        {
            // Do not free in presence of cancellation.
            // See https://github.com/dotnet/roslyn/issues/46859 for details.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledConcurrentDictionary<K, V>> s_poolInstance = CreatePool();
        private static readonly ConcurrentDictionary<IEqualityComparer<K>, ObjectPool<PooledConcurrentDictionary<K, V>>> s_poolInstancesByComparer = new();

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
                s_poolInstancesByComparer.GetOrAdd(keyComparer, CreatePool);
            var instance = pool.Allocate();
            Debug.Assert(instance.IsEmpty);
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
