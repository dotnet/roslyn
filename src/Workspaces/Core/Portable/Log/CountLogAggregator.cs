// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.Internal.Log;

internal class CountLogAggregator<TKey> : AbstractLogAggregator<TKey, CountLogAggregator<TKey>.Counter> where TKey : notnull
{
    protected override Counter CreateCounter()
        => new();

    public void SetCount(TKey key, int count)
    {
        var counter = GetCounter(key);
        counter.SetCount(count);
    }

    public void IncreaseCount(TKey key)
    {
        var counter = GetCounter(key);
        counter.IncreaseCount();
    }

    public void IncreaseCountBy(TKey key, int value)
    {
        var counter = GetCounter(key);
        counter.IncreaseCountBy(value);
    }

    public int GetCount(TKey key)
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
            => _count = count;

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
            => _count;
    }
}
