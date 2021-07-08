// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T> : IImmutableSet<T>, ISet<T>, ICollection, IEquatable<ImmutableSegmentedHashSet<T>>
    {
        /// <inheritdoc cref="ImmutableHashSet{T}.Empty"/>
        public static readonly ImmutableSegmentedHashSet<T> Empty;

        /// <inheritdoc cref="ImmutableHashSet{T}.KeyComparer"/>
        public IEqualityComparer<T> KeyComparer => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Count"/>
        public int Count => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsEmpty"/>
        public bool IsEmpty => throw null!;

        bool ICollection<T>.IsReadOnly => throw null!;

        bool ICollection.IsSynchronized => throw null!;

        object ICollection.SyncRoot => throw null!;

        public static bool operator ==(ImmutableSegmentedHashSet<T> left, ImmutableSegmentedHashSet<T> right)
            => throw null!;

        public static bool operator !=(ImmutableSegmentedHashSet<T> left, ImmutableSegmentedHashSet<T> right)
            => throw null!;

        public static bool operator ==(ImmutableSegmentedHashSet<T>? left, ImmutableSegmentedHashSet<T>? right)
            => throw null!;

        public static bool operator !=(ImmutableSegmentedHashSet<T>? left, ImmutableSegmentedHashSet<T>? right)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Add(T)"/>
        public ImmutableSegmentedHashSet<T> Add(T value)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Clear()"/>
        public ImmutableSegmentedHashSet<T> Clear()
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Contains(T)"/>
        public bool Contains(T value)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Except(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Except(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Intersect(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Intersect(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsProperSubsetOf(IEnumerable{T})"/>
        public bool IsProperSubsetOf(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsProperSupersetOf(IEnumerable{T})"/>
        public bool IsProperSupersetOf(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsSubsetOf(IEnumerable{T})"/>
        public bool IsSubsetOf(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsSupersetOf(IEnumerable{T})"/>
        public bool IsSupersetOf(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Overlaps(IEnumerable{T})"/>
        public bool Overlaps(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Remove(T)"/>
        public ImmutableSegmentedHashSet<T> Remove(T value)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.SetEquals(IEnumerable{T})"/>
        public bool SetEquals(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.SymmetricExcept(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> SymmetricExcept(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.TryGetValue(T, out T)"/>
        public bool TryGetValue(T equalValue, out T actualValue)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.Union(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Union(IEnumerable<T> other)
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.ToBuilder()"/>
        public Builder ToBuilder()
            => throw null!;

        /// <inheritdoc cref="ImmutableHashSet{T}.WithComparer(IEqualityComparer{T}?)"/>
        public ImmutableSegmentedHashSet<T> WithComparer(IEqualityComparer<T>? equalityComparer)
            => throw null!;

        public override int GetHashCode()
            => throw null!;

        public override bool Equals(object? obj)
            => throw null!;

        public bool Equals(ImmutableSegmentedHashSet<T> other)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Clear()
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Add(T value)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Remove(T value)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other)
            => throw null!;

        IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other)
            => throw null!;

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            => throw null!;

        void ICollection.CopyTo(Array array, int index)
            => throw null!;

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => throw null!;

        IEnumerator IEnumerable.GetEnumerator()
            => throw null!;

        bool ISet<T>.Add(T item)
            => throw new NotSupportedException();

        void ISet<T>.UnionWith(IEnumerable<T> other)
            => throw new NotSupportedException();

        void ISet<T>.IntersectWith(IEnumerable<T> other)
            => throw new NotSupportedException();

        void ISet<T>.ExceptWith(IEnumerable<T> other)
            => throw new NotSupportedException();

        void ISet<T>.SymmetricExceptWith(IEnumerable<T> other)
            => throw new NotSupportedException();

        void ICollection<T>.Add(T item)
            => throw new NotSupportedException();

        void ICollection<T>.Clear()
            => throw new NotSupportedException();

        bool ICollection<T>.Remove(T item)
            => throw new NotSupportedException();
    }
}
