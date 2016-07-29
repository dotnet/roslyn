// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution.Serialization
{
    /// <summary>
    /// This is just internal utility type to reduce allocations and reduntant code
    /// </summary>
    internal static class Creator
    {
        public static PooledObject<List<T>> CreateList<T>()
        {
            return SharedPools.Default<List<T>>().GetPooledObject();
        }

        public static PooledObject<List<T>> CreateList<T>(T item1, T item2)
        {
            var items = SharedPools.Default<List<T>>().GetPooledObject();

            items.Object.Add(item1);
            items.Object.Add(item2);

            return items;
        }

        public static PooledObject<List<T>> CreateList<T>(T item1, T item2, T item3, T item4, T item5, T item6, T item7, T item8)
        {
            var items = SharedPools.Default<List<T>>().GetPooledObject();

            items.Object.Add(item1);
            items.Object.Add(item2);
            items.Object.Add(item3);
            items.Object.Add(item4);
            items.Object.Add(item5);
            items.Object.Add(item6);
            items.Object.Add(item7);
            items.Object.Add(item8);

            return items;
        }
    }
}
