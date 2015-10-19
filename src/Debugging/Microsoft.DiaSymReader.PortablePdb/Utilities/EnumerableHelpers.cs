// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal static class EnumerableHelpers
    {
        /// <summary>
        /// Groups specified entries by key optimizing for single-item groups. 
        /// The ordering of values within each bucket is the same as their ordering in the <paramref name="entries"/> sequence.
        /// </summary>
        public static IReadOnlyDictionary<K, KeyValuePair<V, ImmutableArray<V>>> GroupBy<K, V>(this IEnumerable<KeyValuePair<K, V>> entries, IEqualityComparer<K> keyComparer)
        {
            var builder = new Dictionary<K, KeyValuePair<V, ImmutableArray<V>.Builder>>(keyComparer);

            foreach (var entry in entries)
            {
                KeyValuePair<V, ImmutableArray<V>.Builder> existing;
                if (!builder.TryGetValue(entry.Key, out existing))
                {
                    builder[entry.Key] = KeyValuePair.Create(entry.Value, default(ImmutableArray<V>.Builder));
                }
                else if (existing.Value == null)
                {
                    var list = ImmutableArray.CreateBuilder<V>();
                    list.Add(existing.Key);
                    list.Add(entry.Value);
                    builder[entry.Key] = KeyValuePair.Create(default(V), list);
                }
                else
                {
                    existing.Value.Add(entry.Value);
                }
            }

            var result = new Dictionary<K, KeyValuePair<V, ImmutableArray<V>>>(builder.Count, keyComparer);
            foreach (var entry in builder)
            {
                result.Add(entry.Key, KeyValuePair.Create(entry.Value.Key, entry.Value.Value?.ToImmutable() ?? default(ImmutableArray<V>)));
            }

            return result;
        }
    }
}
