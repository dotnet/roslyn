// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static class PooledBuilderExtensions
    {
        public static Dictionary<K, V> ToDictionaryAndFree<K, V>(this PooledDictionary<K, V> builders)
            where K : notnull
        {
            var dictionary = new Dictionary<K, V>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items);
            }

            builders.Free();
            return dictionary;
        }

        public static Dictionary<K, ImmutableArray<V>> ToMultiDictionaryAndFree<K, V>(this PooledDictionary<K, ArrayBuilder<V>> builders)
            where K : notnull
        {
            var dictionary = new Dictionary<K, ImmutableArray<V>>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items.ToImmutableAndFree());
            }

            builders.Free();
            return dictionary;
        }

        public static ImmutableDictionary<K, ImmutableArray<V>> ToImmutableMultiDictionaryAndFree<K, V>(this PooledDictionary<K, ArrayBuilder<V>> builders)
            where K : notnull
        {
            var result = ImmutableDictionary.CreateBuilder<K, ImmutableArray<V>>();

            foreach (var (key, items) in builders)
            {
                result.Add(key, items.ToImmutableAndFree());
            }

            builders.Free();
            return result.ToImmutable();
        }

        public static void FreeValues<K, V>(this IReadOnlyDictionary<K, ArrayBuilder<V>> builders)
            where K : notnull
        {
            foreach (var (_, items) in builders)
            {
                items.Free();
            }
        }

        public static ImmutableArray<T> ToFlattenedImmutableArrayAndFree<T>(this ArrayBuilder<ArrayBuilder<T>> builders)
        {
            try
            {
                if (builders.Count == 0)
                {
                    return ImmutableArray<T>.Empty;
                }

                if (builders.Count == 1)
                {
                    return builders[0].ToImmutableAndFree();
                }

                var result = ArrayBuilder<T>.GetInstance(builders.Sum(b => b.Count));

                foreach (var builder in builders)
                {
                    result.AddRange(builder);
                    builder.Free();
                }

                return result.ToImmutableAndFree();
            }
            finally
            {
                builders.Free();
            }
        }
    }
}
