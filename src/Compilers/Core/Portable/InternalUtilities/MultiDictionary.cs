// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    // Note that this is not threadsafe for concurrent reading and writing.
    internal sealed class MultiDictionary<K, V> : IEnumerable<KeyValuePair<K, MultiDictionary<K, V>.ValueSet>>
    {
        public struct ValueSet : IEnumerable<V>
        {
            public struct Enumerator : IEnumerator<V>
            {
                private readonly V _value;
                private ImmutableHashSet<V>.Enumerator _values;
                private int _count;

                public Enumerator(ValueSet v)
                {
                    if (v._value == null)
                    {
                        _value = default(V);
                        _values = default(ImmutableHashSet<V>.Enumerator);
                        _count = 0;
                    }
                    else
                    {
                        var set = v._value as ImmutableHashSet<V>;
                        if (set == null)
                        {
                            _value = (V)v._value;
                            _values = default(ImmutableHashSet<V>.Enumerator);
                            _count = 1;
                        }
                        else
                        {
                            _value = default(V);
                            _values = set.GetEnumerator();
                            _count = set.Count;
                            Debug.Assert(_count > 1);
                        }

                        Debug.Assert(_count == v.Count);
                    }
                }

                public void Dispose()
                {
                }

                public void Reset()
                {
                    throw new NotSupportedException();
                }

                object IEnumerator.Current => this.Current;

                // Note that this property is not guaranteed to throw either before MoveNext()
                // has been called or after the end of the set has been reached.
                public V Current
                {
                    get
                    {
                        return _count > 1 ? _values.Current : _value;
                    }
                }

                public bool MoveNext()
                {
                    switch (_count)
                    {
                        case 0:
                            return false;

                        case 1:
                            _count = 0;
                            return true;

                        default:
                            if (_values.MoveNext())
                            {
                                return true;
                            }

                            _count = 0;
                            return false;
                    }
                }
            }

            // Stores either a single V or an ImmutableHashSet<V>
            private readonly object _value;

            public int Count
            {
                get
                {
                    if (_value == null)
                    {
                        return 0;
                    }
                    else
                    {
                        return (_value as ImmutableHashSet<V>)?.Count ?? 1;
                    }
                }
            }

            public ValueSet(object value)
            {
                _value = value;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(this);
            }

            public ValueSet Add(V v)
            {
                Debug.Assert(_value != null);

                var set = _value as ImmutableHashSet<V>;
                if (set == null)
                {
                    if (ImmutableHashSet<V>.Empty.KeyComparer.Equals((V)_value, v))
                    {
                        return this;
                    }

                    set = ImmutableHashSet.Create((V)_value);
                }

                return new ValueSet(set.Add(v));
            }

            public V Single()
            {
                Debug.Assert(_value is V); // Implies value != null
                return (V)_value;
            }
        }

        private readonly Dictionary<K, ValueSet> _dictionary;

        public int Count
        {
            get
            {
                return _dictionary.Count;
            }
        }

        public IEnumerable<K> Keys
        {
            get { return _dictionary.Keys; }
        }

        // Returns an empty set if there is no such key in the dictionary.
        public ValueSet this[K k]
        {
            get
            {
                ValueSet set;
                return _dictionary.TryGetValue(k, out set) ? set : default(ValueSet);
            }
        }

        public MultiDictionary()
        {
            _dictionary = new Dictionary<K, ValueSet>();
        }

        public MultiDictionary(IEqualityComparer<K> comparer)
        {
            _dictionary = new Dictionary<K, ValueSet>(comparer);
        }

        public MultiDictionary(int capacity, IEqualityComparer<K> comparer)
        {
            _dictionary = new Dictionary<K, ValueSet>(capacity, comparer);
        }

        public void Add(K k, V v)
        {
            ValueSet set;
            _dictionary[k] = _dictionary.TryGetValue(k, out set) ? set.Add(v) : new ValueSet(v);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<K, ValueSet>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        public bool ContainsKey(K k)
        {
            return _dictionary.ContainsKey(k);
        }

        internal void Clear()
        {
            _dictionary.Clear();
        }
    }
}
