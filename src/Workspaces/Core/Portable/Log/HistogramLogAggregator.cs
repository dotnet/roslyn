// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Internal.Log;

/// <summary>
/// Defines a log aggregator to create a histogram
/// </summary>
internal sealed class HistogramLogAggregator<TKey> : AbstractLogAggregator<TKey, HistogramLogAggregator<TKey>.HistogramCounter> where TKey : notnull
{
    private readonly int _bucketSize;
    private readonly int _maxBucketValue;
    private readonly int _bucketCount;

    public HistogramLogAggregator(int bucketSize, int maxBucketValue)
    {
        if (bucketSize <= 0 || maxBucketValue <= 0 || maxBucketValue % bucketSize != 0)
        {
            throw new ArgumentException();
        }

        _bucketSize = bucketSize;
        _maxBucketValue = maxBucketValue;
        _bucketCount = maxBucketValue / bucketSize + 1;
    }

    protected override HistogramCounter CreateCounter()
        => new(_bucketSize, _maxBucketValue, _bucketCount);

    public void IncreaseCount(TKey key, int value)
    {
        var counter = GetCounter(key);
        counter.IncreaseCount(value);
    }

    public void LogTime(TKey key, TimeSpan timeSpan)
    {
        var counter = GetCounter(key);
        counter.IncreaseCount((int)timeSpan.TotalMilliseconds);
    }

    public HistogramCounter? GetValue(TKey key)
    {
        TryGetCounter(key, out var counter);
        return counter;
    }

    internal sealed class HistogramCounter
    {
        private readonly int[] _buckets;

        public int BucketCount { get; }
        public int BucketSize { get; }
        public int MaxBucketValue { get; }

        public HistogramCounter(int bucketSize, int maxBucketValue, int bucketCount)
        {
            Debug.Assert(bucketSize > 0 && maxBucketValue > 0 && bucketCount > 0);

            BucketSize = bucketSize;
            MaxBucketValue = maxBucketValue;
            BucketCount = bucketCount;
            _buckets = new int[BucketCount];
        }

        public void IncreaseCount(int value)
        {
            var bucket = GetBucket(value);
            _buckets[bucket]++;
        }

        public string GetBucketsAsString()
        {
            var pooledStringBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledStringBuilder.Builder;

            builder.Append('[');
            builder.Append(_buckets[0].ToString(System.Globalization.CultureInfo.InvariantCulture));

            for (var i = 1; i < _buckets.Length; ++i)
            {
                builder.Append(',');
                builder.Append(_buckets[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            builder.Append(']');
            return pooledStringBuilder.ToStringAndFree();
        }

        private int GetBucket(int value)
        {
            var bucket = value / BucketSize;
            if (bucket >= BucketCount)
            {
                bucket = BucketCount - 1;
            }

            return bucket;
        }

        /// <summary>
        /// Writes out these statistics to a property bag for sending to telemetry.
        /// </summary>
        /// <param name="prefix">The prefix given to any properties written. A period is used to delimit between the 
        /// prefix and the value.</param>
        public void WriteTelemetryPropertiesTo(Dictionary<string, object?> properties, string prefix)
        {
            prefix += ".";

            properties.Add(prefix + nameof(BucketSize), BucketSize);
            properties.Add(prefix + nameof(MaxBucketValue), MaxBucketValue);
            properties.Add(prefix + "Buckets", GetBucketsAsString());
        }
    }
}
