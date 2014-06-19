using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal partial class ImmutableDictionary<K, V>
    {
        private abstract class AbstractBucket
        {
            internal abstract int Count { get; }
            internal abstract AbstractBucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<K> keyComparer, bool mutate);
            internal abstract AbstractBucket Remove(int hash, K key, IEqualityComparer<K> keyComparer);
            internal abstract ValueBucket Get(int hash, K key, IEqualityComparer<K> keyComparer);
            internal abstract IEnumerable<AbstractBucket> GetAll();
        }
    }
}