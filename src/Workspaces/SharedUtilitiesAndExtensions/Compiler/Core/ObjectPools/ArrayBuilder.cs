// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

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

    public bool HasDuplicates()
        => HasDuplicates(static x => x);

    public bool HasDuplicates<U>(Func<T, U> selector)
    {
        switch (this.Count)
        {
            case 0:
            case 1:
                return false;

            case 2:
                return EqualityComparer<U>.Default.Equals(selector(this[0]), selector(this[1]));

            default:
                {
                    using var _ = PooledHashSet<U>.GetInstance(out var set);

                    foreach (var element in this)
                    {
                        if (!set.Add(selector(element)))
                            return true;
                    }

                    return false;
                }
        }
    }
}
