// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

#pragma warning disable CA1710 // Rename Microsoft.CodeAnalysis.PooledHashSet<T> to end in 'Collection'.
#pragma warning disable CA1000 // Do not declare static members on generic types
#pragma warning disable CA2237 // Add [Serializable] to PooledHashSet as this type implements ISerializable

namespace Analyzer.Utilities.PooledObjects
{
    // HashSet that can be recycled via an object pool
    // NOTE: these HashSets always have the default comparer.
    internal sealed class PooledHashSet<T> : HashSet<T>, IDisposable
    {
        private readonly ObjectPool<PooledHashSet<T>> _pool;

        private PooledHashSet(ObjectPool<PooledHashSet<T>> pool, IEqualityComparer<T> comparer)
            : base(comparer)
        {
            _pool = pool;
        }

        public void Dispose() => Free();

        public void Free()
        {
            this.Clear();
            _pool?.Free(this);
        }

        public ImmutableHashSet<T> ToImmutableAndFree()
        {
            ImmutableHashSet<T> result;
            if (Count == 0)
            {
                result = ImmutableHashSet<T>.Empty;
            }
            else
            {
                result = this.ToImmutableHashSet(Comparer);
                this.Clear();
            }

            _pool?.Free(this);
            return result;
        }

        // global pool
        private static readonly ObjectPool<PooledHashSet<T>> s_poolInstance = CreatePool();
        private static readonly ConcurrentDictionary<IEqualityComparer<T>, ObjectPool<PooledHashSet<T>>> s_poolInstancesByComparer
            = new ConcurrentDictionary<IEqualityComparer<T>, ObjectPool<PooledHashSet<T>>>();

        // if someone needs to create a pool;
        public static ObjectPool<PooledHashSet<T>> CreatePool(IEqualityComparer<T> comparer = null)
        {
            ObjectPool<PooledHashSet<T>> pool = null;
            pool = new ObjectPool<PooledHashSet<T>>(() => new PooledHashSet<T>(pool, comparer), 128);
            return pool;
        }

        public static PooledHashSet<T> GetInstance(IEqualityComparer<T> comparer = null)
        {
            var pool = comparer == null ?
                s_poolInstance :
                s_poolInstancesByComparer.GetOrAdd(comparer, c => CreatePool(c));
            var instance = pool.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }
    }
}
