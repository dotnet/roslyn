// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class PooledHashSet<T> : IPooled, IReadOnlySet<T>
{
    public static PooledDisposer<PooledHashSet<T>> GetInstance(out PooledHashSet<T> instance)
    {
        instance = GetInstance();
        return new PooledDisposer<PooledHashSet<T>>(instance);
    }

    // Nothing special to do here.
    void IPooled.Free(bool discardLargeInstance)
        => this.Free();
}
