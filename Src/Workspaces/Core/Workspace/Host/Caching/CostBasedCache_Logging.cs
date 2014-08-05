// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class CostBasedCache<T> : IObjectCache<T> where T : class
    {
        // delegate cache to avoid allocations
        private readonly Func<long, T, bool, string> logAddOrAccessed;
        private readonly Func<long, int, string> logEvictedOrRemoved;
        private readonly Func<T, CacheEntry, CacheEntry> updateCacheEntry;
        private readonly Func<KeyValuePair<T, CacheEntry>, double> rankGetter;
        private readonly Comparison<SnapshotItem> comparer;

        // delegate that will return an identity of a cached item regardless of its version
        private readonly Func<T, string> uniqueIdGetter;

        // current cache id
        private readonly int cacheId;

        // cache hit rate for diagnostic purpose
        private float cacheHitRate;

        private void UpdateCacheHitRate(List<SnapshotItem> cacheItems)
        {
            var cacheHit = 0;

            for (int i = 0; i < cacheItems.Count; i++)
            {
                var item = cacheItems[i];
                if (item.Entry.CreatedTime < this.lastTimeTaskRan)
                {
                    cacheHit++;
                }
            }

            // item that has survived last eviction vs new item added after the last eviction
            this.cacheHitRate = (float)cacheHit / Math.Max(1, cacheItems.Count);
        }

        private void LogCacheRankInformation(List<SnapshotItem> snapshot)
        {
#if false
            // log everything inside of the cache for diagnostic purpose
            var now = Environment.TickCount;

            Func<SnapshotItem, string> logMessage = i =>
            {
                return string.Join(",", now, this.cacheId, i.Rank, i.Entry.AccessCount, i.Entry.CreatedTime, i.Entry.LastTimeAccessed, i.Entry.ItemId, this.uniqueIdGetter(i.Item));
            };

            foreach (var item in snapshot)
            {
                Logger.Log(FunctionId.Cache_ItemRank, logMessage, item);
            }
#endif
        }

        private string LogAddOrAccessed(long itemId, T item, bool cacheHit)
        {
            return string.Join(",", cacheId, itemId, item.GetType(), this.uniqueIdGetter(item), cacheHit);
        }

        private string LogEvictedOrRemoved(long itemId, int accessCount)
        {
            return string.Join(",", cacheId, itemId, accessCount);
        }

        // internal information to show in diagnostic
        internal long CurrentCostUpperBound
        {
            get
            {
                return this.currentCostUpperBound;
            }
        }

        internal int CurrentItemCount
        {
            get
            {
                return this.items.Count;
            }
        }

        internal long MinimumCost
        {
            get
            {
                return this.minCost;
            }
        }

        internal long MaximumCost
        {
            get
            {
                return this.maxCost;
            }
        }

        internal float CacheHitRate
        {
            get
            {
                return this.cacheHitRate;
            }
        }
    }
}