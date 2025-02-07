// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Linq;
using Analyzer.Utilities.PooledObjects;

namespace System.Collections.Immutable
{
    internal static class ImmutableHashSetExtensions
    {
        public static ImmutableHashSet<T> AddRange<T>(this ImmutableHashSet<T> set1, ImmutableHashSet<T> set2)
        {
            using var builder = PooledHashSet<T>.GetInstance();

            foreach (var item in set1)
            {
                builder.Add(item);
            }

            foreach (var item in set2)
            {
                builder.Add(item);
            }

            if (builder.Count == set1.Count)
            {
                return set1;
            }

            if (builder.Count == set2.Count)
            {
                return set2;
            }

            return builder.ToImmutable();
        }

        public static ImmutableHashSet<T> IntersectSet<T>(this ImmutableHashSet<T> set1, ImmutableHashSet<T> set2)
        {
            if (set1.IsEmpty || set2.IsEmpty)
            {
                return ImmutableHashSet<T>.Empty;
            }
            else if (set1.Count == 1)
            {
                return set2.Contains(set1.First()) ? set1 : ImmutableHashSet<T>.Empty;
            }
            else if (set2.Count == 1)
            {
                return set1.Contains(set2.First()) ? set2 : ImmutableHashSet<T>.Empty;
            }

            using var builder = PooledHashSet<T>.GetInstance();
            foreach (var item in set1)
            {
                if (set2.Contains(item))
                {
                    builder.Add(item);
                }
            }

            if (builder.Count == set1.Count)
            {
                return set1;
            }
            else if (builder.Count == set2.Count)
            {
                return set2;
            }

            return builder.ToImmutable();
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

        public static void AddIfNotNull<T>(this ImmutableHashSet<T>.Builder builder, T? item)
            where T : class
        {
            if (item != null)
            {
                builder.Add(item);
            }
        }
    }
}