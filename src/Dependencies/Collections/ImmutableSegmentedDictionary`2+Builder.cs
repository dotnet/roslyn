// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public sealed partial class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        {
            /// <summary>
            /// The private builder implementation.
            /// </summary>
            private ValueBuilder _builder;

            internal Builder(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
                => _builder = new ValueBuilder(dictionary);

            public IEqualityComparer<TKey> KeyComparer
            {
                get => _builder.KeyComparer;
                set => _builder.KeyComparer = value;
            }

            public int Count => _builder.Count;

            public KeyCollection Keys => new(this);

            public ValueCollection Values => new(this);

            private SegmentedDictionary<TKey, TValue> ReadOnlyDictionary => _builder.ReadOnlyDictionary;

            IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

            IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

            ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

            ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ICollectionCalls<KeyValuePair<TKey, TValue>>.IsReadOnly(ref _builder);

            ICollection IDictionary.Keys => Keys;

            ICollection IDictionary.Values => Values;

            bool IDictionary.IsReadOnly => IDictionaryCalls.IsReadOnly(ref _builder);

            bool IDictionary.IsFixedSize => IDictionaryCalls.IsFixedSize(ref _builder);

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => ICollectionCalls.IsSynchronized(ref _builder);

            public TValue this[TKey key]
            {
                get => _builder[key];
                set => _builder[key] = value;
            }

            object? IDictionary.this[object key]
            {
                get => IDictionaryCalls.GetItem(ref _builder, key);
                set => IDictionaryCalls.SetItem(ref _builder, key, value);
            }

            public void Add(TKey key, TValue value)
                => _builder.Add(key, value);

            public void Add(KeyValuePair<TKey, TValue> item)
                => _builder.Add(item);

            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
                => _builder.AddRange(items);

            public void Clear()
                => _builder.Clear();

            public bool Contains(KeyValuePair<TKey, TValue> item)
                => _builder.Contains(item);

            public bool ContainsKey(TKey key)
                => _builder.ContainsKey(key);

            public bool ContainsValue(TValue value)
                => _builder.ContainsValue(value);

            public Enumerator GetEnumerator()
                => _builder.GetEnumerator();

            public TValue? GetValueOrDefault(TKey key)
                => _builder.GetValueOrDefault(key);

            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
                => _builder.GetValueOrDefault(key, defaultValue);

            public bool Remove(TKey key)
                => _builder.Remove(key);

            public bool Remove(KeyValuePair<TKey, TValue> item)
                => _builder.Remove(item);

            public void RemoveRange(IEnumerable<TKey> keys)
                => _builder.RemoveRange(keys);

            public bool TryGetKey(TKey equalKey, out TKey actualKey)
                => _builder.TryGetKey(equalKey, out actualKey);

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
                => _builder.TryGetValue(key, out value);

            public ImmutableSegmentedDictionary<TKey, TValue> ToImmutable()
                => _builder.ToImmutable();

            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
                => ICollectionCalls<KeyValuePair<TKey, TValue>>.CopyTo(ref _builder, array, arrayIndex);

            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
                => IEnumerableCalls<KeyValuePair<TKey, TValue>>.GetEnumerator(ref _builder);

            IEnumerator IEnumerable.GetEnumerator()
                => IEnumerableCalls.GetEnumerator(ref _builder);

            bool IDictionary.Contains(object key)
                => IDictionaryCalls.Contains(ref _builder, key);

            void IDictionary.Add(object key, object? value)
                => IDictionaryCalls.Add(ref _builder, key, value);

            IDictionaryEnumerator IDictionary.GetEnumerator()
                => IDictionaryCalls.GetEnumerator(ref _builder);

            void IDictionary.Remove(object key)
                => IDictionaryCalls.Remove(ref _builder, key);

            void ICollection.CopyTo(Array array, int index)
                => ICollectionCalls.CopyTo(ref _builder, array, index);

            internal TestAccessor GetTestAccessor()
                => new TestAccessor(this);

            internal readonly struct TestAccessor(Builder instance)
            {
                internal SegmentedDictionary<TKey, TValue> GetOrCreateMutableDictionary()
                    => instance._builder.GetOrCreateMutableDictionary();
            }
        }
    }
}
