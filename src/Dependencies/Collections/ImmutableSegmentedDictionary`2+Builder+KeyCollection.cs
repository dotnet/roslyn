// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public partial class Builder
        {
            public readonly struct KeyCollection : ICollection<TKey>, IReadOnlyCollection<TKey>, ICollection
            {
                private readonly ImmutableSegmentedDictionary<TKey, TValue>.Builder _dictionary;

                internal KeyCollection(ImmutableSegmentedDictionary<TKey, TValue>.Builder dictionary)
                {
                    Debug.Assert(dictionary is not null);
                    _dictionary = dictionary!;
                }

                public int Count => _dictionary.Count;

                bool ICollection<TKey>.IsReadOnly => false;

                bool ICollection.IsSynchronized => false;

                object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

                void ICollection<TKey>.Add(TKey item)
                    => throw new NotSupportedException();

                public void Clear()
                    => _dictionary.Clear();

                public bool Contains(TKey item)
                    => _dictionary.ContainsKey(item);

                public void CopyTo(TKey[] array, int arrayIndex)
                    => _dictionary.ReadOnlyDictionary.Keys.CopyTo(array, arrayIndex);

                public ImmutableSegmentedDictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator()
                    => new(_dictionary.GetEnumerator());

                public bool Remove(TKey item)
                    => _dictionary.Remove(item);

                void ICollection.CopyTo(Array array, int index)
                    => ((ICollection)_dictionary.ReadOnlyDictionary.Keys).CopyTo(array, index);

                IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
                    => GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => GetEnumerator();
            }
        }
    }
}
