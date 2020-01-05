// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class StatisticLogAggregator : AbstractLogAggregator<StatisticLogAggregator.StatisticCounter>
    {
        protected override StatisticCounter CreateCounter()
        {
            return new StatisticCounter();
        }

        public void AddDataPoint(object key, int value)
        {
            var counter = GetCounter(key);
            counter.AddDataPoint(value);
        }

        public StatisticResult GetStaticticResult(object key)
        {
            if (TryGetCounter(key, out var counter))
            {
                return counter.GetStatisticResult();
            }

            return default;
        }

        internal sealed class StatisticCounter
        {
            private readonly object _lock = new object();
            private int _count;
            private int _maximum;
            private int _mininum;

            private int _total;

            public void AddDataPoint(int value)
            {
                lock (_lock)
                {
                    if (_count == 0 || value > _maximum)
                    {
                        _maximum = value;
                    }

                    if (_count == 0 || value < _mininum)
                    {
                        _mininum = value;
                    }

                    _count++;
                    _total += value;
                }
            }

            public StatisticResult GetStatisticResult()
            {
                if (_count == 0)
                {
                    return default;
                }
                else
                {
                    return new StatisticResult(_maximum, _mininum, median: null, mean: _total / _count, range: _maximum - _mininum, mode: null, count: _count);
                }
            }
        }
    }
}
