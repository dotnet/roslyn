// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    // Dictionary that can be recycled via an object pool
    // NOTE: these dictionaries always have the default comparer.
    internal sealed class PooledDictionary<K, V> : Dictionary<K, V>, IDisposable
        where K : notnull
    {
        private readonly ObjectPool<PooledDictionary<K, V>>? _pool;

        private PooledDictionary(ObjectPool<PooledDictionary<K, V>>? pool, IEqualityComparer<K>? keyComparer)
            : base(keyComparer)
        {
            _pool = pool;
        }

        public void Dispose() => Free(CancellationToken.None);

        public ImmutableDictionary<K, V> ToImmutableDictionaryAndFree()
        {
            ImmutableDictionary<K, V> result;
            if (Count == 0)
            {
                result = ImmutableDictionary<K, V>.Empty;
            }
            else
            {
                result = this.ToImmutableDictionary(Comparer);
                this.Clear();
            }

            _pool?.Free(this, CancellationToken.None);
            return result;
        }

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

            _pool?.Free(this, CancellationToken.None);
            return result;
        }

        public void Free(CancellationToken cancellationToken)
        {
            // Do not free in presence of cancellation.
            // See https://github.com/dotnet/roslyn/issues/46859 for details.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            this.Clear();
            _pool?.Free(this, cancellationToken);
        }

        // global pool
        private static readonly ObjectPool<PooledDictionary<K, V>> s_poolInstance = CreatePool();
        private static readonly ConcurrentDictionary<IEqualityComparer<K>, ObjectPool<PooledDictionary<K, V>>> s_poolInstancesByComparer
            = new();

        // if someone needs to create a pool;
        public static ObjectPool<PooledDictionary<K, V>> CreatePool(IEqualityComparer<K>? keyComparer = null)
        {
            ObjectPool<PooledDictionary<K, V>>? pool = null;
            pool = new ObjectPool<PooledDictionary<K, V>>(() => new PooledDictionary<K, V>(pool, keyComparer), 128);
            return pool;
        }

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
