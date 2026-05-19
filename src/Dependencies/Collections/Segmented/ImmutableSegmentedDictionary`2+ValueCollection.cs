// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public readonly partial struct ValueCollection : IReadOnlyCollection<TValue>, ICollection<TValue>, ICollection
        {
            private readonly ImmutableSegmentedDictionary<TKey, TValue> _dictionary;

            internal ValueCollection(ImmutableSegmentedDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }

            public int Count => _dictionary.Count;

            bool ICollection<TValue>.IsReadOnly => true;

            bool ICollection.IsSynchronized => true;

            object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

            public Enumerator GetEnumerator()
                => new(_dictionary.GetEnumerator());

            public bool Contains(TValue item)
                => _dictionary.ContainsValue(item);

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
                => _dictionary._dictionary.Values.CopyTo(array, arrayIndex);

            void ICollection.CopyTo(Array array, int index)
                => ((ICollection)_dictionary._dictionary.Values).CopyTo(array, index);

            void ICollection<TValue>.Add(TValue item)
                => throw new NotSupportedException();

            void ICollection<TValue>.Clear()
                => throw new NotSupportedException();

            bool ICollection<TValue>.Remove(TValue item)
                => throw new NotSupportedException();

            public bool All<TArg>(Func<TValue, TArg, bool> predicate, TArg arg)
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
