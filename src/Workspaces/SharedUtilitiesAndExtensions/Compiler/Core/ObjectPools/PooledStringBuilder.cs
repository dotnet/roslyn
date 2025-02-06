// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class PooledStringBuilder : IPooled
{
    private static readonly ObjectPool<PooledStringBuilder> s_keepLargeInstancesPool = CreatePool();

    public static PooledDisposer<PooledStringBuilder> GetInstance(out StringBuilder instance)
        => GetInstance(discardLargeInstances: true, out instance);

    public static PooledDisposer<PooledStringBuilder> GetInstance(bool discardLargeInstances, out StringBuilder instance)
    {
        // If we're discarding large instances (the default behavior), then just use the normal pool.  If we're not, use
        // a specific pool so that *other* normal callers don't accidentally get it and discard it.
        var pooledInstance = discardLargeInstances ? GetInstance() : s_keepLargeInstancesPool.Allocate();
        instance = pooledInstance;
        return new PooledDisposer<PooledStringBuilder>(pooledInstance, discardLargeInstances);
    }

    void IPooled.Free(bool discardLargeInstances)
    {
        // If we're discarding large instances, use the default behavior (which already does that).  Otherwise, always
        // clear and free the instance back to its originating pool.
        if (discardLargeInstances)
        {
            Free();
        }
        else
        {
            this.Builder.Clear();
            _pool.Free(this);
        }
    }
}
