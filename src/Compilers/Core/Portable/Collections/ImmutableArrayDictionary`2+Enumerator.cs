// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    partial struct ImmutableArrayDictionary<TKey, TValue>
    {
        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly Dictionary<TKey, TValue> _dictionary;
            private readonly ReturnType _returnType;
            private Dictionary<TKey, TValue>.Enumerator _enumerator;

            internal Enumerator(Dictionary<TKey, TValue> dictionary, ReturnType returnType)
            {
                _dictionary = dictionary;
                _returnType = returnType;
                _enumerator = dictionary.GetEnumerator();
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
                /// <see cref="ImmutableArrayDictionary{TKey, TValue}"/>.
                /// </summary>
                DictionaryEntry,
            }

            public KeyValuePair<TKey, TValue> Current => _enumerator.Current;

            object IEnumerator.Current => _returnType == ReturnType.DictionaryEntry ? (object)((IDictionaryEnumerator)this).Entry : Current;

            DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(Current.Key, Current.Value);

            object IDictionaryEnumerator.Key => Current.Key;

            object? IDictionaryEnumerator.Value => Current.Value;

            public void Dispose()
                => _enumerator.Dispose();

            public bool MoveNext()
                => _enumerator.MoveNext();

            public void Reset()
            {
                _enumerator = _dictionary.GetEnumerator();
            }
        }
    }
}
