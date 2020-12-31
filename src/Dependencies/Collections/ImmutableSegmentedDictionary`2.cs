// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// Represents a segmented dictionary that is immutable; meaning it cannot be changed once it is created.
    /// </summary>
    /// <remarks>
    /// <para>There are different scenarios best for <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> and others
    /// best for <see cref="ImmutableDictionary{TKey, TValue}"/>.</para>
    ///
    /// <para>In general, <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> is applicable in scenarios most like
    /// the scenarios where <see cref="ImmutableArray{T}"/> is applicable, and
    /// <see cref="ImmutableDictionary{TKey, TValue}"/> is applicable in scenarios most like the scenarios where
    /// <see cref="ImmutableList{T}"/> is applicable.</para>
    ///
    /// <para>The following table summarizes the performance characteristics of
    /// <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/>:</para>
    /// 
    /// <list type="table">
    ///   <item>
    ///     <description>Operation</description>
    ///     <description><see cref="ImmutableSegmentedDictionary{TKey, TValue}"/> Complexity</description>
    ///     <description><see cref="ImmutableDictionary{TKey, TValue}"/> Complexity</description>
    ///     <description>Comments</description>
    ///   </item>
    ///   <item>
    ///     <description>Item</description>
    ///     <description>O(1)</description>
    ///     <description>O(log n)</description>
    ///     <description>Directly index into the underlying segmented dictionary</description>
    ///   </item>
    ///   <item>
    ///     <description>Add()</description>
    ///     <description>O(n)</description>
    ///     <description>O(log n)</description>
    ///     <description>Requires creating a new segmented dictionary</description>
    ///   </item>
    /// </list>
    /// 
    /// <para>This type is backed by segmented arrays to avoid using the Large Object Heap without impacting algorithmic
    /// complexity.</para>
    /// </remarks>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    internal partial class ImmutableSegmentedDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
        where TKey : notnull
    {
        public static readonly ImmutableSegmentedDictionary<TKey, TValue> Empty = new(new SegmentedDictionary<TKey, TValue>());

        private readonly SegmentedDictionary<TKey, TValue> _dictionary;

        private ImmutableSegmentedDictionary(SegmentedDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public IEqualityComparer<TKey> KeyComparer => _dictionary.Comparer;

        public int Count => _dictionary.Count;

        public bool IsEmpty => _dictionary.Count == 0;

        public KeyCollection Keys => new(this);

        public ValueCollection Values => new(this);

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

        public ImmutableSegmentedDictionary<TKey, TValue> Add(TKey key, TValue value)
        {
            var dictionary = new SegmentedDictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary.Add(key, value);
            return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
        }

        public ImmutableSegmentedDictionary<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            var dictionary = new SegmentedDictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            foreach (var pair in pairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }

            return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
        }

        public ImmutableSegmentedDictionary<TKey, TValue> Clear()
        {
            if (IsEmpty)
            {
                return this;
            }

            return Empty.WithComparer(KeyComparer);
        }

        public bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            return TryGetValue(pair.Key, out var value)
                && EqualityComparer<TValue>.Default.Equals(value, pair.Value);
        }

        public bool ContainsKey(TKey key)
            => _dictionary.ContainsKey(key);

        public bool ContainsValue(TValue value)
            => _dictionary.ContainsValue(value);

        public Enumerator GetEnumerator()
            => new(_dictionary, Enumerator.ReturnType.KeyValuePair);

        public ImmutableSegmentedDictionary<TKey, TValue> Remove(TKey key)
        {
            if (!_dictionary.ContainsKey(key))
                return this;

            var dictionary = new SegmentedDictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary.Remove(key);
            return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
        }

        public ImmutableSegmentedDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys));

            var result = ToBuilder();
            result.RemoveRange(keys);
            return result.ToImmutable();
        }

        public ImmutableSegmentedDictionary<TKey, TValue> SetItem(TKey key, TValue value)
        {
            var dictionary = new SegmentedDictionary<TKey, TValue>(_dictionary, _dictionary.Comparer);
            dictionary[key] = value;
            return new ImmutableSegmentedDictionary<TKey, TValue>(dictionary);
        }

        public ImmutableSegmentedDictionary<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var result = ToBuilder();
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

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            => _dictionary.TryGetValue(key, out value);

        public ImmutableSegmentedDictionary<TKey, TValue> WithComparer(IEqualityComparer<TKey>? keyComparer)
        {
            keyComparer ??= EqualityComparer<TKey>.Default;

            if (KeyComparer == keyComparer)
            {
                // Don't need to reconstruct the dictionary because the key comparer is the same
                return this;
            }
            else if (IsEmpty)
            {
                if (keyComparer == Empty.KeyComparer)
                {
                    return Empty;
                }
                else
                {
                    return new ImmutableSegmentedDictionary<TKey, TValue>(new SegmentedDictionary<TKey, TValue>(keyComparer));
                }
            }
            else
            {
                return ImmutableSegmentedDictionary.CreateRange(keyComparer, this);
            }
        }

        public Builder ToBuilder()
            => new(this);

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
