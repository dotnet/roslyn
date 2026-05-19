// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly SegmentedDictionary<TKey, TValue> _dictionary;
            private readonly ReturnType _returnType;
            private SegmentedDictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(SegmentedDictionary<TKey, TValue> dictionary, ReturnType returnType)
            {
                this._dictionary = dictionary;
                this._returnType = returnType;
                this._enumerator = dictionary.GetEnumerator();
            }

            internal enum ReturnType
            {
                /// <summary>
                /// The return value from the implementation of <see cref="IEnumerable.GetEnumerator"/> is
                /// <see cref="KeyValuePair{TKey, TValue}"/>. This is the return value for most instances of this
                /// enumerator.
                /// </summary>
                KeyValuePair,

                /// <summary>
                /// The return value from the implementation of <see cref="IEnumerable.GetEnumerator"/> is
                /// <see cref="System.Collections.DictionaryEntry"/>. This is the return value for instances of this
                /// enumerator created by the <see cref="IDictionary.GetEnumerator"/> implementation in
                /// <see cref="ImmutableSegmentedDictionary{TKey, TValue}"/>.
                /// </summary>
                DictionaryEntry,
            }

            public readonly KeyValuePair<TKey, TValue> Current => this._enumerator.Current;

            readonly object IEnumerator.Current
            {
                get
                {
                    var self = this;
                    return self._returnType == ReturnType.DictionaryEntry ? ((IDictionaryEnumerator)self).Entry : self.Current;
                }
            }

            readonly DictionaryEntry IDictionaryEnumerator.Entry
            {
                get
                {
                    var current = this.Current;
                    return new(current.Key, current.Value);
                }
            }

            readonly object IDictionaryEnumerator.Key => this.Current.Key;

            readonly object? IDictionaryEnumerator.Value => this.Current.Value;

            public readonly void Dispose()
                => this._enumerator.Dispose();

            public bool MoveNext()
                => this._enumerator.MoveNext();

            public void Reset()
            {
                var self = this;
                self._enumerator = self._dictionary.GetEnumerator();
                this = self;
            }
        }
    }
}
