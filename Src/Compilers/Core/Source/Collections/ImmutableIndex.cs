using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    // this a b-tree implemention of a K->V map.  You can access values via key or ordinal position
    public sealed class ImmutableIndex<K, V> : IEnumerable<KeyValuePair<K, V>>, IDictionary<K, V>
    {
        private readonly Bucket root;
        private readonly Comparer<K> comparer;
        private const int MaxBuckets = 7;

        private ImmutableIndex()
        {
            this.comparer = Comparer<K>.Default;
        }

        private ImmutableIndex(Bucket root)
            : this()
        {
            this.root = root;
        }

        public ImmutableIndex(K key, V value)
            : this()
        {
            this.root = this.Insert(null, new Bucket(key, value));
        }

        public ImmutableIndex(IEnumerable<KeyValuePair<K, V>> pairs)
            : this()
        {
            if (pairs != null)
            {
                var buckets = pairs.Select(p => new Bucket(p.Key, p.Value)).ToArray();
                foreach (var b in buckets)
                {
                    if (b.MinKey == null)
                    {
                        throw new ArgumentNullException("pairs");
                    }
                }

                var ordered = Order(buckets);
                int length = ordered.Length;
                this.RemoveDuplicates(ordered, 0, ref length);
                if (length > 0)
                {
                    this.root = this.Construct(ordered, 0, length);
                }
            }
        }

        private Bucket[] Order(Bucket[] buckets)
        {
            if (!IsOrdered(buckets))
            {
                buckets = buckets.OrderBy(b => b.MinKey).ToArray();
            }

            return buckets;
        }

        private bool IsOrdered(Bucket[] buckets)
        {
            for (int i = 1, n = buckets.Length; i < n; i++)
            {
                if (comparer.Compare(buckets[i].MinKey, buckets[i - 1].MinKey) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static readonly ImmutableIndex<K, V> Empty = new ImmutableIndex<K, V>();

        public int Count
        {
            get { return this.root != null ? this.root.Count : 0; }
        }

        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            Bucket bucket = this.root;

            while (bucket != null)
            {
                if (bucket.Buckets == null)
                {
                    if (this.comparer.Compare(bucket.MinKey, key) == 0)
                    {
                        value = bucket.Value;
                        return true;
                    }

                    break;
                }
                else
                {
                    int index = this.GetBucket(bucket.Buckets, key);
                    if (index < 0)
                    {
                        break;
                    }

                    bucket = bucket.Buckets[index];
                }
            }

            value = default(V);
            return false;
        }

        public int IndexOf(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            int position = 0;
            Bucket bucket = this.root;

            while (bucket != null)
            {
                if (bucket.Buckets == null)
                {
                    if (this.comparer.Compare(bucket.MinKey, key) == 0)
                    {
                        return position;
                    }

                    break;
                }
                else
                {
                    int index = this.GetBucket(bucket.Buckets, key);
                    if (index < 0)
                    {
                        break;
                    }

                    // increment position as we skip over sub-trees
                    for (int i = 0; i < index; i++)
                    {
                        position += bucket.Buckets[i].Count;
                    }

                    bucket = bucket.Buckets[index];
                }
            }

            return -1;
        }

        public bool GetAt(int index, out KeyValuePair<K, V> pair)
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Bucket bucket;
            if (this.GetBucketAt(index, out bucket))
            {
                pair = new KeyValuePair<K, V>(bucket.MinKey, bucket.Value);
                return true;
            }

            pair = default(KeyValuePair<K, V>);
            return false;
        }

        public bool GetKeyAt(int index, out K key)
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Bucket bucket;
            if (this.GetBucketAt(index, out bucket))
            {
                key = bucket.MinKey;
                return true;
            }

            key = default(K);
            return false;
        }

        public bool GetValueAt(int index, out V value)
        {
            if (index < 0 || index >= this.Count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            Bucket bucket;
            if (this.GetBucketAt(index, out bucket))
            {
                value = bucket.Value;
                return true;
            }

            value = default(V);
            return false;
        }

        private bool GetBucketAt(int index, out Bucket bucket)
        {
            Debug.Assert(index >= 0 && index < this.Count);
            bucket = this.root;
            int position = 0;

            while (bucket != null)
            {
                if (bucket.Buckets == null)
                {
                    return true;
                }
                else
                {
                    for (int i = 0, n = bucket.Buckets.Length; i < n; i++)
                    {
                        // are we in this bucket?
                        int ni = bucket.Buckets[i].Count;
                        if (index - (position + ni) > 0)
                        {
                            position += ni;
                        }
                        else
                        {
                            bucket = bucket.Buckets[index];
                            break;
                        }
                    }

                    // index is past the end?
                    break;
                }
            }

            return false;
        }

        public bool ContainsKey(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            V value;
            return this.TryGetValue(key, out value);
        }

        public V GetValueOrDefault(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            V value;
            this.TryGetValue(key, out value);
            return value;
        }

        public ImmutableIndex<K, V> Add(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            var newRoot = this.Insert(this.root, new Bucket(key, value));
            return new ImmutableIndex<K, V>(newRoot);
        }

        public ImmutableIndex<K, V> Add(IEnumerable<KeyValuePair<K, V>> pairs)
        {
            if (pairs != null)
            {
                var ordered = Order(pairs.Select(p => new Bucket(p.Key, p.Value)).ToArray());
                var length = ordered.Length;
                this.RemoveDuplicates(ordered, 0, ref length);
                if (length > 0)
                {
                    var newRoot = this.InsertOrdered(this.root, ordered, 0, length);
                    return new ImmutableIndex<K, V>(newRoot);
                }
            }

            return this;
        }

        private class Bucket
        {
            internal readonly K MinKey;
            internal readonly V Value;
            internal readonly Bucket[] Buckets;
            internal readonly int Count;

            internal Bucket(params Bucket[] buckets)
            {
                this.Buckets = buckets;
                this.MinKey = buckets[0].MinKey;
                System.Diagnostics.Debug.Assert(buckets.Length > 1);
                for (int i = 0, n = buckets.Length; i < n; i++)
                {
                    this.Count += buckets[i].Count;
                }
            }

            internal Bucket(K key, V value)
            {
                this.MinKey = key;
                this.Value = value;
                this.Count = 1;
            }
        }

        private int GetBucket(Bucket[] buckets, K key)
        {
            Debug.Assert(buckets != null);
            return buckets.BinarySearch(key, (k, bucket) => comparer.Compare(k, bucket.MinKey));
        }

        private Bucket InsertAt(Bucket bucket, Bucket newBucket, int index)
        {
            Bucket[] buckets = bucket.Buckets;
            Bucket[] newBuckets = new Bucket[buckets.Length + 1];
            if (index > 0)
            {
                Array.Copy(buckets, newBuckets, index);
            }

            if (index < buckets.Length)
            {
                Array.Copy(buckets, index, newBuckets, index + 1, buckets.Length - index);
            }

            newBuckets[index] = newBucket;
            if (newBuckets.Length > MaxBuckets)
            {
                Bucket[] leftBuckets = new Bucket[newBuckets.Length / 2];
                Bucket[] rightBuckets = new Bucket[newBuckets.Length - leftBuckets.Length];
                Array.Copy(newBuckets, leftBuckets, leftBuckets.Length);
                Array.Copy(newBuckets, leftBuckets.Length, rightBuckets, 0, rightBuckets.Length);
                newBuckets = new Bucket[2];
                newBuckets[0] = new Bucket(leftBuckets);
                newBuckets[1] = new Bucket(rightBuckets);
            }

            return new Bucket(newBuckets);
        }

        private Bucket ReplaceAt(Bucket bucket, Bucket newBucket, int index)
        {
            if (bucket.Buckets[index] != newBucket)
            {
                Bucket[] newBuckets = new Bucket[bucket.Buckets.Length];
                Array.Copy(bucket.Buckets, newBuckets, newBuckets.Length);
                newBuckets[index] = newBucket;
                return new Bucket(newBuckets);
            }

            return bucket;
        }

        private Bucket RemoveAt(Bucket bucket, int index)
        {
            Bucket[] buckets = bucket.Buckets;
            int len = buckets.Length;
            if (len == 1)
            {
                return null;
            }
            else if (len == 2)
            {
                return buckets[index == 0 ? 1 : 0];
            }
            else
            {
                Bucket[] newBuckets = new Bucket[len - 1];
                if (index > 0)
                {
                    Array.Copy(buckets, newBuckets, index);
                }

                if (index < newBuckets.Length)
                {
                    Array.Copy(buckets, index + 1, newBuckets, index, newBuckets.Length - index);
                }

                return new Bucket(newBuckets);
            }
        }

        private void RemoveDuplicates(Bucket[] ordered, int offset, ref int length)
        {
            int backIndex = 0;
            int frontIndex = 1;
            for (; frontIndex < length; frontIndex++, backIndex++)
            {
                while (frontIndex < length - 1 && this.comparer.Compare(ordered[backIndex].MinKey, ordered[frontIndex].MinKey) == 0)
                {
                    frontIndex++;
                }

                if (frontIndex - backIndex > 1)
                {
                    ordered[backIndex + 1] = ordered[frontIndex];
                }
            }

            length -= frontIndex - backIndex - 1;
        }

        private Bucket Construct(Bucket[] ordered, int offset, int length)
        {
            if (length == 1)
            {
                return ordered[offset];
            }
            else if (length <= MaxBuckets)
            {
                Bucket[] newBuckets = new Bucket[length];
                Array.Copy(ordered, offset, newBuckets, 0, length);
                return new Bucket(newBuckets);
            }
            else
            {
                int size = length / MaxBuckets;
                Bucket[] newBuckets = new Bucket[MaxBuckets];
                for (int i = 0, index = 0, n = newBuckets.Length; i < n; i++, index += size)
                {
                    int len = (i < n - 1) ? size : length - index;
                    newBuckets[i] = Construct(ordered, offset + index, len);
                }

                return new Bucket(newBuckets);
            }
        }

        private Bucket Insert(Bucket bucket, Bucket newBucket)
        {
            if (bucket == null)
            {
                return newBucket;
            }
            else if (bucket.Buckets == null)
            {
                int cmp = this.comparer.Compare(newBucket.MinKey, bucket.MinKey);
                if (cmp == 0)
                {
                    return newBucket;
                }
                else
                {
                    Bucket[] newBuckets = new Bucket[2];
                    if (cmp < 0)
                    {
                        newBuckets[0] = newBucket;
                        newBuckets[1] = bucket;
                    }
                    else
                    {
                        newBuckets[0] = bucket;
                        newBuckets[1] = newBucket;
                    }

                    return new Bucket(newBuckets);
                }
            }
            else
            {
                int index = this.GetBucket(bucket.Buckets, newBucket.MinKey);
                if (index >= 0)
                {
                    // this slot is a single value and new key goes after it
                    if (bucket.Buckets[index].Buckets == null && this.comparer.Compare(newBucket.MinKey, bucket.Buckets[index].MinKey) > 0)
                    {
                        return this.Rebalance(this.InsertAt(bucket, newBucket, index + 1));
                    }

                    // otherwise, insert it down one level
                    return this.Rebalance(this.ReplaceAt(bucket, this.Insert(bucket.Buckets[index], newBucket), index));
                }
                else
                {
                    // it goes before the first bucket, so prepend
                    return this.Rebalance(this.InsertAt(bucket, newBucket, 0));
                }
            }
        }

        private Bucket InsertOrdered(Bucket bucket, Bucket[] ordered, int offset, int length)
        {
            if (length == 1)
            {
                return this.Insert(bucket, ordered[offset]);
            }
            else if (bucket == null)
            {
                return this.Construct(ordered, offset, length);
            }
            else if (bucket.Buckets == null)
            {
                var constructed = this.Construct(ordered, offset, length);
                return this.Insert(constructed, bucket);
            }
            else
            {
                Bucket leftee = null;
                int index = 0;
                int bucketCount = bucket.Buckets.Length;
                Bucket[] newBuckets = new Bucket[bucketCount];
                Array.Copy(bucket.Buckets, newBuckets, bucketCount);
                while (index < length)
                {
                    int start = index;
                    int bucketIndex = this.GetBucket(bucket.Buckets, ordered[offset + start].MinKey);

                    // NOTE(cyrusn): This is to preserve the previous invariant 
                    // that bucketIndex was the actual bucket, or -1.  I'm not sure
                    // if the -1 is required for correct behavior, but the 
                    // following code is complex enough that i want to risk it.
                    if (bucketIndex < 0)
                    {
                        bucketIndex = -1;
                    }

                    while (index < length && (bucketIndex == bucketCount - 1 || this.comparer.Compare(ordered[offset + index].MinKey, bucket.Buckets[bucketIndex + 1].MinKey) < 0))
                    {
                        index++;
                    }

                    if (index > start)
                    {
                        // all these are belong to us
                        if (bucketIndex < 0)
                        {
                            leftee = this.Construct(ordered, offset + start, index - start);
                        }
                        else
                        {
                            newBuckets[bucketIndex] = this.InsertOrdered(newBuckets[bucketIndex], ordered, offset + start, index - start);
                        }
                    }
                }

                var merged = new Bucket(newBuckets);
                if (leftee != null)
                {
                    merged = this.InsertAt(merged, leftee, 0);
                }

                return merged;
            }
        }

        private Bucket MoveLeft(Bucket bucket, int index)
        {
            var leftMost = bucket.Buckets[index].Buckets[0];
            var rest = this.RemoveAt(bucket.Buckets[index], 0);
            var replaced = this.ReplaceAt(bucket, rest, index);
            if (index == 0 || bucket.Buckets.Length < MaxBuckets)
            {
                // insert it at this level if we can (or we have to)
                return this.InsertAt(replaced, leftMost, index);
            }
            else
            {
                // otherwise combine the left bucket with the left-most sub bucket at this index
                return this.ReplaceAt(replaced, new Bucket(replaced.Buckets[index - 1], leftMost), index - 1);
            }
        }

        private Bucket Rebalance(Bucket bucket)
        {
            if (bucket != null && bucket.Buckets != null)
            {
                if (bucket.Buckets.Length < MaxBuckets)
                {
                    int heavyIndex = 0;
                    int heavyCount = bucket.Buckets[0].Count;
                    for (int i = 1, n = bucket.Buckets.Length; i < n; i++)
                    {
                        if (bucket.Buckets[i].Count > heavyCount)
                        {
                            heavyIndex = i;
                            heavyCount = bucket.Buckets[i].Count;
                        }
                    }

                    if (heavyCount > MaxBuckets)
                    {
                        return this.MoveLeft(bucket, heavyIndex);
                    }
                }
            }

            return bucket;
        }

        public ImmutableIndex<K, V> Remove(K key)
        {
            var newRoot = this.Remove(this.root, key);
            if (newRoot == null)
            {
                return Empty;
            }

            return new ImmutableIndex<K, V>(newRoot);
        }

        public ImmutableIndex<K, V> Remove(IEnumerable<K> keys)
        {
            var newRoot = this.root;
            foreach (var key in keys)
            {
                newRoot = this.Remove(newRoot, key);
            }

            if (newRoot == null)
            {
                return Empty;
            }

            return new ImmutableIndex<K, V>(newRoot);
        }

        private Bucket Remove(Bucket bucket, K key)
        {
            if (bucket == null)
            {
                return null;
            }
            else if (bucket.Buckets == null)
            {
                int cmp = this.comparer.Compare(bucket.MinKey, key);
                if (cmp == 0)
                {
                    return null;
                }
            }
            else
            {
                int index = this.GetBucket(bucket.Buckets, key);
                if (index >= 0)
                {
                    var removed = this.Remove(bucket.Buckets[index], key);
                    if (removed != bucket.Buckets[index])
                    {
                        if (removed == null)
                        {
                            return this.RemoveAt(bucket, index);
                        }
                        else
                        {
                            return this.ReplaceAt(bucket, removed, index);
                        }
                    }
                }
            }

            return bucket;
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return new Enumerable(this.root).Select(b => new KeyValuePair<K, V>(b.MinKey, b.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<K> Keys
        {
            get
            {
                if (this.root != null)
                {
                    return new Enumerable(this.root).Select(b => b.MinKey);
                }

                return NoKeys;
            }
        }

        public IEnumerable<V> Values
        {
            get
            {
                if (this.root != null)
                {
                    return new Enumerable(this.root).Select(b => b.Value);
                }

                return NoValues;
            }
        }

        private class Enumerable : IEnumerable<Bucket>
        {
            private Bucket root;
            public Enumerable(Bucket root)
            {
                this.root = root;
            }

            public IEnumerator<Bucket> GetEnumerator()
            {
                return new Enumerator(root);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        private class Enumerator : IEnumerator<Bucket>
        {
            private Stack<BucketIndex> stack = new Stack<BucketIndex>();
            private Bucket current;

            public Enumerator(Bucket root)
            {
                this.stack = new Stack<BucketIndex>();
                this.stack.Push(new BucketIndex(root));
            }

            private class BucketIndex
            {
                internal Bucket Bucket;
                internal int Index;

                internal BucketIndex(Bucket bucket)
                {
                    this.Bucket = bucket;
                    this.Index = -1;
                }
            }

            public Bucket Current
            {
                get { return this.current; }
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                while (this.stack.Count > 0)
                {
                    var bi = this.stack.Peek();
                    if (bi.Bucket.Buckets == null)
                    {
                        this.stack.Pop();
                        this.current = bi.Bucket;
                        return true;
                    }
                    else
                    {
                        bi.Index += 1;
                        if (bi.Index < bi.Bucket.Buckets.Length)
                        {
                            var bucket = bi.Bucket.Buckets[bi.Index];
                            if (bucket.Buckets == null)
                            {
                                this.current = bucket;
                                return true;
                            }
                            else
                            {
                                this.stack.Push(new BucketIndex(bucket));
                            }
                        }
                        else
                        {
                            this.stack.Pop();
                        }
                    }
                }

                return false;
            }
        }

        private static readonly K[] NoKeys = new K[] { };
        private static readonly V[] NoValues = new V[] { };

        #region IDictionary<K,V> Members

        void IDictionary<K, V>.Add(K key, V value)
        {
            throw new NotSupportedException();
        }

        bool IDictionary<K, V>.Remove(K key)
        {
            throw new NotSupportedException();
        }

        V IDictionary<K, V>.this[K key]
        {
            get
            {
                return this.GetValueOrDefault(key);
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        ICollection<K> IDictionary<K, V>.Keys
        {
            get { return this.Keys.ToArray(); }
        }

        ICollection<V> IDictionary<K, V>.Values
        {
            get { return this.Values.ToArray(); }
        }

        #endregion

        #region ICollection<KeyValuePair<K,V>> Members

        void ICollection<KeyValuePair<K, V>>.Add(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        void ICollection<KeyValuePair<K, V>>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<KeyValuePair<K, V>>.Contains(KeyValuePair<K, V> item)
        {
            V value;
            if (this.TryGetValue(item.Key, out value))
            {
                return EqualityComparer<V>.Default.Equals(value, item.Value);
            }

            return false;
        }

        void ICollection<KeyValuePair<K, V>>.CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            int index = 0;
            foreach (var pair in this)
            {
                array[arrayIndex + index] = pair;
                index++;
            }
        }

        bool ICollection<KeyValuePair<K, V>>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<KeyValuePair<K, V>>.Remove(KeyValuePair<K, V> item)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}