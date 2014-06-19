using System;
using System.Collections.Generic;

namespace System.Collections.Immutable
{
    internal partial class ImmutableDictionary<K, V>
    {
        private class ValueBucket : AbstractValueOrListBucket
        {
            internal ValueBucket(K key, V value, IEqualityComparer<K> keyComparer)
                : base(keyComparer.GetHashCode(key))
            {
                this.Key = key;
                this.Value = value;
            }

            internal K Key { get; private set; }
            internal V Value { get; private set; }

            internal override int Count
            {
                get
                {
                    return 1;
                }
            }

            internal override AbstractBucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<K> keyComparer, bool mutate)
            {
                if (this.Hash == bucket.Hash)
                {
                    if (keyComparer.Equals(this.Key, bucket.Key))
                    {
                        // overwrite of same key
                        return bucket;
                    }
                    else
                    {
                        // two of the same hash must be stored in a list
                        return new ListBucket(new ValueBucket[] { this, bucket });
                    }
                }
                else
                {
                    return new HashBucket(suggestedHashRoll, this, bucket);
                }
            }

            internal override AbstractBucket Remove(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                if (this.Hash == hash && keyComparer.Equals(this.Key, key))
                {
                    return null;
                }

                return this;
            }

            internal override ValueBucket Get(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                if (this.Hash == hash && keyComparer.Equals(this.Key, key))
                {
                    return this;
                }

                return null;
            }

            internal override IEnumerable<AbstractBucket> GetAll()
            {
                return new AbstractBucket[] { this };
            }
        }
    }
}