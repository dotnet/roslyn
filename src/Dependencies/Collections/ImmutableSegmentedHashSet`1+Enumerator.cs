// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T>
    {
        /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator"/>
        public struct Enumerator : IEnumerator<T>
        {
            internal Enumerator(SegmentedHashSet<T> set)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Current"/>
            public T Current => throw null!;

            object? IEnumerator.Current => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Dispose()"/>
            public void Dispose()
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.MoveNext()"/>
            public bool MoveNext()
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Reset()"/>
            public void Reset()
                => throw null!;
        }
    }
}
