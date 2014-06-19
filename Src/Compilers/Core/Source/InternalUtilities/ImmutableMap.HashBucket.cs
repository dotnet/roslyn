using System;
using System.Collections.Generic;
using System.Diagnostics;
using Roslyn.Utilities;

namespace System.Collections.Immutable
{
    internal partial class ImmutableDictionary<K, V>
    {
        private class HashBucket : AbstractBucket
        {
            private readonly int hashRoll;
            private readonly uint used;
            private readonly AbstractBucket[] buckets;
            private int count; // mutate on construction

            private HashBucket(int hashRoll, uint used, AbstractBucket[] buckets, int count)
            {
                this.hashRoll = hashRoll & 31;
                this.used = used;
                this.buckets = buckets;
                this.count = count;
                Debug.Assert(this.buckets.Length == BitArithmeticUtilities.CountBits(this.used));
            }

            internal HashBucket(int suggestedHashRoll, AbstractValueOrListBucket bucket1, AbstractValueOrListBucket bucket2)
            {
                // find next hashRoll that causes these two to be slotted in different buckets
                var h1 = bucket1.Hash;
                var h2 = bucket2.Hash;
                Debug.Assert(h1 != h2);
                for (int i = 0; i < 32; i++)
                {
                    this.hashRoll = (suggestedHashRoll + i) & 31;
                    int s1 = this.ComputeLogicalSlot(h1);
                    int s2 = this.ComputeLogicalSlot(h2);
                    if (s1 != s2)
                    {
                        this.count = 2;
                        this.used = (1u << s1) | (1u << s2);
                        this.buckets = new AbstractBucket[2];
                        this.buckets[this.ComputePhysicalSlot(s1)] = bucket1;
                        this.buckets[this.ComputePhysicalSlot(s2)] = bucket2;
                        return;
                    }
                }

                throw new InvalidOperationException();
            }

            internal override int Count
            {
                get
                {
                    return this.count;
                }
            }

            internal override AbstractBucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<K> keyComparer, bool mutate)
            {
                int logicalSlot = ComputeLogicalSlot(bucket.Hash);
                if (IsInUse(logicalSlot))
                {
                    // if this slot is in use, then add the new item to the one in this slot
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var existing = this.buckets[physicalSlot];
                    
                    // suggest hash roll that will cause any nested hash bucket to use entirely new bits for picking logical slot
                    // note: we ignore passed in suggestion, and base new suggestion off current hash roll.

                    int existingCount = existing.Count;
                    var added = existing.Add(this.hashRoll + 5, bucket, keyComparer, mutate);
                    var newCount = this.count - existingCount + added.Count;

                    if (mutate)
                    {
                        this.buckets[physicalSlot] = added;
                        this.count = newCount;
                        return this;
                    }
                    else
                    {
                        var newBuckets = this.buckets.ReplaceAt(physicalSlot, added);
                        return new HashBucket(this.hashRoll, this.used, newBuckets, newCount);
                    }
                }
                else
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var newBuckets = this.buckets.InsertAt(physicalSlot, bucket);
                    var newUsed = InsertBit(logicalSlot, this.used);
                    return new HashBucket(this.hashRoll, newUsed, newBuckets, this.count + bucket.Count);
                }
            }

            internal override AbstractBucket Remove(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                int logicalSlot = ComputeLogicalSlot(hash);
                if (IsInUse(logicalSlot))
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var existing = this.buckets[physicalSlot];
                    AbstractBucket result = existing.Remove(hash, key, keyComparer);
                    if (result == null)
                    {
                        if (this.buckets.Length == 1)
                        {
                            return null;
                        }
                        else if (this.buckets.Length == 2)
                        {
                            return physicalSlot == 0 ? this.buckets[1] : this.buckets[0];
                        }
                        else
                        {
                            return new HashBucket(this.hashRoll, RemoveBit(logicalSlot, this.used), this.buckets.RemoveAt(physicalSlot), this.count - existing.Count);
                        }
                    }
                    else if (this.buckets[physicalSlot] != result)
                    {
                        return new HashBucket(this.hashRoll, this.used, this.buckets.ReplaceAt(physicalSlot, result), this.count - existing.Count + result.Count);
                    }
                }

                return this;
            }

            internal override ValueBucket Get(int hash, K key, IEqualityComparer<K> keyComparer)
            {
                int logicalSlot = ComputeLogicalSlot(hash);
                if (IsInUse(logicalSlot))
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    return this.buckets[physicalSlot].Get(hash, key, keyComparer);
                }

                return null;
            }

            internal override IEnumerable<AbstractBucket> GetAll()
            {
                return this.buckets;
            }

            private bool IsInUse(int logicalSlot)
            {
                return ((1 << logicalSlot) & this.used) != 0;
            }

            private int ComputeLogicalSlot(int hc)
            {
                uint uc = unchecked((uint)hc);
                var rotated = RotateRight(uc, this.hashRoll);
                return unchecked((int)(rotated & 31));
            }

            private static uint RotateRight(uint v, int n)
            {
                Debug.Assert(n >= 0 && n < 32);
                if (n == 0)
                {
                    return v;
                }

                return v >> n | (v << (32 - n));
            }

            private int ComputePhysicalSlot(int logicalSlot)
            {
                Debug.Assert(logicalSlot >= 0 && logicalSlot < 32);
                if (this.buckets.Length == 32)
                {
                    return logicalSlot;
                }

                if (logicalSlot == 0)
                {
                    return 0;
                }

                var mask = uint.MaxValue >> (32 - logicalSlot); // only count the bits up to the logical slot #
                return BitArithmeticUtilities.CountBits(this.used & mask);
            }

            private static uint InsertBit(int position, uint bits)
            {
                Debug.Assert(0 == (bits & (1u << position)));
                return bits | (1u << position);
            }

            private static uint RemoveBit(int position, uint bits)
            {
                Debug.Assert(0 != (bits & (1u << position)));
                return bits & ~(1u << position);
            }
        }
    }
}