// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public readonly partial struct KeyCollection : IReadOnlyCollection<TKey>, ICollection<TKey>, ICollection
        {
            private readonly ImmutableSegmentedDictionary<TKey, TValue> _dictionary;

            internal KeyCollection(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public int Count => _dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            bool ICollection.IsSynchronized => true;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public Enumerator GetEnumerator()
                => new(_dictionary.GetEnumerator());

            public bool Contains(TKey item)
                => _dictionary.ContainsKey(item);

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
                => _dictionary._dictionary.Keys.CopyTo(array, arrayIndex);

            void ICollection.CopyTo(Array array, int index)
                => ((ICollection)_dictionary._dictionary.Keys).CopyTo(array, index);

            void ICollection<TKey>.Add(TKey item)
                => throw new NotSupportedException();

            void ICollection<TKey>.Clear()
                => throw new NotSupportedException();

            bool ICollection<TKey>.Remove(TKey item)
                => throw new NotSupportedException();

            public bool All<TArg>(Func<TKey, TArg, bool> predicate, TArg arg)
            {
                foreach (var item in this)
                {
                    if (!predicate(item, arg))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
