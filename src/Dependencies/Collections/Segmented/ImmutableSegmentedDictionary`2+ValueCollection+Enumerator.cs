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
        public partial struct ValueCollection
        {
            public struct Enumerator : IEnumerator<TValue>
            {
                private ImmutableSegmentedDictionary<TKey, TValue>.Enumerator _enumerator;

                internal Enumerator(ImmutableSegmentedDictionary<TKey, TValue>.Enumerator enumerator)
                {
                    this._enumerator = enumerator;
                }

                public readonly TValue Current => this._enumerator.Current.Value;

                readonly object? IEnumerator.Current => this.Current;

                public readonly void Dispose()
                    => this._enumerator.Dispose();

                public bool MoveNext()
                    => this._enumerator.MoveNext();

                public void Reset()
                    => this._enumerator.Reset();
            }
        }
    }
}
