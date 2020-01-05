// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Defines a log aggregator to create a histogram
    /// </summary>
    internal sealed class HistogramLogAggregator : AbstractLogAggregator<HistogramLogAggregator.HistogramCounter>
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
        {
            return new HistogramCounter(_bucketSize, _maxBucketValue, _bucketCount);
        }

        public void IncreaseCount(object key, decimal value)
        {
            var counter = GetCounter(key);
            counter.IncreaseCount(value);
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

            public void IncreaseCount(decimal value)
            {
                var bucket = GetBucket(value);
                _buckets[bucket]++;
            }

            public string GetBucketsAsString()
            {
                var pooledStringBuilder = PooledStringBuilder.GetInstance();
                var builder = pooledStringBuilder.Builder;

                builder.Append('[');
                builder.Append(_buckets[0]);

                for (var i = 1; i < _buckets.Length; ++i)
                {
                    builder.Append(',');
                    builder.Append(_buckets[i]);
                }

                builder.Append(']');
                return pooledStringBuilder.ToStringAndFree();
            }

            private int GetBucket(decimal value)
            {
                var bucket = (int)Math.Floor(value / BucketSize);
                if (bucket >= BucketCount)
                {
                    bucket = BucketCount - 1;
                }

                return bucket;
            }
        }
    }
}
