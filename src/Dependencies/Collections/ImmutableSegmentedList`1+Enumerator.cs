// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Collections
{
    internal partial struct ImmutableSegmentedList<T>
    {
        public struct Enumerator : IEnumerator<T>
        {
            public T Current => throw null!;

            object? IEnumerator.Current => throw null!;

            public void Dispose() => throw null!;

            public bool MoveNext() => throw null!;

            public void Reset() => throw null!;
        }
    }
}
