// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license 

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace System.Collections.Immutable
{
    internal static class ImmutableHashSetExtensions
    {
        public static ImmutableHashSet<T> AddRange<T>(this ImmutableHashSet<T> set1, IEnumerable<T> set2)
        {
            var builder = PooledHashSet<T>.GetInstance();
            
            foreach (var item in set1)
            {
                builder.Add(item);
            }

            foreach (var item in set2)
            {
                builder.Add(item);
            }

            return builder.ToImmutableAndFree();
        }

        public static ImmutableHashSet<T> IntersectSet<T>(this ImmutableHashSet<T> set1, ImmutableHashSet<T> set2)
        {
            var builder = PooledHashSet<T>.GetInstance();
            foreach (var item in set1)
            {
                if (set2.Contains(item))
                {
                    builder.Add(item);
                }
            }

            return builder.ToImmutableAndFree();
        }

        public static bool IsSubsetOfSet<T>(this ImmutableHashSet<T> set1, ImmutableHashSet<T> set2)
        {
            if (set1.Count > set2.Count)
            {
                return false;
            }

            foreach (var item in set1)
            {
                if (!set2.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }
    }
}