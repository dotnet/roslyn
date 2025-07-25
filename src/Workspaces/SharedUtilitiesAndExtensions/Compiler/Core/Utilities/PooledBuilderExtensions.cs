// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities;

internal static class PooledBuilderExtensions
{
    extension<K, V>(PooledDictionary<K, V> builders) where K : notnull
    {
        public Dictionary<K, V> ToDictionaryAndFree()
        {
            var dictionary = new Dictionary<K, V>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items);
            }

            builders.Free();
            return dictionary;
        }
    }

    extension<K, V>(PooledDictionary<K, ArrayBuilder<V>> builders) where K : notnull
    {
        public Dictionary<K, ImmutableArray<V>> ToMultiDictionaryAndFree()
        {
            var dictionary = new Dictionary<K, ImmutableArray<V>>(builders.Count);

            foreach (var (key, items) in builders)
            {
                dictionary.Add(key, items.ToImmutableAndFree());
            }

            builders.Free();
            return dictionary;
        }

        public ImmutableDictionary<K, ImmutableArray<V>> ToImmutableMultiDictionaryAndFree()
             => ToImmutableMultiDictionaryAndFree(builders, where: null, whereArg: 0);

        public ImmutableDictionary<K, ImmutableArray<V>> ToImmutableMultiDictionaryAndFree<TArg>(Func<K, TArg, bool>? where, TArg whereArg)
        {
            var result = ImmutableDictionary.CreateBuilder<K, ImmutableArray<V>>();

            foreach (var (key, items) in builders)
            {
                if (where == null || where(key, whereArg))
                {
                    result.Add(key, items.ToImmutableAndFree());
                }
                else
                {
                    items.Free();
                }
            }

            builders.Free();
            return result.ToImmutable();
        }
    }

    extension<K, V>(IReadOnlyDictionary<K, ArrayBuilder<V>> builders) where K : notnull
    {
        public void FreeValues()
        {
            foreach (var (_, items) in builders)
            {
                items.Free();
            }
        }
    }

    extension<T>(ArrayBuilder<ArrayBuilder<T>> builders)
    {
        public ImmutableArray<T> ToFlattenedImmutableArrayAndFree()
        {
            try
            {
                if (builders.Count == 0)
                {
                    return [];
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
