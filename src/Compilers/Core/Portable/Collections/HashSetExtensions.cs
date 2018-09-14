// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal static class HashSetExtensions
    {
        internal static bool IsNullOrEmpty<T>(this HashSet<T> hashSet)
        {
            return hashSet == null || hashSet.Count == 0;
        }

        internal static bool InitializeAndAdd<T>(ref HashSet<T> hashSet, T item) where T : class
        {
            if (item is null)
            {
                return false;
            }
            else if (hashSet is null)
            {
                hashSet = new HashSet<T>();
            }

            return hashSet.Add(item);
        }
    }
}
