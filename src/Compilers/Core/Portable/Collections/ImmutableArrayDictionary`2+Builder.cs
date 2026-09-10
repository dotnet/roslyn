// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections
{
    partial struct ImmutableArrayDictionary<TKey, TValue>
    {
        public sealed partial class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            private readonly ImmutableArrayDictionary<TKey, TValue> _dictionary;
            private Dictionary<TKey, TValue>? _mutableDictionary;

            internal Builder(ImmutableArrayDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public IEqualityComparer<TKey> KeyComparer => _dictionary.KeyComparer;

            public IEqualityComparer<TValue> ValueComparer => _dictionary.ValueComparer;

            public int Count => ReadOnlyDictionary.Count;

            public KeyCollection Keys => new KeyCollection(this);

            public ValueCollection Values => new ValueCollection(this);

            private Dictionary<TKey, TValue> ReadOnlyDictionary => _mutableDictionary ?? _dictionary._dictionary;

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

            private Dictionary<TKey, TValue> GetOrCreateMutableDictionary()
            {
                return _mutableDictionary ??= new Dictionary<TKey, TValue>(_dictionary._dictionary, _dictionary.KeyComparer);
            }

            public void Add(TKey key, TValue value)
                => GetOrCreateMutableDictionary().Add(key, value);

            public void Add(KeyValuePair<TKey, TValue> item)
                => Add(item.Key, item.Value);

            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                if (items == null)
                    throw new ArgumentNullException(nameof(items));

                foreach (KeyValuePair<TKey, TValue> pair in items)
                    Add(pair.Key, pair.Value);
            }

            public void Clear()
            {
                if (ReadOnlyDictionary.Count != 0)
                {
                    if (_mutableDictionary is null)
                        _mutableDictionary = new Dictionary<TKey, TValue>(KeyComparer);
                    else
                        _mutableDictionary.Clear();
                }
            }

            public bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return TryGetValue(item.Key, out TValue value)
                    && ValueComparer.Equals(value, item.Value);
            }

            public bool ContainsKey(TKey key)
                => ReadOnlyDictionary.ContainsKey(key);

            public bool ContainsValue(TValue value)
            {
                if (ValueComparer == EqualityComparer<TValue>.Default)
                {
                    return _dictionary.ContainsValue(value);
                }
                else
                {
                    foreach (var pair in ReadOnlyDictionary)
                    {
                        if (ValueComparer.Equals(pair.Value, value))
                            return true;
                    }

                    return false;
                }
            }

            public Enumerator GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            [return: MaybeNull]
            public TValue GetValueOrDefault(TKey key)
            {
                if (TryGetValue(key, out TValue value))
                    return value;

                return default;
            }

            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
            {
                if (TryGetValue(key, out TValue value))
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

            public bool TryGetKey(TKey equalKey, [NotNullWhen(true)] out TKey? actualKey)
            {
                foreach (var key in Keys)
                {
                    if (KeyComparer.Equals(key, equalKey))
                    {
                        actualKey = key;
                        return true;
                    }
                }

                actualKey = default;
                return false;
            }

            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
                => ReadOnlyDictionary.TryGetValue(key, out value);

            public ImmutableArrayDictionary<TKey, TValue> ToImmutable()
            {
                var dictionary = ReadOnlyDictionary;
                _mutableDictionary = null;
                return new ImmutableArrayDictionary<TKey, TValue>(dictionary, ValueComparer);
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
