// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class StatisticLogAggregator<TKey> : AbstractLogAggregator<TKey, StatisticLogAggregator<TKey>.StatisticCounter> where TKey : notnull
    {
        protected override StatisticCounter CreateCounter()
            => new();

        public void AddDataPoint(TKey key, int value)
        {
            var counter = GetCounter(key);
            counter.AddDataPoint(value);
        }

        public void AddDataPoint(TKey key, TimeSpan timeSpan)
        {
            AddDataPoint(key, (int)timeSpan.TotalMilliseconds);
        }

        public StatisticResult GetStatisticResult(TKey key)
        {
            if (TryGetCounter(key, out var counter))
            {
                return counter.GetStatisticResult();
            }

            return default;
        }

        internal sealed class StatisticCounter
        {
            private readonly object _lock = new();
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
                    return new StatisticResult(_maximum, _mininum, mean: (double)_total / _count, range: _maximum - _mininum, mode: null, count: _count);
                }
            }
        }
    }
}
