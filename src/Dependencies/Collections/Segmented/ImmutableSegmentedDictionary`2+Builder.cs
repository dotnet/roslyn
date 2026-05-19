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
                => this._builder = new ValueBuilder(dictionary);

            public IEqualityComparer<TKey> KeyComparer
            {
                get => this._builder.KeyComparer;
                set => this._builder.KeyComparer = value;
            }

            public int Count => this._builder.Count;

            public KeyCollection Keys => new(this);

            public ValueCollection Values => new(this);

            private SegmentedDictionary<TKey, TValue> ReadOnlyDictionary => this._builder.ReadOnlyDictionary;

            IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => this.Keys;

            IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => this.Values;

            ICollection<TKey> IDictionary<TKey, TValue>.Keys => this.Keys;

            ICollection<TValue> IDictionary<TKey, TValue>.Values => this.Values;

            bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ICollectionCalls<KeyValuePair<TKey, TValue>>.IsReadOnly(ref this._builder);

            ICollection IDictionary.Keys => this.Keys;

            ICollection IDictionary.Values => this.Values;

            bool IDictionary.IsReadOnly => IDictionaryCalls.IsReadOnly(ref this._builder);

            bool IDictionary.IsFixedSize => IDictionaryCalls.IsFixedSize(ref this._builder);

            object ICollection.SyncRoot => this;

            bool ICollection.IsSynchronized => ICollectionCalls.IsSynchronized(ref this._builder);

            public TValue this[TKey key]
            {
                get => this._builder[key];
                set => this._builder[key] = value;
            }

            object? IDictionary.this[object key]
            {
                get => IDictionaryCalls.GetItem(ref this._builder, key);
                set => IDictionaryCalls.SetItem(ref this._builder, key, value);
            }

            public void Add(TKey key, TValue value)
                => this._builder.Add(key, value);

            public void Add(KeyValuePair<TKey, TValue> item)
                => this._builder.Add(item);

            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
                => this._builder.AddRange(items);

            public void Clear()
                => this._builder.Clear();

            public bool Contains(KeyValuePair<TKey, TValue> item)
                => this._builder.Contains(item);

            public bool ContainsKey(TKey key)
                => this._builder.ContainsKey(key);

            public bool ContainsValue(TValue value)
                => this._builder.ContainsValue(value);

            public Enumerator GetEnumerator()
                => this._builder.GetEnumerator();

            public TValue? GetValueOrDefault(TKey key)
                => this._builder.GetValueOrDefault(key);

            public TValue GetValueOrDefault(TKey key, TValue defaultValue)
                => this._builder.GetValueOrDefault(key, defaultValue);

            public bool Remove(TKey key)
                => this._builder.Remove(key);

            public bool Remove(KeyValuePair<TKey, TValue> item)
                => this._builder.Remove(item);

            public void RemoveRange(IEnumerable<TKey> keys)
                => this._builder.RemoveRange(keys);

            public bool TryGetKey(TKey equalKey, out TKey actualKey)
                => this._builder.TryGetKey(equalKey, out actualKey);

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
                => this._builder.TryGetValue(key, out value);

            public ImmutableSegmentedDictionary<TKey, TValue> ToImmutable()
                => this._builder.ToImmutable();

            void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
                => ICollectionCalls<KeyValuePair<TKey, TValue>>.CopyTo(ref this._builder, array, arrayIndex);

            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
                => IEnumerableCalls<KeyValuePair<TKey, TValue>>.GetEnumerator(ref this._builder);

            IEnumerator IEnumerable.GetEnumerator()
                => IEnumerableCalls.GetEnumerator(ref this._builder);

            bool IDictionary.Contains(object key)
                => IDictionaryCalls.Contains(ref this._builder, key);

            void IDictionary.Add(object key, object? value)
                => IDictionaryCalls.Add(ref this._builder, key, value);

            IDictionaryEnumerator IDictionary.GetEnumerator()
                => IDictionaryCalls.GetEnumerator(ref this._builder);

            void IDictionary.Remove(object key)
                => IDictionaryCalls.Remove(ref this._builder, key);

            void ICollection.CopyTo(Array array, int index)
                => ICollectionCalls.CopyTo(ref this._builder, array, index);

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
