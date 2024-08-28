// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.PooledObjects;

internal partial class ArrayBuilder<T> : IPooled
{
    public static PooledDisposer<ArrayBuilder<T>> GetInstance(out ArrayBuilder<T> instance)
    {
        instance = GetInstance();
        return new PooledDisposer<ArrayBuilder<T>>(instance);
    }

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
}
