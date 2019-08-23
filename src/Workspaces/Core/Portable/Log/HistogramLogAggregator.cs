// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// Defines a log aggregator to create a histogram
    /// </summary>
    internal class HistogramLogAggregator : AbstractLogAggregator<LogAggregator.Counter>
    {
        private readonly int _bucketSize;
        private readonly int _maxBucketValue;

        public HistogramLogAggregator(int bucketSize, int maxBucketValue)
        {
            _bucketSize = bucketSize;
            _maxBucketValue = maxBucketValue;
        }

        public void IncreaseCount(decimal value)
        {
            var counter = GetCounter(GetBucket(value));
            counter.IncreaseCount();
        }

        public int GetCount(decimal value)
        {
            if (TryGetCounter(GetBucket(value), out var counter))
            {
                return counter.GetCount();
            }

            return 0;
        }

        protected override LogAggregator.Counter CreateCounter()
        {
            return new LogAggregator.Counter();
        }

        private int GetBucket(decimal value)
        {
            var bucket = (int)Math.Floor(value / _bucketSize) * _bucketSize;
            if (bucket > _maxBucketValue)
            {
                bucket = _maxBucketValue;
            }

            return bucket;
        }
    }
}
