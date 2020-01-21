// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal partial class PooledHashSet<T> : IPooled
    {
        public static PooledDisposer<PooledHashSet<T>> GetInstance(out PooledHashSet<T> instance)
        {
            instance = GetInstance();
            return new PooledDisposer<PooledHashSet<T>>(instance);
        }
    }
}
