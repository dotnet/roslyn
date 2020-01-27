// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal struct PooledDisposer<TPoolable> : IDisposable
        where TPoolable : IPooled
    {
        private TPoolable _pooledObject;

        public PooledDisposer(TPoolable instance)
        {
            _pooledObject = instance;
        }

        public void Dispose()
        {
            var pooledObject = _pooledObject;
            if (pooledObject != null)
            {
                pooledObject.Free();
                _pooledObject = default;
            }
        }
    }

}
