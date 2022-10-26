// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T> : IImmutableSet<T>, ISet<T>, ICollection, IEquatable<ImmutableSegmentedHashSet<T>>
    {
        /// <inheritdoc cref="ImmutableHashSet{T}.Empty"/>
        public static readonly ImmutableSegmentedHashSet<T> Empty = new(new SegmentedHashSet<T>());

        private readonly SegmentedHashSet<T> _set;

        private ImmutableSegmentedHashSet(SegmentedHashSet<T> set)
        {
            _set = set;
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.KeyComparer"/>
        public IEqualityComparer<T> KeyComparer => _set.Comparer;

        /// <inheritdoc cref="ImmutableHashSet{T}.Count"/>
        public int Count => _set.Count;

        public bool IsDefault => _set == null;

        /// <inheritdoc cref="ImmutableHashSet{T}.IsEmpty"/>
        public bool IsEmpty => _set.Count == 0;

        bool ICollection<T>.IsReadOnly => true;

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => _set;

        public static bool operator ==(ImmutableSegmentedHashSet<T> left, ImmutableSegmentedHashSet<T> right)
            => left.Equals(right);

        public static bool operator !=(ImmutableSegmentedHashSet<T> left, ImmutableSegmentedHashSet<T> right)
            => !left.Equals(right);

        public static bool operator ==(ImmutableSegmentedHashSet<T>? left, ImmutableSegmentedHashSet<T>? right)
            => left.GetValueOrDefault().Equals(right.GetValueOrDefault());

        public static bool operator !=(ImmutableSegmentedHashSet<T>? left, ImmutableSegmentedHashSet<T>? right)
            => !left.GetValueOrDefault().Equals(right.GetValueOrDefault());

        /// <inheritdoc cref="ImmutableHashSet{T}.Add(T)"/>
        public ImmutableSegmentedHashSet<T> Add(T value)
        {
            var self = this;

            if (self.IsEmpty)
            {
                var set = new SegmentedHashSet<T>(self.KeyComparer) { value };
                return new ImmutableSegmentedHashSet<T>(set);
            }
            else if (self.Contains(value))
            {
                return self;
            }
            else
            {
                // TODO: Avoid the builder allocation
                // TODO: Reuse all pages with no changes
                var builder = self.ToBuilder();
                builder.Add(value);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.Clear()"/>
        public ImmutableSegmentedHashSet<T> Clear()
        {
            var self = this;

            if (self.IsEmpty)
            {
                return self;
            }

            return Empty.WithComparer(self.KeyComparer);
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.Contains(T)"/>
        public bool Contains(T value)
            => _set.Contains(value);

        /// <inheritdoc cref="ImmutableHashSet{T}.Except(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Except(IEnumerable<T> other)
        {
            var self = this;

            if (other is ImmutableSegmentedHashSet<T> { IsEmpty: true })
            {
                return self;
            }
            else if (self.IsEmpty)
            {
                // Enumerate the argument to match behavior of ImmutableHashSet<T>
                foreach (var _ in other)
                {
                    // Intentionally empty
                }

                return self;
            }
            else
            {
                // TODO: Avoid the builder allocation
                // TODO: Reuse all pages with no changes
                var builder = self.ToBuilder();
                builder.ExceptWith(other);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.GetEnumerator()"/>
        public Enumerator GetEnumerator()
            => new(_set);

        /// <inheritdoc cref="ImmutableHashSet{T}.Intersect(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Intersect(IEnumerable<T> other)
        {
            var self = this;

            if (self.IsEmpty || other is ImmutableSegmentedHashSet<T> { IsEmpty: true })
            {
                return self.Clear();
            }
            else
            {
                // TODO: Avoid the builder allocation
                // TODO: Reuse all pages with no changes
                var builder = self.ToBuilder();
                builder.IntersectWith(other);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.IsProperSubsetOf(IEnumerable{T})"/>
        public bool IsProperSubsetOf(IEnumerable<T> other)
            => _set.IsProperSubsetOf(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.IsProperSupersetOf(IEnumerable{T})"/>
        public bool IsProperSupersetOf(IEnumerable<T> other)
            => _set.IsProperSupersetOf(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.IsSubsetOf(IEnumerable{T})"/>
        public bool IsSubsetOf(IEnumerable<T> other)
            => _set.IsSubsetOf(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.IsSupersetOf(IEnumerable{T})"/>
        public bool IsSupersetOf(IEnumerable<T> other)
            => _set.IsSupersetOf(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.Overlaps(IEnumerable{T})"/>
        public bool Overlaps(IEnumerable<T> other)
            => _set.Overlaps(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.Remove(T)"/>
        public ImmutableSegmentedHashSet<T> Remove(T value)
        {
            var self = this;

            if (!self.Contains(value))
            {
                return self;
            }
            else
            {
                // TODO: Avoid the builder allocation
                // TODO: Reuse all pages with no changes
                var builder = self.ToBuilder();
                builder.Remove(value);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.SetEquals(IEnumerable{T})"/>
        public bool SetEquals(IEnumerable<T> other)
            => _set.SetEquals(other);

        /// <inheritdoc cref="ImmutableHashSet{T}.SymmetricExcept(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> SymmetricExcept(IEnumerable<T> other)
        {
            var self = this;

            if (other is ImmutableSegmentedHashSet<T> otherSet)
            {
                if (otherSet.IsEmpty)
                    return self;
                else if (self.IsEmpty)
                    return otherSet.WithComparer(self.KeyComparer);
            }

            if (self.IsEmpty)
            {
                return ImmutableSegmentedHashSet.CreateRange(self.KeyComparer, other);
            }
            else
            {
                // TODO: Avoid the builder allocation
                // TODO: Reuse all pages with no changes
                var builder = self.ToBuilder();
                builder.SymmetricExceptWith(other);
                return builder.ToImmutable();
            }
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.TryGetValue(T, out T)"/>
        public bool TryGetValue(T equalValue, out T actualValue)
        {
            if (_set.TryGetValue(equalValue, out var value))
            {
                actualValue = value;
                return true;
            }

            actualValue = equalValue;
            return false;
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.Union(IEnumerable{T})"/>
        public ImmutableSegmentedHashSet<T> Union(IEnumerable<T> other)
        {
            var self = this;

            if (other is ImmutableSegmentedHashSet<T> otherSet)
            {
                if (otherSet.IsEmpty)
                    return self;
                else if (self.IsEmpty)
                    return otherSet.WithComparer(self.KeyComparer);
            }

            // TODO: Avoid the builder allocation
            // TODO: Reuse all pages with no changes
            var builder = self.ToBuilder();
            builder.UnionWith(other);
            return builder.ToImmutable();
        }

        /// <inheritdoc cref="ImmutableHashSet{T}.ToBuilder()"/>
        public Builder ToBuilder()
            => new(this);

        /// <inheritdoc cref="ImmutableHashSet{T}.WithComparer(IEqualityComparer{T}?)"/>
        public ImmutableSegmentedHashSet<T> WithComparer(IEqualityComparer<T>? equalityComparer)
        {
            var self = this;

            equalityComparer ??= EqualityComparer<T>.Default;
            if (Equals(self.KeyComparer, equalityComparer))
                return self;

            return new ImmutableSegmentedHashSet<T>(new SegmentedHashSet<T>(self._set, equalityComparer));
        }

        public override int GetHashCode()
            => _set?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
            => obj is ImmutableSegmentedHashSet<T> other && Equals(other);

        public bool Equals(ImmutableSegmentedHashSet<T> other)
            => _set == other._set;

        IImmutableSet<T> IImmutableSet<T>.Clear()
            => Clear();

        IImmutableSet<T> IImmutableSet<T>.Add(T value)
            => Add(value);

        IImmutableSet<T> IImmutableSet<T>.Remove(T value)
            => Remove(value);

        IImmutableSet<T> IImmutableSet<T>.Intersect(IEnumerable<T> other)
            => Intersect(other);

        IImmutableSet<T> IImmutableSet<T>.Except(IEnumerable<T> other)
            => Except(other);

        IImmutableSet<T> IImmutableSet<T>.SymmetricExcept(IEnumerable<T> other)
            => SymmetricExcept(other);

        IImmutableSet<T> IImmutableSet<T>.Union(IEnumerable<T> other)
            => Union(other);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
            => _set.CopyTo(array, arrayIndex);

        void ICollection.CopyTo(Array array, int index)
        {
            if (array is null)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            if (index < 0)
                ThrowHelper.ThrowArgumentOutOfRange_IndexException();
            if (array.Length < index + Count)
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);

            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

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
