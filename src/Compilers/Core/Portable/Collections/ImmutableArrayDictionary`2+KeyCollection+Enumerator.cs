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
        partial struct KeyCollection
        {
            public struct Enumerator : IEnumerator<TKey>
            {
                private ImmutableArrayDictionary<TKey, TValue>.Enumerator _enumerator;

                internal Enumerator(ImmutableArrayDictionary<TKey, TValue>.Enumerator enumerator)
                {
                    _enumerator = enumerator;
                }

                public TKey Current => _enumerator.Current.Key;

                object IEnumerator.Current => Current;

                public void Dispose()
                    => _enumerator.Dispose();

                public bool MoveNext()
                    => _enumerator.MoveNext();

                public void Reset()
                    => _enumerator.Reset();
            }
        }
    }
}
