// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Host
{
    internal partial class CostBasedCache<T> : IObjectCache<T> where T : class
    {
        // entry that holds onto various info
        // not a struct since it will be stored in concurrent dictionary as a value
        // and that would wrap it into a class anyways
        private class CacheEntry
        {
            public readonly long ItemId;
            public readonly int CreatedTime;
            public readonly IWeakAction<T> EvictAction;

            public DateTime LastTimeAccessed;
            public int AccessCount;
            public long Cost;

            public CacheEntry(long itemId, IWeakAction<T> evictor)
            {
                this.ItemId = itemId;
                this.CreatedTime = Environment.TickCount;
                this.EvictAction = evictor;

                this.LastTimeAccessed = DateTime.UtcNow;
                this.AccessCount = 1;
                this.Cost = UnitializedCost;
            }
        }

        private struct SnapshotItem
        {
            public T Item { get; private set; }
            public double Rank { get; private set; }
            public CacheEntry Entry { get; private set; }

            public SnapshotItem(T item, double rank, CacheEntry entry)
                : this()
            {
                this.Item = item;
                this.Rank = rank;
                this.Entry = entry;
            }
        }
    }
}