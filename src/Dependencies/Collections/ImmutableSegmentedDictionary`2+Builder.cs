// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public sealed partial class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            /// <summary>
            /// The immutable collection this builder is based on.
            /// </summary>
            private ImmutableSegmentedDictionary<TKey, TValue> _dictionary;

            /// <summary>
            /// The current mutable collection this builder is operating on. This field is initialized to a copy of
            /// <see cref="_dictionary"/> the first time a change is made.
            /// </summary>
            private SegmentedDictionary<TKey, TValue>? _mutableDictionary;

            internal Builder(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public IEqualityComparer<TKey> KeyComparer
            {
                get
                {
                    return ReadOnlyDictionary.Comparer;
                }

                set
                {
                    if (value is null)
                        throw new ArgumentNullException(nameof(value));

                    if (value != KeyComparer)
                    {
                        // Rewrite the mutable dictionary using a new comparer
                        var valuesToAdd = ReadOnlyDictionary;
                        _mutableDictionary = new SegmentedDictionary<TKey, TValue>(value);
                        AddRange(valuesToAdd);
                    }
                }
            }

            public int Count => ReadOnlyDictionary.Count;

            public KeyCollection Keys => new(this);

            public ValueCollection Values => new(this);

            private SegmentedDictionary<TKey, TValue> ReadOnlyDictionary => _mutableDictionary ?? _dictionary._dictionary;

            IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

            IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

            ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

            ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

            ICollection IDictionary.Keys => Keys;

            ICollection IDictionary.Values => Values;

            bool IDictionary.IsReadOnly => false;

            bool IDictionary.IsFixedSize => false;

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => false;

            public TValue this[TKey key]
            {
                get => ReadOnlyDictionary[key];
                set => GetOrCreateMutableDictionary()[key] = value;
            }

            object? IDictionary.this[object key]
            {
                get => ((IDictionary)ReadOnlyDictionary)[key];
                set => ((IDictionary)GetOrCreateMutableDictionary())[key] = value;
            }

            private SegmentedDictionary<TKey, TValue> GetOrCreateMutableDictionary()
            {
                return _mutableDictionary ??= new SegmentedDictionary<TKey, TValue>(_dictionary._dictionary, _dictionary.KeyComparer);
            }

            public void Add(TKey key, TValue value)
            {
                if (Contains(new KeyValuePair<TKey, TValue>(key, value)))
                    return;

                GetOrCreateMutableDictionary().Add(key, value);
            }

            public void Add(KeyValuePair<TKey, TValue> item)
                => Add(item.Key, item.Value);

            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                if (items == null)
                    throw new ArgumentNullException(nameof(items));

                foreach (var pair in items)
                    Add(pair.Key, pair.Value);
            }

            public void Clear()
            {
                if (ReadOnlyDictionary.Count != 0)
                {
                    if (_mutableDictionary is null)
                        _mutableDictionary = new SegmentedDictionary<TKey, TValue>(KeyComparer);
                    else
                        _mutableDictionary.Clear();
                }
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return TryGetValue(item.Key, out var value)
                    && EqualityComparer<TValue>.Default.Equals(value, item.Value);
            }

            public bool ContainsKey(TKey key)
                => ReadOnlyDictionary.ContainsKey(key);

            public bool ContainsValue(TValue value)
            {
                return _dictionary.ContainsValue(value);
            }

            public Enumerator GetEnumerator()
                => new(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            public TValue? GetValueOrDefault(TKey key)
            {
                if (TryGetValue(key, out var value))
                    return value;

                return default;
            }

            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
            {
                if (TryGetValue(key, out var value))
                    return value;

                return defaultValue;
            }

            public bool Remove(TKey key)
            {
                if (_mutableDictionary is null && !ContainsKey(key))
                    return false;

                return GetOrCreateMutableDictionary().Remove(key);
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                if (!Contains(item))
                {
                    return false;
                }

                GetOrCreateMutableDictionary().Remove(item.Key);
                return true;
            }

            public void RemoveRange(IEnumerable<TKey> keys)
            {
                if (keys is null)
                    throw new ArgumentNullException(nameof(keys));

                foreach (var key in keys)
                {
                    Remove(key);
                }
            }

            public bool TryGetKey(TKey equalKey, out TKey actualKey)
            {
                foreach (var key in Keys)
                {
                    if (KeyComparer.Equals(key, equalKey))
                    {
                        actualKey = key;
                        return true;
                    }
                }

                actualKey = equalKey;
                return false;
            }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
                => ReadOnlyDictionary.TryGetValue(key, out value);

            public ImmutableSegmentedDictionary<TKey, TValue> ToImmutable()
            {
                _dictionary = new ImmutableSegmentedDictionary<TKey, TValue>(ReadOnlyDictionary);
                _mutableDictionary = null;
                return _dictionary;
            }

            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
                => ((ICollection<KeyValuePair<TKey, TValue>>)ReadOnlyDictionary).CopyTo(array, arrayIndex);

            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            IEnumerator IEnumerable.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            bool IDictionary.Contains(object key)
                => ((IDictionary)ReadOnlyDictionary).Contains(key);

            void IDictionary.Add(object key, object? value)
                => ((IDictionary)GetOrCreateMutableDictionary()).Add(key, value);

            IDictionaryEnumerator IDictionary.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.DictionaryEntry);

            void IDictionary.Remove(object key)
                => ((IDictionary)GetOrCreateMutableDictionary()).Remove(key);

            void ICollection.CopyTo(Array array, int index)
                => ((ICollection)ReadOnlyDictionary).CopyTo(array, index);
        }
    }
}
