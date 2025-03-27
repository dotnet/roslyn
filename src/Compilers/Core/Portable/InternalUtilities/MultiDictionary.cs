// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    // Note that this is not threadsafe for concurrent reading and writing.
    internal sealed class MultiDictionary<K, V> : IEnumerable<KeyValuePair<K, MultiDictionary<K, V>.ValueSet>>
        where K : notnull
    {
        public readonly struct ValueSet : IEnumerable<V>
        {
            public struct Enumerator : IEnumerator<V>
            {
                [AllowNull]
                private readonly V _value;
                private ImmutableHashSet<V>.Enumerator _values;
                private int _count;

                public Enumerator(ValueSet v)
                {
                    if (v._value == null)
                    {
                        _value = default;
                        _values = default;
                        _count = 0;
                    }
                    else
                    {
                        var set = v._value as ImmutableHashSet<V>;
                        if (set == null)
                        {
                            _value = (V)v._value;
                            _values = default;
                            _count = 1;
                        }
                        else
                        {
                            _value = default;
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

                object? IEnumerator.Current => this.Current;

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
            private readonly object? _value;

            private readonly IEqualityComparer<V> _equalityComparer;

            public int Count
            {
                get
                {
                    if (_value == null)
                    {
                        return 0;
                    }

                    // The following code used to be written like so:
                    //    
                    //    return (_value as ImmutableHashSet<V>)?.Count ?? 1;
                    // 
                    // This code pattern triggered a code-gen bug on Mac:
                    // https://github.com/dotnet/coreclr/issues/4801

                    var set = _value as ImmutableHashSet<V>;
                    if (set == null)
                    {
                        return 1;
                    }

                    return set.Count;
                }
            }

            public ValueSet(object? value, IEqualityComparer<V>? equalityComparer = null)
            {
                _value = value;
                _equalityComparer = equalityComparer ?? ImmutableHashSet<V>.Empty.KeyComparer;
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
                    if (_equalityComparer.Equals((V)_value!, v))
                    {
                        return this;
                    }

                    set = ImmutableHashSet.Create(_equalityComparer, (V)_value!);
                }

                return new ValueSet(set.Add(v), _equalityComparer);
            }

            public bool Contains(V v)
            {
                var set = _value as ImmutableHashSet<V>;
                if (set == null)
                {
                    return _equalityComparer.Equals((V)_value!, v);
                }

                return set.Contains(v);
            }

            public bool Contains(V v, IEqualityComparer<V> comparer)
            {
                foreach (V other in this)
                {
                    if (comparer.Equals(other, v))
                    {
                        return true;
                    }
                }

                return false;
            }

            public V Single()
            {
                RoslynDebug.Assert(_value is V); // Implies value != null
                return (V)_value;
            }

            public bool Equals(ValueSet other)
            {
                return _value == other._value;
            }
        }

        private readonly Dictionary<K, ValueSet> _dictionary;

        private readonly IEqualityComparer<V>? _valueComparer;

        public int Count => _dictionary.Count;

        public bool IsEmpty => _dictionary.Count == 0;

        public Dictionary<K, ValueSet>.KeyCollection Keys => _dictionary.Keys;

        public Dictionary<K, ValueSet>.ValueCollection Values => _dictionary.Values;

        private readonly ValueSet _emptySet = new(null, null);

        // Returns an empty set if there is no such key in the dictionary.
        public ValueSet this[K k]
        {
            get
            {
                return _dictionary.TryGetValue(k, out var set) ? set : _emptySet;
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

        public void EnsureCapacity(int capacity)
        {
#if NET
            _dictionary.EnsureCapacity(capacity);
#endif
        }

        public MultiDictionary(int capacity, IEqualityComparer<K> comparer, IEqualityComparer<V>? valueComparer = null)
        {
            _dictionary = new Dictionary<K, ValueSet>(capacity, comparer);
            _valueComparer = valueComparer;
        }

        public bool Add(K k, V v)
        {
            ValueSet updated;

            if (_dictionary.TryGetValue(k, out ValueSet set))
            {
                updated = set.Add(v);
                if (updated.Equals(set))
                {
                    return false;
                }
            }
            else
            {
                updated = new ValueSet(v, _valueComparer);
            }

            _dictionary[k] = updated;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Dictionary<K, ValueSet>.Enumerator GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }

        IEnumerator<KeyValuePair<K, ValueSet>> IEnumerable<KeyValuePair<K, ValueSet>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(K k)
        {
            return _dictionary.ContainsKey(k);
        }

        internal void Clear()
        {
            _dictionary.Clear();
        }

        public void Remove(K key)
        {
            _dictionary.Remove(key);
        }
    }
}
