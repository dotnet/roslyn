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
            public readonly struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>, ICollection
            {
                private readonly ImmutableSegmentedDictionary<TKey, TValue>.Builder _dictionary;

                internal ValueCollection(ImmutableSegmentedDictionary<TKey, TValue>.Builder dictionary)
                {
                    Debug.Assert(dictionary is not null);
                    _dictionary = dictionary!;
                }

                public int Count => _dictionary.Count;

                bool ICollection<TValue>.IsReadOnly => false;

                bool ICollection.IsSynchronized => false;

                object ICollection.SyncRoot => ((ICollection)_dictionary).SyncRoot;

                void ICollection<TValue>.Add(TValue item)
                    => throw new NotSupportedException();

                public void Clear()
                    => _dictionary.Clear();

                public bool Contains(TValue item)
                    => _dictionary.ContainsValue(item);

                public void CopyTo(TValue[] array, int arrayIndex)
                    => _dictionary.ReadOnlyDictionary.Values.CopyTo(array, arrayIndex);

                public ImmutableSegmentedDictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator()
                    => new(_dictionary.GetEnumerator());

                bool ICollection<TValue>.Remove(TValue item)
                    => throw new NotSupportedException();

                void ICollection.CopyTo(Array array, int index)
                    => ((ICollection)_dictionary.ReadOnlyDictionary.Values).CopyTo(array, index);

                IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
                    => GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => GetEnumerator();
            }
        }
    }
}
