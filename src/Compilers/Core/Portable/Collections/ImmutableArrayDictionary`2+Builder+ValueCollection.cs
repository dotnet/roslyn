// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Collections
{
    partial struct ImmutableArrayDictionary<TKey, TValue>
    {
        partial class Builder
        {
            public struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>, ICollection
            {
                private readonly ImmutableArrayDictionary<TKey, TValue>.Builder _dictionary;

                internal ValueCollection(ImmutableArrayDictionary<TKey, TValue>.Builder dictionary)
                {
                    RoslynDebug.AssertNotNull(dictionary);
                    _dictionary = dictionary;
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
                    => ((ICollection<TValue>)_dictionary.ReadOnlyDictionary.Values).CopyTo(array, arrayIndex);

                public ImmutableArrayDictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator()
                    => new ImmutableArrayDictionary<TKey, TValue>.ValueCollection.Enumerator(_dictionary.GetEnumerator());

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
