// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
                    this._dictionary = dictionary!;
                }

                public int Count => this._dictionary.Count;

                bool ICollection<TKey>.IsReadOnly => false;

                bool ICollection.IsSynchronized => false;

                object ICollection.SyncRoot => ((ICollection)this._dictionary).SyncRoot;

                void ICollection<TKey>.Add(TKey item)
                    => throw new NotSupportedException();

                public void Clear()
                    => this._dictionary.Clear();

                public bool Contains(TKey item)
                    => this._dictionary.ContainsKey(item);

                public void CopyTo(TKey[] array, int arrayIndex)
                    => this._dictionary.ReadOnlyDictionary.Keys.CopyTo(array, arrayIndex);

                public ImmutableSegmentedDictionary<TKey, TValue>.KeyCollection.Enumerator GetEnumerator()
                    => new(this._dictionary.GetEnumerator());

                public bool Remove(TKey item)
                    => this._dictionary.Remove(item);

                void ICollection.CopyTo(Array array, int index)
                    => ((ICollection)this._dictionary.ReadOnlyDictionary.Keys).CopyTo(array, index);

                IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
                    => this.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => this.GetEnumerator();
            }
        }
    }
}
