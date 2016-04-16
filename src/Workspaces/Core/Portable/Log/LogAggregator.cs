// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    /// <summary>
    /// helper class to aggregate some numeric value log in client side
    /// </summary>
    internal class LogAggregator : IEnumerable<KeyValuePair<object, LogAggregator.Counter>>
    {
        private static int s_globalId;

        private readonly ConcurrentDictionary<object, Counter> _map = new ConcurrentDictionary<object, Counter>(concurrencyLevel: 2, capacity: 2);

        public static int GetNextId()
        {
            return Interlocked.Increment(ref s_globalId);
        }

        public static StatisticResult GetStatistics(List<int> values)
        {
            if (values.Count == 0)
            {
                return default(StatisticResult);
            }

            var max = int.MinValue;
            var min = int.MaxValue;

            var total = 0;
            for (var i = 0; i < values.Count; i++)
            {
                var current = values[i];
                max = max < current ? current : max;
                min = min > current ? current : min;

                total += current;
            }

            var mean = total / values.Count;
            var median = values[values.Count / 2];

            var range = max - min;
            var mode = values.GroupBy(i => i).OrderByDescending(g => g.Count()).FirstOrDefault().Key;

            return new StatisticResult(max, min, median, mean, range, mode, values.Count);
        }

        public void SetCount(object key, int count)
        {
            var counter = GetCounter(key);
            counter.SetCount(count);
        }

        public void IncreaseCount(object key)
        {
            var counter = GetCounter(key);
            counter.IncreaseCount();
        }

        public void IncreaseCountBy(object key, int value)
        {
            var counter = GetCounter(key);
            counter.IncreaseCountBy(value);
        }

        public int GetCount(object key)
        {
            Counter counter;
            if (_map.TryGetValue(key, out counter))
            {
                return counter.GetCount();
            }

            return 0;
        }

        public int GetAverage(string key)
        {
            return 0;
        }

        public IEnumerator<KeyValuePair<object, Counter>> GetEnumerator()
        {
            return _map.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private Counter GetCounter(object key)
        {
            return _map.GetOrAdd(key, _ => new Counter());
        }

        internal class Counter
        {
            private int _count;

            public void SetCount(int count)
            {
                _count = count;
            }

            public void IncreaseCount()
            {
                // Counter class probably not needed. but it is here for 2 reasons.
                // make handling concurrency easier and be a place holder for different type of counter
                Interlocked.Increment(ref _count);
            }

            public void IncreaseCountBy(int value)
            {
                // Counter class probably not needed. but it is here for 2 reasons.
                // make handling concurrency easier and be a place holder for different type of counter
                Interlocked.Add(ref _count, value);
            }

            public int GetCount()
            {
                return _count;
            }
        }

        internal struct StatisticResult
        {
            /// <summary>
            /// maximum value
            /// </summary>
            public readonly int Maximum;

            /// <summary>
            /// minimum value
            /// </summary>
            public readonly int Minimum;

            /// <summary>
            /// middle value of the total data set
            /// </summary>
            public readonly int Median;

            /// <summary>
            /// average value of the total data set
            /// </summary>
            public readonly int Mean;

            /// <summary>
            /// most frequent value in the total data set
            /// </summary>
            public readonly int Mode;

            /// <summary>
            /// difference between max and min value
            /// </summary>
            public readonly int Range;

            /// <summary>
            /// number of data points in the total data set
            /// </summary>
            public readonly int Count;

            public StatisticResult(int max, int min, int median, int mean, int range, int mode, int count)
            {
                this.Maximum = max;
                this.Minimum = min;
                this.Median = median;
                this.Mean = mean;
                this.Range = range;
                this.Mode = mode;
                this.Count = count;
            }
        }
    }
}
