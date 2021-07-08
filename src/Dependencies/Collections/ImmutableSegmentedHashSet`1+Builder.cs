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
        public sealed class Builder : ISet<T>, IReadOnlyCollection<T>
        {
            internal Builder(ImmutableSegmentedHashSet<T> set)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.KeyComparer"/>
            public IEqualityComparer<T> KeyComparer => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Count"/>
            public int Count => throw null!;

            bool ICollection<T>.IsReadOnly => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Add(T)"/>
            public bool Add(T item)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Clear()"/>
            public void Clear()
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Contains(T)"/>
            public bool Contains(T item)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ExceptWith(IEnumerable{T})"/>
            public void ExceptWith(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.GetEnumerator()"/>
            public Enumerator GetEnumerator()
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IntersectWith(IEnumerable{T})"/>
            public void IntersectWith(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSubsetOf(IEnumerable{T})"/>
            public bool IsProperSubsetOf(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSupersetOf(IEnumerable{T})"/>
            public bool IsProperSupersetOf(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSubsetOf(IEnumerable{T})"/>
            public bool IsSubsetOf(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSupersetOf(IEnumerable{T})"/>
            public bool IsSupersetOf(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Overlaps(IEnumerable{T})"/>
            public bool Overlaps(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Remove(T)"/>
            public bool Remove(T item)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SetEquals(IEnumerable{T})"/>
            public bool SetEquals(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SymmetricExceptWith(IEnumerable{T})"/>
            public void SymmetricExceptWith(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.UnionWith(IEnumerable{T})"/>
            public void UnionWith(IEnumerable<T> other)
                => throw null!;

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ToImmutable()"/>
            public ImmutableSegmentedHashSet<T> ToImmutable()
                => throw null!;

            void ICollection<T>.Add(T item)
                => throw null!;

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
                => throw null!;

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => throw null!;

            IEnumerator IEnumerable.GetEnumerator()
                => throw null!;
        }
    }
}
