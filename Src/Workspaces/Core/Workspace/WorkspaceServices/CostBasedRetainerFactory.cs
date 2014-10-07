using System;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// A RetainerFactory that creates retainers with a retention policy based on a 
    /// costing algorithm and preference toward retaining the most recently accessed values.
    /// </summary>
    public class CostBasedRetainerFactory<T> : IRetainerFactory<T> where T : class
    {
        private readonly Func<T, long> itemCost;
        private readonly long maxCost;
        private readonly int minCount;
        private readonly NonReentrantLock gate = new NonReentrantLock();
        private readonly LinkedHashQueue<T> mostRecentlyUsedList = new LinkedHashQueue<T>();
        private long currentCost;

        public CostBasedRetainerFactory(Func<T, long> itemCost, long maxCost, int minCount)
        {
            this.itemCost = itemCost;
            this.maxCost = maxCost;
            this.minCount = minCount;
        }

        private void Accessed(T item)
        {
            List<IRetainedObject> evictions = null;

            using (gate.DisposableWait(CancellationToken.None))
            {
                if (mostRecentlyUsedList.Enqueue(item))
                {
                    var cost = itemCost(item);
                    currentCost += cost;
                }

                while (currentCost > maxCost && mostRecentlyUsedList.Count > minCount)
                {
                    var evicted = mostRecentlyUsedList.Dequeue();
                    var cost = itemCost(evicted);
                    currentCost -= cost;

                    // let the object know about eviction if it wants to hear
                    var retained = evicted as IRetainedObject;
                    if (retained != null)
                    {
                        if (evictions == null)
                        {
                            evictions = new List<IRetainedObject>();
                        }

                        evictions.Add(retained);
                    }
                }
            }

            // evict outside of lock
            if (evictions != null)
            {
                foreach (var evicted in evictions)
                {
                    evicted.OnEvicted();
                }
            }
        }

        public IRetainer<T> CreateRetainer(T value)
        {
            return new CostBasedRetainer(value, this);
        }

        public void ClearPool()
        {
            using (gate.DisposableWait(CancellationToken.None))
            {
                this.mostRecentlyUsedList.Clear();
            }
        }

        private class CostBasedRetainer : Retainer<T>
        {
            private readonly WeakReference<T> weakValue;
            private readonly CostBasedRetainerFactory<T> factory;

            public CostBasedRetainer(T value, CostBasedRetainerFactory<T> factory)
            {
                this.weakValue = new WeakReference<T>(value);
                this.factory = factory;
                factory.Accessed(value);
            }

            public override T GetValue(CancellationToken cancellationToken = default(CancellationToken))
            {
                T value;
                if (this.weakValue.TryGetTarget(out value))
                {
                    this.factory.Accessed(value);
                    return value;
                }

                return default(T);
            }

            public override bool TryGetValue(out T value)
            {
                if (this.weakValue.TryGetTarget(out value))
                {
                    this.factory.Accessed(value);
                    return true;
                }

                return false;
            }
        }
    }
}
