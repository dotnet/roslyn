// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects;

internal sealed partial class ArrayBuilder<T> : IPooled
{
    private static readonly ObjectPool<ArrayBuilder<T>> s_keepLargeInstancesPool = CreatePool();

    public static PooledDisposer<ArrayBuilder<T>> GetInstance(out ArrayBuilder<T> instance)
        => GetInstance(discardLargeInstances: true, out instance);

    public static PooledDisposer<ArrayBuilder<T>> GetInstance(int capacity, out ArrayBuilder<T> instance)
    {
        instance = GetInstance(capacity);
        return new PooledDisposer<ArrayBuilder<T>>(instance);
    }

    public static PooledDisposer<ArrayBuilder<T>> GetInstance(int capacity, T fillWithValue, out ArrayBuilder<T> instance)
    {
        instance = GetInstance(capacity, fillWithValue);
        return new PooledDisposer<ArrayBuilder<T>>(instance);
    }

    public static PooledDisposer<ArrayBuilder<T>> GetInstance(bool discardLargeInstances, out ArrayBuilder<T> instance)
    {
        // If we're discarding large instances (the default behavior), then just use the normal pool.  If we're not, use
        // a specific pool so that *other* normal callers don't accidentally get it and discard it.
        instance = discardLargeInstances ? GetInstance() : s_keepLargeInstancesPool.Allocate();
        return new PooledDisposer<ArrayBuilder<T>>(instance, discardLargeInstances);
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
            this.Clear();
            _pool?.Free(this);
        }
    }
}
