// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal record struct BuilderAndStatistics<TValue>(
        ImmutableArray<TValue>.Builder Builder)
    {
        /// <summary>
        /// The number of times this item has been added back to the pool.  Once this goes past some threshold
        /// we will start checking if we're continually returning a large array that is mostly empty.  If so, we
        /// will try to lower the capacity of the array to prevent wastage.
        /// </summary>
        public int NumberOfTimesPooled;

        /// <summary>
        /// The number of times we returned a large array to the pool that was barely filled.  If this is a
        /// significant number of the total times pooled, then we will attempt to lower the capacity of the
        /// array.
        /// </summary>
        public int NumberOfTimesPooledWhenSparse;

        public int Count
            => Builder.Count;

        public void Add(TValue value)
            => Builder.Add(value);

        public ImmutableArray<TValue> ToImmutable()
            => Builder.ToImmutable();

        public TValue this[int index]
            => Builder[index];

        public bool Any(Func<TValue, bool> predicate)
            => Builder.Any(predicate);
    }

    internal static class PoolingExtensions
    {
        public static BuilderAndStatistics<TValue> DequeuePooledItem<TValue>(this ConcurrentQueue<BuilderAndStatistics<TValue>> queue)
            => queue.TryDequeue(out var item) ? item : new BuilderAndStatistics<TValue> { Builder = ImmutableArray.CreateBuilder<TValue>() };

        public static void ReturnPooledItem<TValue>(
            this ConcurrentQueue<BuilderAndStatistics<TValue>> queue,
            BuilderAndStatistics<TValue> item)
        {
            // Don't bother shrinking the array for arrays less than this capacity.  They're not going to be a
            // huge waste of space so we can just pool them forever.
            const int MinCapacityToConsiderThreshold = 1000;

            // The number of times something is added/removed to the pool before we start considering
            // statistics. This is so that we have enough data to reasonably see if something is consistently
            // sparse.
            const int MinTimesPooledToConsiderStatistics = 100;

            // The ratio of Count/Capacity to be at to be considered sparse.  under this, there is a lot of
            // wasted space and we would prefer to just throw the array away.  Above this and we're reasonably
            // filling the array and should keep it around.
            const double SparseThresholdRatio = 0.25;

            // The ratio of times we pooled something sparse.  Once above this, we will jettison the array as
            // being not worth keeping.
            const double ConsistentlySparseRatio = 0.75;

            // Note: the values 0.25 and 0.75 were picked as they reflect the common array practicing of growing
            // by doubling.  So once we've grown so much that we're consistently under 25% of the array, then we
            // want to shrink down.  To prevent shrinking and inflating over and over again, we only shrink when
            // we're highly confident we're going to stay small.

            var builder = item.Builder;
            item.NumberOfTimesPooled++;

            // See if we're pooling something both large and sparse.
            if (builder.Capacity > MinCapacityToConsiderThreshold &&
                ((double)builder.Count / builder.Capacity) < SparseThresholdRatio)
            {
                CodeAnalysisEventSource.Log.PooledWhenSparse(builder.GetType(), builder.Count, builder.Capacity);
                item.NumberOfTimesPooledWhenSparse++;
            }

            builder.Clear();

            // See if this builder has been consistently sparse. If so then time to lower its capacity.
            if (item.NumberOfTimesPooled > MinTimesPooledToConsiderStatistics &&
                ((double)item.NumberOfTimesPooledWhenSparse / item.NumberOfTimesPooled) > ConsistentlySparseRatio)
            {
                CodeAnalysisEventSource.Log.HalvedCapacity(builder.GetType(), builder.Count, builder.Capacity);
                builder.Capacity /= 2;

                // Reset our statistics.  We'll wait another 100 pooling attempts to reassess if we need to
                // adjust the capacity here.
                item.NumberOfTimesPooled = 1;
                item.NumberOfTimesPooledWhenSparse = 0;
            }

            queue.Enqueue(item);
        }
    }
}
