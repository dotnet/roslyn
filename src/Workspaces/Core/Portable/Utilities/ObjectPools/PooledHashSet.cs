// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
