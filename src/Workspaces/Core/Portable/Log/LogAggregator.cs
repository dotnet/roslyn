// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal class LogAggregator : AbstractLogAggregator<LogAggregator.Counter>
    {
        protected override Counter CreateCounter()
        {
            return new Counter();
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
            if (TryGetCounter(key, out var counter))
            {
                return counter.GetCount();
            }

            return 0;
        }

        internal sealed class Counter
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
    }
}
