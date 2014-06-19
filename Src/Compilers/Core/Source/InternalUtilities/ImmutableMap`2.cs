using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Collections.Immutable
{
#if !COMPILERCORE
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
    internal partial class ImmutableDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>, IReadOnlyDictionary<K, V>
    {
        private static readonly ImmutableDictionary<K, V> Empty = new ImmutableDictionary<K, V>();

        private readonly AbstractBucket rootOpt;
        internal readonly IEqualityComparer<K> KeyComparer;
        private readonly IEqualityComparer<V> valueEqualityComparer;

        // Temporary hack to allow access to the creation methods until we can use the BCL version
        internal static ImmutableDictionary<K, V> PrivateEmpty_DONOTUSE()
        {
            return Empty;
        }

        internal static ImmutableDictionary<K, V> PrivateConstructor_DONOTUSE(IEqualityComparer<K> keyComparer)
        {
            return new ImmutableDictionary<K, V>(keyComparer);
        }

        private ImmutableDictionary(IEqualityComparer<K> keyEqualityComparerOpt = null, IEqualityComparer<V> valueEqualityComparerOpt = null)
            : this(rootOpt: null, keyEqualityComparerOpt: keyEqualityComparerOpt, valueEqualityComparerOpt: valueEqualityComparerOpt)
        {
        }

        private ImmutableDictionary(
            AbstractBucket rootOpt,
            IEqualityComparer<K> keyEqualityComparerOpt,
            IEqualityComparer<V> valueEqualityComparerOpt)
        {
            this.rootOpt = rootOpt;
            this.KeyComparer = keyEqualityComparerOpt ?? EqualityComparer<K>.Default;
            this.valueEqualityComparer = valueEqualityComparerOpt ?? EqualityComparer<V>.Default;
        }

        public int Count
        {
            get
            {
                return this.rootOpt != null ? this.rootOpt.Count : 0;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.Count == 0;
            }
        }

        public bool ContainsKey(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (this.rootOpt != null)
            {
                var vb = this.rootOpt.Get(this.KeyComparer.GetHashCode(key), key, this.KeyComparer);
                return vb != null;
            }

            return false;
        }

        public ImmutableDictionary<K, V> Add(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            // Temporary: The BCL ImmutableDictionary<K, V> changes the behavior of "Add" so
            // that it throws if the key already exists.
            // To get the "AddOrUpdate" behavior, the BCL has a "SetItem" method.
            // For now, changing the behavior of "Add" so that we can find all the places that
            // really should be "SetItem"
            if (ContainsKey(key))
            {
                throw new ArgumentException("key already exists. Use SetItem.");
            }

            return new ImmutableDictionary<K, V>(this.AddBucket(this.rootOpt, key, value, mutate: false), this.KeyComparer, this.valueEqualityComparer);
        }

        public ImmutableDictionary<K, V> SetItem(K key, V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            return new ImmutableDictionary<K, V>(this.AddBucket(this.rootOpt, key, value, mutate: false), this.KeyComparer, this.valueEqualityComparer);
        }

        private AbstractBucket AddBucket(AbstractBucket root, K key, V value, bool mutate)
        {
            var vb = new ValueBucket(key, value, this.KeyComparer);
            if (root == null)
            {
                return vb;
            }
            else
            {
                return root.Add(0, vb, this.KeyComparer, mutate);
            }
        }

        public ImmutableDictionary<K, V> AddRange(IEnumerable<KeyValuePair<K, V>> pairs)
        {
            bool mutate = false;

            if (this.rootOpt == null)
            {
                mutate = true;
            }

            var newRoot = this.rootOpt;
            foreach (var pair in pairs)
            {
                newRoot = this.AddBucket(newRoot, pair.Key, pair.Value, mutate);
            }

            return new ImmutableDictionary<K, V>(newRoot, this.KeyComparer, this.valueEqualityComparer);
        }

        public ImmutableDictionary<K, V> Remove(K key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (this.rootOpt != null)
            {
                var newRoot = this.rootOpt.Remove(this.KeyComparer.GetHashCode(key), key, this.KeyComparer);
                if (newRoot == null && this.KeyComparer == null && this.valueEqualityComparer == null)
                {
                    return Empty;
                }
                else if (object.ReferenceEquals(rootOpt, newRoot))
                {
                    return this;
                }
                else
                {
                    return new ImmutableDictionary<K, V>(newRoot, this.KeyComparer, this.valueEqualityComparer);
                }
            }

            return this;
        }

        public ImmutableDictionary<K, V> Remove(IEnumerable<K> keys)
        {
            ImmutableDictionary<K, V> map = this;
            foreach (var key in keys)
            {
                map = map.Remove(key);
            }

            return map;
        }

#if false
        public ImmutableDictionary<K, V> Remove(ImmutableDictionary<K, V> map)
        {
            ImmutableDictionary<K, V> result = this;
            foreach (var tuple in this.GetValueBuckets())
            {
                K key = tuple.Key;
                V value;
                if (map.TryGetValue(key, out value))
                {
                    if (this.valueEqualityComparer.Equals(tuple.Value, value))
                    {
                        result = result.Remove(key);
                    }
                }
            }

            return result;
        }
#endif

        public bool TryGetValue(K key, out V value)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }

            if (this.rootOpt != null)
            {
                var vb = this.rootOpt.Get(this.KeyComparer.GetHashCode(key), key, this.KeyComparer);
                if (vb != null)
                {
                    value = vb.Value;
                    return true;
                }
            }

            value = default(V);
            return false;
        }

        public V this[K key]
        {
            get
            {
                V result;
                if (this.TryGetValue(key, out result))
                {
                    return result;
                }

                throw new KeyNotFoundException();
            }
        }

        public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
        {
            return this.GetValueBuckets().Select(vb => new KeyValuePair<K, V>(vb.Key, vb.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IEnumerable<ValueBucket> GetValueBuckets()
        {
            if (this.rootOpt == null)
            {
                yield break;
            }

            var stack = new Stack<IEnumerator<AbstractBucket>>();
            stack.Push(this.rootOpt.GetAll().GetEnumerator());
            while (stack.Count > 0)
            {
                var en = stack.Peek();
                if (en.MoveNext())
                {
                    var vb = en.Current as ValueBucket;
                    if (vb != null)
                    {
                        yield return vb;
                    }
                    else
                    {
                        stack.Push(en.Current.GetAll().GetEnumerator());
                    }
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        public IEnumerable<K> Keys
        {
            get
            {
                return this.GetValueBuckets().Select(vb => vb.Key);
            }
        }

        public IEnumerable<V> Values
        {
            get
            {
                return this.GetValueBuckets().Select(vb => vb.Value);
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder("[");
            builder.Append(string.Join(",", this.Select(t => t.Key + ":" + t.Value).ToArray()));
            builder.Append("]");
            return builder.ToString();
        }

        public bool Equals(ImmutableDictionary<K, V> other)
        {
            if (this == (object)other)
            {
                return true;
            }

            if (other == null)
            {
                return false;
            }

            if (this.Count != other.Count)
            {
                return false;
            }

            foreach (var bucket in this.GetValueBuckets())
            {
                var key = bucket.Key;
                var value1 = bucket.Value;
                V value2;
                if (!other.TryGetValue(key, out value2))
                {
                    return false;
                }

                if (!this.valueEqualityComparer.Equals(value1, value2))
                {
                    return false;
                }
            }

            return true;
        }
    }
}