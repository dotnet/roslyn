// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    // Note that this is not threadsafe for concurrent reading and writing.
    internal sealed class MultiDictionary<K, V>
    {
        public struct ValueSet
        {
            public struct Enumerator
            {
                private readonly V value;
                private ImmutableHashSet<V>.Enumerator values;
                private int count;

                public Enumerator(ValueSet v)
                {
                    if (v.value == null)
                    {
                        value = default(V);
                        values = default(ImmutableHashSet<V>.Enumerator);
                        count = 0;
                    }
                    else
                    {
                        var set = v.value as ImmutableHashSet<V>;
                        if (set == null)
                        {
                            value = (V)v.value;
                            values = default(ImmutableHashSet<V>.Enumerator);
                            count = 1;
                        }
                        else
                        {
                            value = default(V);
                            values = set.GetEnumerator();
                            count = set.Count;
                            Debug.Assert(count > 1);
                        }

                        Debug.Assert(count == v.Count);
                    }
                }

                // Note that this property is not guaranteed to throw either before MoveNext()
                // has been called or after the end of the set has been reached.
                public V Current
                {
                    get
                    {
                        return count > 1 ? values.Current : value;
                    }
                }

                public bool MoveNext()
                {
                    switch (count)
                    {
                        case 0:
                            return false;

                        case 1:
                            count = 0;
                            return true;

                        default:
                            if (values.MoveNext())
                            {
                                return true;
                            }

                            count = 0;
                            return false;
                    }
                }
            }

            // Stores either a single V or an ImmutableHashSet<V>
            private readonly object value;

            public int Count
            {
                get
                {
                    if (value == null)
                    {
                        return 0;
                    }
                    else
                    {
                        var set = value as ImmutableHashSet<V>;
                        return set == null ? 1 : set.Count;
                    }
                }
            }

            public ValueSet(object value)
            {
                this.value = value;
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            public ValueSet Add(V v)
            {
                Debug.Assert(value != null);

                var set = value as ImmutableHashSet<V>;
                if (set == null)
                {
                    if (ImmutableHashSet<V>.Empty.KeyComparer.Equals((V)value, v))
                    {
                        return this;
                    }

                    set = ImmutableHashSet.Create((V)value);
                }

                return new ValueSet(set.Add(v));
            }

            public V Single()
            {
                Debug.Assert(value is V); // Implies value != null
                return (V)value;
            }
        }

        private readonly Dictionary<K, ValueSet> dictionary;

        public int Count
        {
            get
            {
                return this.dictionary.Count;
            }
        }

        public IEnumerable<K> Keys
        {
            get { return this.dictionary.Keys; }
        }

        // Returns an empty set if there is no such key in the dictionary.
        public ValueSet this[K k]
        {
            get
            {
                ValueSet set;
                return this.dictionary.TryGetValue(k, out set) ? set : default(ValueSet);
            }
        }

        public MultiDictionary()
        {
            this.dictionary = new Dictionary<K, ValueSet>();
        }

        public MultiDictionary(IEqualityComparer<K> comparer)
        {
            this.dictionary = new Dictionary<K, ValueSet>(comparer);
        }

        public MultiDictionary(int capacity, IEqualityComparer<K> comparer)
        {
            this.dictionary = new Dictionary<K, ValueSet>(capacity, comparer);
        }

        public void Add(K k, V v)
        {
            ValueSet set;
            this.dictionary[k] = this.dictionary.TryGetValue(k, out set) ? set.Add(v) : new ValueSet(v);
        }

        public IEnumerator<KeyValuePair<K, ValueSet>> GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        public bool ContainsKey(K k)
        {
            return this.dictionary.ContainsKey(k);
        }

        internal void Clear()
        {
            this.dictionary.Clear();
        }
    }
}
