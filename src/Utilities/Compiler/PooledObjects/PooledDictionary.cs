// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities.PooledObjects
{
    // Dictionary that can be recycled via an object pool
    // NOTE: these dictionaries always have the default comparer.
    internal class PooledDictionary<K, V> : Dictionary<K, V>
    {
        private readonly ObjectPool<PooledDictionary<K, V>> _pool;

        private PooledDictionary(ObjectPool<PooledDictionary<K, V>> pool)
        {
            _pool = pool;
        }

        public ImmutableDictionary<K, V> ToImmutableDictionaryAndFree()
        {
            ImmutableDictionary<K, V> result;
            if (Count == 0)
            {
                result = ImmutableDictionary<K, V>.Empty;
            }
            else
            {
                result = this.ToImmutableDictionary();
                this.Clear();
            }

            _pool?.Free(this);
            return result;
        }

        public void Free()
        {
            this.Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledDictionary<K, V>> s_poolInstance = CreatePool();

        // if someone needs to create a pool;
        public static ObjectPool<PooledDictionary<K, V>> CreatePool()
        {
            ObjectPool<PooledDictionary<K, V>> pool = null;
            pool = new ObjectPool<PooledDictionary<K, V>>(() => new PooledDictionary<K, V>(pool), 128);
            return pool;
        }

        public static PooledDictionary<K, V> GetInstance()
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        public static PooledDictionary<K, V> GetInstance(IEnumerable<KeyValuePair<K, V>> initializer)
        {
            var instance = GetInstance();
            foreach (var kvp in initializer)
            {
                instance.Add(kvp.Key, kvp.Value);
            }

            return instance;
        }
    }
}
