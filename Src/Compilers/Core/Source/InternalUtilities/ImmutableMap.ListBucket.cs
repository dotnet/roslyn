using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace System.Collections.Immutable
{
    internal partial class ImmutableDictionary<K, V>
    {
        private class ListBucket : AbstractValueOrListBucket
        {
            private readonly ValueBucket[] buckets;

            internal ListBucket(ValueBucket[] buckets)
                : base(buckets[0].Hash)
            {
                Debug.Assert(buckets.Length >= 2);
                this.buckets = buckets;
            }

            internal override int Count
            {
                get
                {
                    return this.buckets.Length;
                }
            }

            internal override AbstractBucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<K> keyComparer, bool mutate)
            {
                if (this.Hash == bucket.Hash)
                {
                    int pos = this.Find(bucket.Key, keyComparer);
                    if (pos >= 0)
                    {
                        if (mutate)
                        {
                           this.buckets[pos] = bucket;
                           return this;
                        }
                        else
                        {
                            return new ListBucket(this.buckets.ReplaceAt(pos, bucket));
                        }
                    }
                    else
                    {
                        return new ListBucket(this.buckets.InsertAt(this.buckets.Length, bucket));
                    }
                }
                else
                {
                    return new HashBucket(suggestedHashRoll, this, bucket);
                }
            }

            internal override AbstractBucket Remove(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                if (this.Hash == hash)
                {
                    int pos = this.Find(key, keyComparer);
                    if (pos >= 0)
                    {
                        if (this.buckets.Length == 1)
                        {
                            return null;
                        }
                        else if (this.buckets.Length == 2)
                        {
                            return pos == 0 ? this.buckets[1] : this.buckets[0];
                        }
                        else
                        {
                            return new ListBucket(this.buckets.RemoveAt(pos));
                        }
                    }
                }

                return this;
            }

            internal override ValueBucket Get(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                if (this.Hash == hash)
                {
                    int pos = this.Find(key, keyComparer);
                    if (pos >= 0)
                    {
                        return this.buckets[pos];
                    }
                }

                return null;
            }

            private int Find(K key, IEqualityComparer<K> keyComparer)
            {
                for (int i = 0; i < this.buckets.Length; i++)
                {
                    if (keyComparer.Equals(key, this.buckets[i].Key))
                    {
                        return i;
                    }
                }

                return -1;
            }

            internal override IEnumerable<AbstractBucket> GetAll()
            {
                return this.buckets;
            }
        }
    }
}