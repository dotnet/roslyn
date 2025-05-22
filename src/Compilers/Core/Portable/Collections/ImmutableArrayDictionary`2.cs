// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableArrayDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        where TKey : notnull
    {
        public static readonly ImmutableArrayDictionary<TKey, TValue> Empty = new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(), valueComparer: null);

        private readonly Dictionary<TKey, TValue> _dictionary;
        private readonly IEqualityComparer<TValue> _valueComparer;

        private ImmutableArrayDictionary(Dictionary<TKey, TValue> dictionary, IEqualityComparer<TValue>? valueComparer)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public IEqualityComparer<TKey> KeyComparer => _dictionary.Comparer;

        public IEqualityComparer<TValue> ValueComparer => _valueComparer;

        public bool IsDefault => _dictionary is null;

        public bool IsDefaultOrEmpty => IsDefault || IsEmpty;

        public int Count => _dictionary.Count;

        public bool IsEmpty => _dictionary.Count == 0;

        public KeyCollection Keys => new KeyCollection(this);

        public ValueCollection Values => new ValueCollection(this);

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        ICollection IDictionary.Keys => Keys;

        ICollection IDictionary.Values => Values;

        bool IDictionary.IsReadOnly => true;

        bool IDictionary.IsFixedSize => true;

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => true;

        public TValue this[TKey key] => _dictionary[key];

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => this[key];
            set => throw new NotSupportedException();
        }

        object? IDictionary.this[object key]
        {
            get => ((IDictionary)_dictionary)[key];
            set => throw new NotSupportedException();
        }

        public ImmutableArrayDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var dictionary = new Dictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary.Add(key, value);
            return new ImmutableArrayDictionary<TKey, TValue>(dictionary, _valueComparer);
        }

        public ImmutableArrayDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            var dictionary = new Dictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            foreach (var (key, value) in pairs)
            {
                dictionary.Add(key, value);
            }

            return new ImmutableArrayDictionary<TKey, TValue>(dictionary, _valueComparer);
        }

        public ImmutableArrayDictionary<TKey, TValue> Clear()
        {
            if (IsEmpty)
            {
                return this;
            }

            return Empty.WithComparers(KeyComparer, ValueComparer);
        }

        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return TryGetValue(pair.Key, out TValue value)
                && ValueComparer.Equals(value, pair.Value);
        }

        public bool ContainsKey(TKey key)
            => _dictionary.ContainsKey(key);

        public bool ContainsValue(TValue value)
        {
            if (ValueComparer == EqualityComparer<TValue>.Default)
            {
                return _dictionary.ContainsValue(value);
            }
            else
            {
                foreach (var pair in this)
                {
                    if (ValueComparer.Equals(pair.Value, value))
                        return true;
                }

                return false;
            }
        }

        public Enumerator GetEnumerator()
            => new Enumerator(_dictionary, Enumerator.ReturnType.KeyValuePair);

        public ImmutableArrayDictionary<TKey, TValue> Remove(TKey key)
        {
            if (!_dictionary.ContainsKey(key))
                return this;

            var dictionary = new Dictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary.Remove(key);
            return new ImmutableArrayDictionary<TKey, TValue>(dictionary, _valueComparer);
        }

        public ImmutableArrayDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys));

            var result = ToBuilder();
            result.RemoveRange(keys);
            return result.ToImmutable();
        }

        public ImmutableArrayDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            var dictionary = new Dictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary[key] = value;
            return new ImmutableArrayDictionary<TKey, TValue>(dictionary, _valueComparer);
        }

        public ImmutableArrayDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            Builder result = ToBuilder();
            foreach (var item in items)
            {
                result[item.Key] = item.Value;
            }

            return result.ToImmutable();
        }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetKey(TKey equalKey, [NotNullWhen(true)] out TKey? actualKey)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
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
            => _dictionary.TryGetValue(key, out value);

        public ImmutableArrayDictionary<TKey, TValue> WithComparers(IEqualityComparer<TKey>? keyComparer)
            => WithComparers(keyComparer, valueComparer: null);

        public ImmutableArrayDictionary<TKey, TValue> WithComparers(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
        {
            keyComparer ??= EqualityComparer<TKey>.Default;
            valueComparer ??= EqualityComparer<TValue>.Default;

            if (IsEmpty)
            {
                if (keyComparer == Empty.KeyComparer)
                {
                    return new ImmutableArrayDictionary<TKey, TValue>(Empty._dictionary, valueComparer);
                }
                else
                {
                    return new ImmutableArrayDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(keyComparer), _valueComparer);
                }
            }
            else if (KeyComparer == keyComparer)
            {
                // Don't need to reconstruct the dictionary because the key comparer is the same
                return new ImmutableArrayDictionary<TKey, TValue>(_dictionary, valueComparer);
            }
            else
            {
                return ImmutableArrayDictionary.CreateRange(keyComparer, valueComparer, this);
            }
        }

        public Builder ToBuilder()
            => new Builder(this);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
            => Clear();

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
            => Add(key, value);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
            => AddRange(pairs);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
            => SetItem(key, value);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
            => SetItems(items);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
            => RemoveRange(keys);

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key) => Remove(key);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => new Enumerator(_dictionary, Enumerator.ReturnType.KeyValuePair);

        IDictionaryEnumerator IDictionary.GetEnumerator()
            => new Enumerator(_dictionary, Enumerator.ReturnType.DictionaryEntry);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(_dictionary, Enumerator.ReturnType.KeyValuePair);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);

        bool IDictionary.Contains(object key)
            => ((IDictionary)_dictionary).Contains(key);

        void ICollection.CopyTo(Array array, int index)
            => ((ICollection)_dictionary).CopyTo(array, index);

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
            => throw new NotSupportedException();

        bool IDictionary<TKey, TValue>.Remove(TKey key)
            => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => throw new NotSupportedException();

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
            => throw new NotSupportedException();

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
            => throw new NotSupportedException();

        void IDictionary.Add(object key, object? value)
            => throw new NotSupportedException();

        void IDictionary.Clear()
            => throw new NotSupportedException();

        void IDictionary.Remove(object key)
            => throw new NotSupportedException();
    }
}
