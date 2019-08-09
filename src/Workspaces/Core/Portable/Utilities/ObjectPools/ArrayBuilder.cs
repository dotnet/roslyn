// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.PooledObjects
{
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
}
