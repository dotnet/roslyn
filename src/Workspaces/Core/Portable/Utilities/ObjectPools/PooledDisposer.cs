// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
