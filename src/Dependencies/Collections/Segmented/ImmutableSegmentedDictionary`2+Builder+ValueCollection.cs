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
            public readonly struct ValueCollection : ICollection<TValue>, IReadOnlyCollection<TValue>, ICollection
            {
                private readonly ImmutableSegmentedDictionary<TKey, TValue>.Builder _dictionary;

                internal ValueCollection(ImmutableSegmentedDictionary<TKey, TValue>.Builder dictionary)
                {
                    Debug.Assert(dictionary is not null);
                    this._dictionary = dictionary!;
                }

                public int Count => this._dictionary.Count;

                bool ICollection<TValue>.IsReadOnly => false;

                bool ICollection.IsSynchronized => false;

                object ICollection.SyncRoot => ((ICollection)this._dictionary).SyncRoot;

                void ICollection<TValue>.Add(TValue item)
                    => throw new NotSupportedException();

                public void Clear()
                    => this._dictionary.Clear();

                public bool Contains(TValue item)
                    => this._dictionary.ContainsValue(item);

                public void CopyTo(TValue[] array, int arrayIndex)
                    => this._dictionary.ReadOnlyDictionary.Values.CopyTo(array, arrayIndex);

                public ImmutableSegmentedDictionary<TKey, TValue>.ValueCollection.Enumerator GetEnumerator()
                    => new(this._dictionary.GetEnumerator());

                bool ICollection<TValue>.Remove(TValue item)
                    => throw new NotSupportedException();

                void ICollection.CopyTo(Array array, int index)
                    => ((ICollection)this._dictionary.ReadOnlyDictionary.Values).CopyTo(array, index);

                IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
                    => this.GetEnumerator();

                IEnumerator IEnumerable.GetEnumerator()
                    => this.GetEnumerator();
            }
        }
    }
}
