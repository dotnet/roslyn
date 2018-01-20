// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal static class PooledBuilderExtensions
    {
        public static Dictionary<K, V> ToDictionaryAndFree<K, V>(this PooledDictionary<K, V> builders)
        {
            var dictionary = new Dictionary<K, V>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items);
            }

            builders.Free();
            return dictionary;
        }

        public static Dictionary<K, ImmutableArray<V>> ToDictionaryAndFree<K, V>(this PooledDictionary<K, ArrayBuilder<V>> builders)
        {
            var dictionary = new Dictionary<K, ImmutableArray<V>>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items.ToImmutableAndFree());
            }

            builders.Free();
            return dictionary;
        }

        public static ImmutableDictionary<K, ImmutableArray<V>> ToImmutableDictionaryAndFree<K, V>(this PooledDictionary<K, ArrayBuilder<V>> builders)
        {
            var result = ImmutableDictionary.CreateBuilder<K, ImmutableArray<V>>();

            foreach (var (key, items) in builders)
            {
                result.Add(key, items.ToImmutableAndFree());
            }

            builders.Free();
            return result.ToImmutable();
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
