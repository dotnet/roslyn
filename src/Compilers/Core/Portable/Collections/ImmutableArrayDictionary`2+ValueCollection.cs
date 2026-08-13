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
        public readonly partial struct ValueCollection : IReadOnlyCollection<TValue>, ICollection<TValue>, ICollection
        {
            private readonly ImmutableArrayDictionary<TKey, TValue> _dictionary;

            internal ValueCollection(ImmutableArrayDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public int Count => _dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection.IsSynchronized => true;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public Enumerator GetEnumerator()
                => new Enumerator(_dictionary.GetEnumerator());

            public bool Contains(TValue item)
                => _dictionary.ContainsValue(item);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
                => ((ICollection<TValue>)_dictionary.Values).CopyTo(array, arrayIndex);

            void ICollection.CopyTo(Array array, int index)
                => ((ICollection)_dictionary.Values).CopyTo(array, index);

            void ICollection<TValue>.Add(TValue item)
                => throw new NotSupportedException();

            void ICollection<TValue>.Clear()
                => throw new NotSupportedException();

            bool ICollection<TValue>.Remove(TValue item)
                => throw new NotSupportedException();
        }
    }
}
