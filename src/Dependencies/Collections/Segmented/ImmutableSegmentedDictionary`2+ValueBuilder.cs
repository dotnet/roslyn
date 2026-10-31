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
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        private struct ValueBuilder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
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

            internal ValueBuilder(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
                _mutableDictionary = null;
            }

            public IEqualityComparer<TKey> KeyComparer
            {
                readonly get
                {
                    return ReadOnlyDictionary.Comparer;
                }

                set
                {
                    if (value is null)
                        throw new ArgumentNullException(nameof(value));

                    var self = this;

                    if (value != self.KeyComparer)
                    {
                        // Rewrite the mutable dictionary using a new comparer
                        var valuesToAdd = self.ReadOnlyDictionary;
                        self._mutableDictionary = new SegmentedDictionary<TKey, TValue>(value);
                        self._dictionary = default;
                        self.AddRange(valuesToAdd);
                    }

                    this = self;
                }
            }

            public readonly int Count => ReadOnlyDictionary.Count;

            internal readonly SegmentedDictionary<TKey, TValue> ReadOnlyDictionary
            {
                get
                {
                    var self = this;
                    return self._mutableDictionary ?? self._dictionary._dictionary;
                }
            }

            readonly IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => throw new NotSupportedException();

            readonly IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => throw new NotSupportedException();

            readonly ICollection<TKey> IDictionary<TKey, TValue>.Keys => throw new NotSupportedException();

            readonly ICollection<TValue> IDictionary<TKey, TValue>.Values => throw new NotSupportedException();

            readonly bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

            readonly ICollection IDictionary.Keys => throw new NotSupportedException();

            readonly ICollection IDictionary.Values => throw new NotSupportedException();

            readonly bool IDictionary.IsReadOnly => false;

            readonly bool IDictionary.IsFixedSize => false;

            readonly object ICollection.SyncRoot => throw new NotSupportedException();

            readonly bool ICollection.IsSynchronized => false;

            public TValue this[TKey key]
            {
                readonly get => ReadOnlyDictionary[key];
                set => GetOrCreateMutableDictionary()[key] = value;
            }

            object? IDictionary.this[object key]
            {
                readonly get => ((IDictionary)ReadOnlyDictionary)[key];
                set => ((IDictionary)GetOrCreateMutableDictionary())[key] = value;
            }

            internal SegmentedDictionary<TKey, TValue> GetOrCreateMutableDictionary()
            {
                var self = this;
                if (self._mutableDictionary is null)
                {
                    var originalDictionary = RoslynImmutableInterlocked.InterlockedExchange(ref self._dictionary, default);
                    if (originalDictionary.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {self.GetType()}");

                    self._mutableDictionary = new SegmentedDictionary<TKey, TValue>(originalDictionary._dictionary, originalDictionary.KeyComparer);
                }

                this = self;
                return self._mutableDictionary;
            }

            public void Add(TKey key, TValue value)
            {
                var self = this;
                if (self.Contains(new KeyValuePair<TKey, TValue>(key, value)))
                    return;

                self.GetOrCreateMutableDictionary().Add(key, value);
                this = self;
            }

            public void Add(KeyValuePair<TKey, TValue> item)
                => Add(item.Key, item.Value);

            public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
            {
                var self = this;
                if (items == null)
                    throw new ArgumentNullException(nameof(items));

                foreach (var pair in items)
                {
                    self.Add(pair.Key, pair.Value);
                }
                this = self;
            }

            public void Clear()
            {
                var self = this;
                if (self.ReadOnlyDictionary.Count != 0)
                {
                    if (self._mutableDictionary is null)
                    {
                        self._mutableDictionary = new SegmentedDictionary<TKey, TValue>(self.KeyComparer);
                        self._dictionary = default;
                    }
                    else
                    {
                        self._mutableDictionary.Clear();
                    }
                }

                this = self;
            }

            public readonly bool Contains(KeyValuePair<TKey, TValue> item)
            {
                return TryGetValue(item.Key, out var value)
                    && EqualityComparer<TValue>.Default.Equals(value, item.Value);
            }

            public readonly bool ContainsKey(TKey key)
                => ReadOnlyDictionary.ContainsKey(key);

            public readonly bool ContainsValue(TValue value)
                => ReadOnlyDictionary.ContainsValue(value);

            public Enumerator GetEnumerator()
                => new(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            public readonly TValue? GetValueOrDefault(TKey key)
            {
                if (TryGetValue(key, out var value))
                    return value;

                return default;
            }

            public readonly TValue GetValueOrDefault(TKey key, TValue defaultValue)
            {
                if (TryGetValue(key, out var value))
                    return value;

                return defaultValue;
            }

            public bool Remove(TKey key)
            {
                var self = this;
                if (self._mutableDictionary is null && !self.ContainsKey(key))
                    return false;

                bool removed = self.GetOrCreateMutableDictionary().Remove(key);
                this = self;
                return removed;
            }

            public bool Remove(KeyValuePair<TKey, TValue> item)
            {
                var self = this;
                if (!self.Contains(item))
                {
                    return false;
                }

                bool removed = self.GetOrCreateMutableDictionary().Remove(item.Key);
                this = self;
                return removed;
            }

            public void RemoveRange(IEnumerable<TKey> keys)
            {
                var self = this;
                if (keys is null)
                    throw new ArgumentNullException(nameof(keys));

                foreach (var key in keys)
                {
                    self.Remove(key);
                }

                this = self;
            }

#pragma warning disable IDE0251 // Make member 'readonly' (false positive: https://github.com/dotnet/roslyn/issues/72335)
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
#pragma warning restore IDE0251 // Make member 'readonly'
            {
                var self = this;
                foreach (var pair in self.ReadOnlyDictionary)
                {
                    if (self.KeyComparer.Equals(pair.Key, equalKey))
                    {
                        actualKey = pair.Key;
                        return true;
                    }
                }

                actualKey = equalKey;
                return false;
            }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
            public readonly bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
                => ReadOnlyDictionary.TryGetValue(key, out value);

            public ImmutableSegmentedDictionary<TKey, TValue> ToImmutable()
            {
                var self = this;
                self._dictionary = new ImmutableSegmentedDictionary<TKey, TValue>(self.ReadOnlyDictionary);
                self._mutableDictionary = null;
                this = self;
                return self._dictionary;
            }

            readonly void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
                => ((ICollection<KeyValuePair<TKey, TValue>>)ReadOnlyDictionary).CopyTo(array, arrayIndex);

            IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            IEnumerator IEnumerable.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.KeyValuePair);

            readonly bool IDictionary.Contains(object key)
                => ((IDictionary)ReadOnlyDictionary).Contains(key);

            void IDictionary.Add(object key, object? value)
                => ((IDictionary)GetOrCreateMutableDictionary()).Add(key, value);

            IDictionaryEnumerator IDictionary.GetEnumerator()
                => new Enumerator(GetOrCreateMutableDictionary(), Enumerator.ReturnType.DictionaryEntry);

            void IDictionary.Remove(object key)
                => ((IDictionary)GetOrCreateMutableDictionary()).Remove(key);

            readonly void ICollection.CopyTo(Array array, int index)
                => ((ICollection)ReadOnlyDictionary).CopyTo(array, index);
        }
    }
}
