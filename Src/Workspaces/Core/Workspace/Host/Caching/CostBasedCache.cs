// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Cost based cache with elastic upper bound.
    /// 
    /// This cache has two distinct characteristics where it will try to keep actively 
    /// referenced entries as long as possible but also tries to keep more data in the cache 
    /// if there is a sudden flood of data coming in.
    /// 
    /// The first part is achieved by giving more weight on items that are more frequently
    /// accessed. But also by gradually retiring them once they are not accessed, it should 
    /// give space back to newer data.
    /// 
    /// The second part is achieved by increasing the size of the upper bound of the cache when there is a
    /// sudden increase in the rate of data coming into the cache.
    /// 
    /// the size of the upper bound will be decreased to its normal level once those activities have ceased.
    /// </summary>
    internal partial class CostBasedCache<T> : IObjectCache<T> where T : class
    {
        // constant to indicate that cost is not evaluated yet.
        private const long UnitializedCost = -1;

        // how long it would take an item that got 10 accesses to become neutral.
        private const int FadeOutTimeInSeconds = 10;

        // cache hit threshold where it would slow down cache decay rate
        private const float CacheHitThreshold = 0.8F;

        // control variable for exponential decaying algorithm that is used to
        // determine current cost upper bound
        private readonly int fixedIncrementAmount;
        private readonly double coolingRate;

        // this is a buffer between two data points to collect information such as how many times
        // items are accessed and etc.
        private readonly int dataCollectionBufferInMS;

        // delegate that will return cost of an item
        private readonly Func<T, long> costCalculator;

        // cache storage
        private readonly ConcurrentDictionary<T, CacheEntry> items =
            new ConcurrentDictionary<T, CacheEntry>(ReferenceEqualityComparer.Instance);

        // pre-allocated list to be re-used during eviction
        private readonly List<SnapshotItem> evictionCandidateList = new List<SnapshotItem>();

        // option service
        private readonly IOptionService optionService;

        // hold onto last time the task ran
        private int lastTimeTaskRan;

        // current cost upper bound
        private long currentCostUpperBound;

        // thread that is successful at setting the slot gets to fill it with a running task.
        private Task taskSlot;

        // evict items without waiting for next item access
        protected bool eagarlyEvict;

        // minimum number of entries to keep in the cache
        protected int minCount;

        // lower and upper bound of the cache in terms of cost
        protected long minCost;
        protected long maxCost;

        protected CostBasedCache(
            IOptionService optionService,
            int minCount, long minCost, long maxCost,
            double coolingRate, int fixedIncAmount, TimeSpan dataCollectionTimeSpan,
            Func<T, long> costCalculator,
            Func<T, string> uniqueIdGetter)
            : this(optionService, minCount, minCost, maxCost, coolingRate, fixedIncAmount,
                   dataCollectionTimeSpan, costCalculator, false, uniqueIdGetter)
        {
        }

        protected CostBasedCache(
            IOptionService optionService,
            int minCount, long minCost, long maxCost,
            double coolingRate, int fixedIncAmount, TimeSpan dataCollectionTimeSpan,
            Func<T, long> costCalculator,
            bool eagarlyEvict,
            Func<T, string> uniqueIdGetter)
        {
            // set cache id for this cache
            this.cacheId = CacheId.NextCacheId();

            // log all variables used to create the cache. this should be called only once. should be okay to allocate the string.
            Logger.Log(FunctionId.Cache_Created, string.Join(",", cacheId, typeof(T), minCount, minCost, maxCost, coolingRate, fixedIncAmount, dataCollectionTimeSpan.TotalMilliseconds));

            this.minCount = minCount;
            this.minCost = minCost;
            this.maxCost = maxCost;

            this.fixedIncrementAmount = fixedIncAmount;
            this.coolingRate = coolingRate;

            this.dataCollectionBufferInMS = (int)dataCollectionTimeSpan.TotalMilliseconds;

            this.costCalculator = costCalculator;
            this.uniqueIdGetter = uniqueIdGetter;

            this.eagarlyEvict = eagarlyEvict;

            this.updateCacheEntry = UpdateCacheEntry;
            this.logAddOrAccessed = LogAddOrAccessed;
            this.logEvictedOrRemoved = LogEvictedOrRemoved;

            this.comparer = Comparer;

            // use environment tick here which is known to be the least expensive one to get and low
            // but good enough accuracy for the cache (15ms on most of system)
            this.lastTimeTaskRan = Environment.TickCount;

            // set base cost
            this.currentCostUpperBound = minCost;

            this.rankGetter = RankingAlgorithm;

            // respond to option change
            this.optionService = optionService;
            this.optionService.OptionChanged += OnOptionChanged;
        }

        private static double RankingAlgorithm(KeyValuePair<T, CacheEntry> item)
        {
            // There can be a race where we get a stale access count, 
            // but that should be good enough for our purpose.
            // 
            // This algorithm basically favors newly inserted item more. 
            // But it also bumps up the rank of items that are accessed more frequently. This should make items 
            // that are more frequently accessed stay longer in the cache. Example of such items are ones 
            // that are re-accessed repeatedly during typing, single file code actions, completion set etc.
            // 
            // By using logarithm, this algorithm should also make sure that even highly accessed files 
            // will be evicted from the cache once they are no longer accessed.
            // 
            // Current implementation should make a file that is accessed 10 times to have the same rank as newly 
            // inserted files after FadeOutTimeInSeconds since it is last accessed.
            // 
            // * the current FadeOutTimeInSeconds value was selected after several manual experiments, it seems that the value ensures that
            // most of the files used during typing remain in the cache even after suddenly increased traffic caused 
            // by things like compilation reading files to build up decl tables and etc.
            return Math.Log10(item.Value.AccessCount) - (DateTime.UtcNow - item.Value.LastTimeAccessed).TotalSeconds / FadeOutTimeInSeconds;
        }

        private static int Comparer(SnapshotItem x, SnapshotItem y)
        {
            // give it descending order
            return y.Rank.CompareTo(x.Rank);
        }

        public void AddOrAccess(T item, IWeakAction<T> evictor)
        {
            Contract.ThrowIfNull(evictor);

            // move up cost upper bound by fixed increase amount.
            Interlocked.Add(ref this.currentCostUpperBound, this.fixedIncrementAmount);

            // check whether it already exist. we do this here since we don't want to
            // create new CacheEntry unnecessarily
            CacheEntry existingEntry;
            var existed = this.items.TryGetValue(item, out existingEntry);

            // cache miss
            if (!existed)
            {
                // try add - could have been added by other threads
                var newCacheEntry = new CacheEntry(CacheId.NextItemId(), evictor);
                existingEntry = this.items.AddOrUpdate(item, newCacheEntry, updateCacheEntry);

                // it was already cached by other thread
                existed = newCacheEntry != existingEntry;
            }
            else
            {
                // there is a race here. entry might have been kicked out from the cache and we are changing a
                // stale one. the race could introduce a situation where an item that has been chosen for eviction by one thread
                // is being accessed here. that's unfortunate, but I feel it's acceptable.
                UpdateCacheEntry(item, existingEntry);
            }

            CreateEvictionTaskIfNecessary();

            Logger.Log(FunctionId.Cache_AddOrAccess, logAddOrAccessed, existingEntry.ItemId, item, existed);
        }

        private CacheEntry UpdateCacheEntry(T key, CacheEntry existingEntry)
        {
            // key is not necessary here, but it is a part of concurrent dictionary signature so we can't remove 
            // it from the parameters

            // there is a race, but the difference should be small enough not to affect ranking result
            existingEntry.LastTimeAccessed = DateTime.UtcNow;
            Interlocked.Increment(ref existingEntry.AccessCount);

            return existingEntry;
        }

        private void CreateEvictionTaskIfNecessary()
        {
            // make sure to give some time to the cache so that we can collect meaningful data 
            // before starting to kick out stuff from the cache.
            var delta = Environment.TickCount - this.lastTimeTaskRan;
            if (delta < this.dataCollectionBufferInMS)
            {
                return;
            }

            // If we don't already have an eviction task, and we're now storing
            // more items than our minimum, then create an eviction task to clear us old items
            // at some point in the future.
            if (this.taskSlot == null)
            {
                if (items.Count > minCount)
                {
                    // create a cold task
                    var task = new Task(Evict);
                    if (Interlocked.CompareExchange(ref this.taskSlot, task, null) == null)
                    {
                        // actually run the task
                        task.Start();
                    }
                }
            }
        }

        private double UpdateCostUpperBound()
        {
            var now = Environment.TickCount;
            var delta = now - this.lastTimeTaskRan;

            var currentUpperBound = this.currentCostUpperBound;

            // exponentially decay cost upper bound.
            //
            // this should make sure that when there is a sudden increase in traffic, rather than we go unbound (like a purely time based cache)
            // or take a perf hit (like a purely cost based cache), the size of the cache will temporarily grow up to the max cost. and it will decay
            // back to minCost once the access pattern dies down. how fast it would go back to minCost depends on the given cooling rate.
            //
            // combined with the ranking algorithm above, longer-lived items, such as those involved in low-latency scenarios (e.q. typing), should survive 
            // even after cache size reduced back to min cost most of times.
            //
            // this will also make sure cost is always in the range of [minCost,maxCost].

            // if, after last eviction, cache hit rate gets lower than the threshold, slow down decay rate. "5" will make
            // "decay to half" to take about twice as much time as the original decay rate.
            var currentCoolingRate = (this.cacheHitRate > CacheHitThreshold) ? this.coolingRate : this.coolingRate / 5;

            var newUpperBound = Math.Max(this.minCost, Math.Min((int)(currentUpperBound * Math.Exp(-currentCoolingRate * delta)), this.maxCost));

            // update the bound
            Interlocked.Exchange(ref this.currentCostUpperBound, newUpperBound);

            return newUpperBound;
        }

        private void Evict()
        {
            var costUpperBound = UpdateCostUpperBound();

            // Check if we have anything to evict.
            if (items.Count - this.minCount <= 0)
            {
                ResetEvictionStates();
                return;
            }

            // fill eviction list with snapshot of the item list and rank
            FillEvictionCandidateList(costUpperBound);

            // this should order things from hottest to coolest order based on rank calculated
            // at the moment of creating snapshot
            this.evictionCandidateList.Sort(this.comparer);

            UpdateCacheHitRate(this.evictionCandidateList);

            LogCacheRankInformation(this.evictionCandidateList);

            if (this.evictionCandidateList.Count > this.minCount)
            {
                long currentCost;
                var index = GetIndexAndCostToDelete(costUpperBound, this.evictionCandidateList, out currentCost);

                // go through the snapshot and remove items whose cost are above the upper bound
                for (int i = index; i < this.evictionCandidateList.Count; i++)
                {
                    var snapshotItem = this.evictionCandidateList[i];

                    // keep track of current cost as we evict items and if previous eviction made room in the cache
                    // keep the item
                    if (currentCost < costUpperBound)
                    {
                        currentCost += CalcuateCostAndUpdate(snapshotItem);
                        continue;
                    }

                    currentCost -= CalcuateCostAndUpdate(snapshotItem);

                    // there is a race where item might have been accessed during this eviction process.
                    // It is okay to ignore such cases and remove things based on info calculated at the moment
                    // we created the snapshot
                    CacheEntry entry;
                    if (items.TryRemove(snapshotItem.Item, out entry))
                    {
                        entry.EvictAction.Invoke(snapshotItem.Item);

                        Logger.Log(FunctionId.Cache_Evict, logEvictedOrRemoved, snapshotItem.Entry.ItemId, snapshotItem.Entry.AccessCount);
                    }
                }
            }

            ResetEvictionStates();

            EvictEagarly();
        }

        private void EvictEagarly()
        {
            if (!this.eagarlyEvict || this.taskSlot != null || items.Count <= minCount)
            {
                return;
            }

            var snapshot = items.ToArray();

            var cost = 0L;
            for (var i = 0; i < snapshot.Length; i++)
            {
                cost += CalcuateCostAndUpdate(new SnapshotItem(snapshot[i].Key, 0, snapshot[i].Value));

                if (cost > this.minCost)
                {
                    var task = Task.Delay(this.dataCollectionBufferInMS);
                    if (Interlocked.CompareExchange(ref this.taskSlot, task, null) == null)
                    {
                        task.ContinueWith(_ => Evict());
                    }

                    return;
                }
            }
        }

        private int GetIndexAndCostToDelete(double costUpperBound, List<SnapshotItem> snapshot, out long cost)
        {
            cost = 0L;
            for (int i = 0; i < snapshot.Count; i++)
            {
                var item = snapshot[i];

                cost += CalcuateCostAndUpdate(item);

                if ((i < this.minCount) || (cost < costUpperBound))
                {
                    continue;
                }

                return i;
            }

            return snapshot.Count;
        }

        private void ResetEvictionStates()
        {
            // clear eviction candidate list
            this.evictionCandidateList.Clear();

            // update the field to indicate task has ran
            this.lastTimeTaskRan = Environment.TickCount;

            // Clear the eviction task out. so that next access could kick off new task 
            // if needed
            this.taskSlot = null;
        }

        private void FillEvictionCandidateList(double upperBound)
        {
            // current time
            var current = Environment.TickCount;

            // ToArray is implemented by ConcurrentDictionary itself so should know how to do it.
            // (do not use LINQ for such things, it may crash if collection changes)
            var listSnapshot = this.items.ToArray();

            this.evictionCandidateList.Clear();
            for (int i = 0; i < listSnapshot.Length; i++)
            {
                // let items that have lived less than data collection buffer to be alive in the cache 
                // as long as cache is under this max cost threshold
                var entry = listSnapshot[i].Value;
                if ((current - entry.CreatedTime) < this.dataCollectionBufferInMS)
                {
                    continue;
                }

                this.evictionCandidateList.Add(new SnapshotItem(listSnapshot[i].Key, rankGetter(listSnapshot[i]), entry));
            }
        }

        private long CalcuateCostAndUpdate(SnapshotItem item)
        {
            // if it is already calcuated before, and if our thread already got the value propagated to,
            // use the cached value
            if (item.Entry.Cost != UnitializedCost)
            {
                return item.Entry.Cost;
            }

            // calculate the cost and try to cache it, again, there could be a race where two threads calculating same
            // cost and trying to set the field. but no big deal.
            Interlocked.CompareExchange(ref item.Entry.Cost, costCalculator(item.Item), UnitializedCost);

            // either we updated or other thread updated. so use the information cached.
            // Interlocked should have behaved as a full memory barrior making sure all threads see same value
            // for the field.
            return item.Entry.Cost;
        }

        public void Clear()
        {
            // Get readonly snapshot of keys.
            var itemsToEvict = items.Keys;

            // Now, actually remove the items from the cache.
            foreach (var item in itemsToEvict)
            {
                CacheEntry trackingValue;
                if (items.TryRemove(item, out trackingValue))
                {
                    trackingValue.EvictAction.Invoke(item);

                    Logger.Log(FunctionId.Cache_EvictAll, logEvictedOrRemoved, trackingValue.ItemId, trackingValue.AccessCount);
                }
            }
        }

        private void OnOptionChanged(object sender, OptionChangedEventArgs e)
        {
            OnOptionChanged(e);
        }

        protected virtual void OnOptionChanged(OptionChangedEventArgs e)
        {
            // do nothing
        }
    }
}