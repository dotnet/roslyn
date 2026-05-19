// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T>
    {
        public sealed class Builder : ISet<T>, IReadOnlyCollection<T>
        {
            /// <summary>
            /// The private builder implementation.
            /// </summary>
            private ValueBuilder _builder;

            internal Builder(ImmutableSegmentedHashSet<T> set)
                => this._builder = new ValueBuilder(set);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.KeyComparer"/>
            public IEqualityComparer<T> KeyComparer
            {
                get => this._builder.KeyComparer;
                set => this._builder.KeyComparer = value;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Count"/>
            public int Count => this._builder.Count;

            bool ICollection<T>.IsReadOnly => ICollectionCalls<T>.IsReadOnly(ref this._builder);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Add(T)"/>
            public bool Add(T item)
                => this._builder.Add(item);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Clear()"/>
            public void Clear()
                => this._builder.Clear();

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Contains(T)"/>
            public bool Contains(T item)
                => this._builder.Contains(item);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ExceptWith(IEnumerable{T})"/>
            public void ExceptWith(IEnumerable<T> other)
            {
                var self = this;
                if (other == self)
                {
                    // The ValueBuilder knows how to optimize for this case by calling Clear, provided it does not need
                    // to access properties of the wrapping Builder instance.
                    self._builder.ExceptWith(self._builder.ReadOnlySet);
                }
                else
                {
                    self._builder.ExceptWith(other);
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.GetEnumerator()"/>
            public Enumerator GetEnumerator()
                => this._builder.GetEnumerator();

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IntersectWith(IEnumerable{T})"/>
            public void IntersectWith(IEnumerable<T> other)
                => this._builder.IntersectWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSubsetOf(IEnumerable{T})"/>
            public bool IsProperSubsetOf(IEnumerable<T> other)
                => this._builder.IsProperSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSupersetOf(IEnumerable{T})"/>
            public bool IsProperSupersetOf(IEnumerable<T> other)
                => this._builder.IsProperSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSubsetOf(IEnumerable{T})"/>
            public bool IsSubsetOf(IEnumerable<T> other)
                => this._builder.IsSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSupersetOf(IEnumerable{T})"/>
            public bool IsSupersetOf(IEnumerable<T> other)
                => this._builder.IsSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Overlaps(IEnumerable{T})"/>
            public bool Overlaps(IEnumerable<T> other)
                => this._builder.Overlaps(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Remove(T)"/>
            public bool Remove(T item)
                => this._builder.Remove(item);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SetEquals(IEnumerable{T})"/>
            public bool SetEquals(IEnumerable<T> other)
                => this._builder.SetEquals(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SymmetricExceptWith(IEnumerable{T})"/>
            public void SymmetricExceptWith(IEnumerable<T> other)
                => this._builder.SymmetricExceptWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.TryGetValue(T, out T)"/>
            public bool TryGetValue(T equalValue, out T actualValue)
                => this._builder.TryGetValue(equalValue, out actualValue);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.UnionWith(IEnumerable{T})"/>
            public void UnionWith(IEnumerable<T> other)
                => this._builder.UnionWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ToImmutable()"/>
            public ImmutableSegmentedHashSet<T> ToImmutable()
                => this._builder.ToImmutable();

            void ICollection<T>.Add(T item)
                => ICollectionCalls<T>.Add(ref this._builder, item);

            void ICollection<T>.CopyTo(T[] array, int arrayIndex)
                => ICollectionCalls<T>.CopyTo(ref this._builder, array, arrayIndex);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => IEnumerableCalls<T>.GetEnumerator(ref this._builder);

            IEnumerator IEnumerable.GetEnumerator()
                => IEnumerableCalls.GetEnumerator(ref this._builder);
        }
    }
}
