// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects
{
    internal sealed partial class PooledDictionary<K, V> : IPooled
    {
        public static PooledDisposer<PooledDictionary<K, V>> GetInstance(out PooledDictionary<K, V> instance)
        {
            instance = GetInstance();
            return new PooledDisposer<PooledDictionary<K, V>>(instance);
        }
    }
}
