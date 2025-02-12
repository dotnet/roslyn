// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    // HashSet that can be recycled via an object pool
    // NOTE: these HashSets always have the default comparer.
    internal sealed partial class PooledHashSet<T> : HashSet<T>
#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
        , IPooled
#endif
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
            ObjectPool<PooledHashSet<T>>? pool = null;
            pool = new ObjectPool<PooledHashSet<T>>(() => new PooledHashSet<T>(pool!, equalityComparer), 128);
            return pool;
        }

        public static PooledHashSet<T> GetInstance()
        {
            var instance = s_poolInstance.Allocate();
            Debug.Assert(instance.Count == 0);
            return instance;
        }

#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
        public static PooledDisposer<PooledHashSet<T>> GetInstance(out PooledHashSet<T> instance)
        {
            instance = GetInstance();
            return new PooledDisposer<PooledHashSet<T>>(instance);
        }

        // Nothing special to do here.
        void IPooled.Free(bool discardLargeInstance)
            => this.Free();
#endif
    }
}
