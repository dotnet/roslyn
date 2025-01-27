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

                    if (value != KeyComparer)
                    {
                        // Rewrite the mutable dictionary using a new comparer
                        var valuesToAdd = ReadOnlyDictionary;
                        _mutableDictionary = new SegmentedDictionary<TKey, TValue>(value);
                        _dictionary = default;
                        AddRange(valuesToAdd);
                    }
                }
            }

            public readonly int Count => ReadOnlyDictionary.Count;

            internal readonly SegmentedDictionary<TKey, TValue> ReadOnlyDictionary => _mutableDictionary ?? _dictionary._dictionary;

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
                if (_mutableDictionary is null)
                {
                    var originalDictionary = RoslynImmutableInterlocked.InterlockedExchange(ref _dictionary, default);
                    if (originalDictionary.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {GetType()}");

                    _mutableDictionary = new SegmentedDictionary<TKey, TValue>(originalDictionary._dictionary, originalDictionary.KeyComparer);
                }

                return _mutableDictionary;
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
                    {
                        _mutableDictionary = new SegmentedDictionary<TKey, TValue>(KeyComparer);
                        _dictionary = default;
                    }
                    else
                    {
                        _mutableDictionary.Clear();
                    }
                }
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

#pragma warning disable IDE0251 // Make member 'readonly' (false positive: https://github.com/dotnet/roslyn/issues/72335)
            public bool TryGetKey(TKey equalKey, out TKey actualKey)
#pragma warning restore IDE0251 // Make member 'readonly'
            {
                foreach (var pair in this)
                {
                    if (KeyComparer.Equals(pair.Key, equalKey))
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
                _dictionary = new ImmutableSegmentedDictionary<TKey, TValue>(ReadOnlyDictionary);
                _mutableDictionary = null;
                return _dictionary;
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
