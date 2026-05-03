// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial struct ImmutableSegmentedList<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            private readonly SegmentedList<T> _list;
            private SegmentedList<T>.Enumerator _enumerator;

            internal Enumerator(SegmentedList<T> list)
            {
                _list = list;
                _enumerator = list.GetEnumerator();
            }

            public readonly T Current => _enumerator.Current;

            readonly object? IEnumerator.Current => ((IEnumerator)_enumerator).Current;

            public readonly void Dispose()
                => _enumerator.Dispose();

            public bool MoveNext()
                => _enumerator.MoveNext();

            public void Reset()
            {
                // Create a new enumerator, since _enumerator.Reset() will fail for cases where the list was mutated
                // after enumeration started, and ImmutableSegmentList<T>.Builder allows for this case without error.
                _enumerator = _list.GetEnumerator();
            }
        }
    }
}
