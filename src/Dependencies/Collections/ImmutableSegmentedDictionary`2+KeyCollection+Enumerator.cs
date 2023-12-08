// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedDictionary<TKey, TValue>
    {
        public partial struct KeyCollection
        {
            public struct Enumerator : IEnumerator<TKey>
            {
                private ImmutableSegmentedDictionary<TKey, TValue>.Enumerator _enumerator;

                internal Enumerator(ImmutableSegmentedDictionary<TKey, TValue>.Enumerator enumerator)
                {
                    _enumerator = enumerator;
                }

                public readonly TKey Current => _enumerator.Current.Key;

                readonly object IEnumerator.Current => Current;

                public readonly void Dispose()
                    => _enumerator.Dispose();

                public bool MoveNext()
                    => _enumerator.MoveNext();

                public void Reset()
                    => _enumerator.Reset();
            }
        }
    }
}
