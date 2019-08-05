// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    // HashSet that can be recycled via an object pool
    // NOTE: these HashSets always have the default comparer.
    internal sealed class PooledHashSet<T> : HashSet<T>
    {
        private readonly ObjectPool<PooledHashSet<T>> _pool;

        private PooledHashSet(ObjectPool<PooledHashSet<T>> pool, IEqualityComparer<T> equalityComparer) :
            base(equalityComparer)
        {
            _pool = pool;
        }

        public void Free()
        {
            this.Clear();
            _pool?.Free(this);
        }

        // global pool
        private static readonly ObjectPool<PooledHashSet<T>> s_poolInstance = CreatePool(EqualityComparer<T>.Default);

        // if someone needs to create a pool;
        public static ObjectPool<PooledHashSet<T>> CreatePool(IEqualityComparer<T> equalityComparer)
        {
            ObjectPool<PooledHashSet<T>> pool = null;
            pool = new ObjectPool<PooledHashSet<T>>(() => new PooledHashSet<T>(pool, equalityComparer), 128);
            return pool;
        }

        public static PooledHashSet<T> GetInstance()
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

        public static PooledHashSetDisposer GetInstance(out PooledHashSet<T> instance)
        {
            instance = GetInstance();
            return new PooledHashSetDisposer(instance);
        }

        internal struct PooledHashSetDisposer : IDisposable
        {
            private bool _disposed;
            private readonly PooledHashSet<T> _pooledItem;

            public PooledHashSetDisposer(PooledHashSet<T> instance)
            {
                _disposed = false;
                _pooledItem = instance;
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _pooledItem.Free();
                }
            }
        }
    }
}
