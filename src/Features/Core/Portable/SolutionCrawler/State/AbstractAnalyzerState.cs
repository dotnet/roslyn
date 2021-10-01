// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler.State
{
    internal abstract class AbstractAnalyzerState<TKey, TValue, TData>
    {
        protected readonly ConcurrentDictionary<TKey, CacheEntry> DataCache = new(concurrencyLevel: 2, capacity: 10);

        protected abstract TKey GetCacheKey(TValue value);
        protected abstract Solution GetSolution(TValue value);
        protected abstract bool ShouldCache(TValue value);
        protected abstract int GetCount(TData data);

        public int Count => DataCache.Count;

        public int GetDataCount(TKey key)
        {
            if (!DataCache.TryGetValue(key, out var entry))
            {
                return 0;
            }

            return entry.Count;
        }

        public virtual bool Remove(TKey id)
        {
            // remove doesn't actually remove data from the persistent storage
            // that will be automatically managed by the service itself.
            return DataCache.TryRemove(id, out _);
        }

        protected readonly struct CacheEntry
        {
            public readonly TData Data;
            public readonly int Count;

            public CacheEntry(TData data, int count)
            {
                Data = data;
                Count = count;
            }

            public bool HasCachedData => !object.Equals(Data, null);
        }
    }
}
