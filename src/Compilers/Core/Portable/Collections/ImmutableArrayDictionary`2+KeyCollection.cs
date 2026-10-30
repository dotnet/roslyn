// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    partial struct ImmutableArrayDictionary<TKey, TValue>
    {
        public readonly partial struct KeyCollection : IReadOnlyCollection<TKey>, ICollection<TKey>, ICollection
        {
            private readonly ImmutableArrayDictionary<TKey, TValue> _dictionary;

            internal KeyCollection(ImmutableArrayDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public int Count => _dictionary.Count;

            bool ICollection<TKey>.IsReadOnly => true;

            bool ICollection.IsSynchronized => true;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public Enumerator GetEnumerator()
                => new Enumerator(_dictionary.GetEnumerator());

            public bool Contains(TKey item)
                => _dictionary.ContainsKey(item);

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            void ICollection<TKey>.CopyTo(TKey[] array, int arrayIndex)
                => ((ICollection<TKey>)_dictionary.Keys).CopyTo(array, arrayIndex);

            void ICollection.CopyTo(Array array, int index)
                => ((ICollection)_dictionary.Keys).CopyTo(array, index);

            void ICollection<TKey>.Add(TKey item)
                => throw new NotSupportedException();

            void ICollection<TKey>.Clear()
                => throw new NotSupportedException();

            bool ICollection<TKey>.Remove(TKey item)
                => throw new NotSupportedException();
        }
    }
}
