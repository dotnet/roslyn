// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            private readonly SegmentedHashSet<T> _set;
            private SegmentedHashSet<T>.Enumerator _enumerator;

            internal Enumerator(SegmentedHashSet<T> set)
            {
                this._set = set;
                this._enumerator = set.GetEnumerator();
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Current"/>
            public readonly T Current => this._enumerator.Current;

            readonly object? IEnumerator.Current => ((IEnumerator)this._enumerator).Current;

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Dispose()"/>
            public readonly void Dispose()
                => this._enumerator.Dispose();

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.MoveNext()"/>
            public bool MoveNext()
                => this._enumerator.MoveNext();

            /// <inheritdoc cref="ImmutableHashSet{T}.Enumerator.Reset()"/>
            public void Reset()
            {
                var self = this;
                // Create a new enumerator, since _enumerator.Reset() will fail for cases where the set was mutated
                // after enumeration started, and ImmutableSegmentHashSet<T>.Builder allows for this case without error.
                self._enumerator = self._set.GetEnumerator();
                this = self;
            }
        }
    }
}
