// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal class PooledStopwatch : Stopwatch
    {
        private static readonly ObjectPool<PooledStopwatch> s_poolInstance = CreatePool();

        private readonly ObjectPool<PooledStopwatch> _pool;

        private PooledStopwatch(ObjectPool<PooledStopwatch> pool)
        {
            _pool = pool;
        }

        public void Free()
        {
            Reset();
            _pool?.Free(this);
        }

        public static ObjectPool<PooledStopwatch> CreatePool()
        {
            ObjectPool<PooledStopwatch> pool = null;
            pool = new ObjectPool<PooledStopwatch>(() => new PooledStopwatch(pool), 128);
            return pool;
        }

        public static PooledStopwatch StartInstance()
        {
            var instance = s_poolInstance.Allocate();
            instance.Restart();
            return instance;
        }
    }
}
