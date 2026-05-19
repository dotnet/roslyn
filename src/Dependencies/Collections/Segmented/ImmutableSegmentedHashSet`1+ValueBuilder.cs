// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    internal readonly partial struct ImmutableSegmentedHashSet<T>
    {
        private struct ValueBuilder : ISet<T>, IReadOnlyCollection<T>
        {
            /// <summary>
            /// The immutable collection this builder is based on.
            /// </summary>
            private ImmutableSegmentedHashSet<T> _set;

            /// <summary>
            /// The current mutable collection this builder is operating on. This field is initialized to a copy of
            /// <see cref="_set"/> the first time a change is made.
            /// </summary>
            private SegmentedHashSet<T>? _mutableSet;

            internal ValueBuilder(ImmutableSegmentedHashSet<T> set)
            {
                this._set = set;
                this._mutableSet = null;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.KeyComparer"/>
            public IEqualityComparer<T> KeyComparer
            {
                readonly get
                {
                    return this.ReadOnlySet.Comparer;
                }

                set
                {
                    var self = this;
                    if (Equals(self.KeyComparer, value ?? EqualityComparer<T>.Default))
                        return;

                    self._mutableSet = new SegmentedHashSet<T>(self.ReadOnlySet, value ?? EqualityComparer<T>.Default);
                    self._set = default;
                    this = self;
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Count"/>
            public readonly int Count => this.ReadOnlySet.Count;

            internal readonly SegmentedHashSet<T> ReadOnlySet
            {
                get
                {
                    var self = this;

                    return self._mutableSet ?? self._set._set;
                }
            }

            readonly bool ICollection<T>.IsReadOnly => false;

            private SegmentedHashSet<T> GetOrCreateMutableSet()
            {
                var self = this;
                if (self._mutableSet is null)
                {
                    var originalSet = RoslynImmutableInterlocked.InterlockedExchange(ref self._set, default);
                    if (originalSet.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {self.GetType()}");

                    self._mutableSet = new SegmentedHashSet<T>(originalSet._set, originalSet.KeyComparer);
                }

                this = self;
                return self._mutableSet;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Add(T)"/>
            public bool Add(T item)
            {
                var self = this;
                if (self._mutableSet is null && self.Contains(item))
                    return false;

                bool result = self.GetOrCreateMutableSet().Add(item);
                this = self;
                return result;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Clear()"/>
            public void Clear()
            {
                var self = this;
                if (self.ReadOnlySet.Count != 0)
                {
                    if (self._mutableSet is null)
                    {
                        self._mutableSet = new SegmentedHashSet<T>(self.KeyComparer);
                        self._set = default;
                    }
                    else
                    {
                        self._mutableSet.Clear();
                    }
                }

                this = self;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Contains(T)"/>
            public readonly bool Contains(T item)
                => this.ReadOnlySet.Contains(item);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ExceptWith(IEnumerable{T})"/>
            public void ExceptWith(IEnumerable<T> other)
            {
                var self = this;
                if (other is null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);

                InnerExceptWith(other, ref self);
                this = self;

                void InnerExceptWith(IEnumerable<T> other, ref ValueBuilder self)
                {
                    if (self._mutableSet is not null)
                    {
                        self._mutableSet.ExceptWith(other);
                        return;
                    }

                    // ValueBuilder is not a public API, so there shouldn't be any callers trying to pass a boxed instance
                    // to this method.
                    Debug.Assert(other is not ValueBuilder);

                    if (other == self.ReadOnlySet)
                    {
                        self.Clear();
                        return;
                    }
                    else if (other is ImmutableSegmentedHashSet<T> otherSet)
                    {
                        if (otherSet == self._set)
                        {
                            self.Clear();
                            return;
                        }
                        else if (otherSet.IsEmpty)
                        {
                            // No action required
                            return;
                        }
                        else
                        {
                            self.GetOrCreateMutableSet().ExceptWith(otherSet._set);
                            return;
                        }
                    }
                    else
                    {
                        // Manually enumerate to avoid changes to the builder if 'other' is empty or does not contain any
                        // items present in the current set.
                        SegmentedHashSet<T>? mutableSet = null;
                        foreach (var item in other)
                        {
                            if (mutableSet is null)
                            {
                                if (!self.ReadOnlySet.Contains(item))
                                    continue;

                                mutableSet = self.GetOrCreateMutableSet();
                            }

                            mutableSet.Remove(item);
                        }

                        return;
                    }
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.GetEnumerator()"/>
            public Enumerator GetEnumerator()
                => new(this.GetOrCreateMutableSet());

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IntersectWith(IEnumerable{T})"/>
            public void IntersectWith(IEnumerable<T> other)
                => this.GetOrCreateMutableSet().IntersectWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSubsetOf(IEnumerable{T})"/>
            public readonly bool IsProperSubsetOf(IEnumerable<T> other)
                => this.ReadOnlySet.IsProperSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSupersetOf(IEnumerable{T})"/>
            public readonly bool IsProperSupersetOf(IEnumerable<T> other)
                => this.ReadOnlySet.IsProperSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSubsetOf(IEnumerable{T})"/>
            public readonly bool IsSubsetOf(IEnumerable<T> other)
                => this.ReadOnlySet.IsSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSupersetOf(IEnumerable{T})"/>
            public readonly bool IsSupersetOf(IEnumerable<T> other)
                => this.ReadOnlySet.IsSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Overlaps(IEnumerable{T})"/>
            public readonly bool Overlaps(IEnumerable<T> other)
                => this.ReadOnlySet.Overlaps(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Remove(T)"/>
            public bool Remove(T item)
            {
                var self = this;
                if (self._mutableSet is null && !self.Contains(item))
                    return false;

                bool removed = self.GetOrCreateMutableSet().Remove(item);
                this = self;
                return removed;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SetEquals(IEnumerable{T})"/>
            public readonly bool SetEquals(IEnumerable<T> other)
                => this.ReadOnlySet.SetEquals(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SymmetricExceptWith(IEnumerable{T})"/>
            public void SymmetricExceptWith(IEnumerable<T> other)
                => this.GetOrCreateMutableSet().SymmetricExceptWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.TryGetValue(T, out T)"/>
            public readonly bool TryGetValue(T equalValue, out T actualValue)
            {
                var self = this;
                if (self.ReadOnlySet.TryGetValue(equalValue, out var value))
                {
                    actualValue = value;
                    return true;
                }

                actualValue = equalValue;
                return false;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.UnionWith(IEnumerable{T})"/>
            public void UnionWith(IEnumerable<T> other)
            {
                var self = this;
                if (other is null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);

                InnerUnionWith(other, ref self);
                this = self;

                void InnerUnionWith(IEnumerable<T> other, ref ValueBuilder self)
                {
                    if (self._mutableSet is not null)
                    {
                        self._mutableSet.UnionWith(other);
                        return;
                    }

                    if (other is ImmutableSegmentedHashSet<T> { IsEmpty: true })
                    {
                        return;
                    }
                    else
                    {
                        // Manually enumerate to avoid changes to the builder if 'other' is empty or only contains items
                        // already present in the current set.
                        SegmentedHashSet<T>? mutableSet = null;
                        foreach (var item in other)
                        {
                            if (mutableSet is null)
                            {
                                if (self.ReadOnlySet.Contains(item))
                                    continue;

                                mutableSet = self.GetOrCreateMutableSet();
                            }

                            mutableSet.Add(item);
                        }

                        return;
                    }
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ToImmutable()"/>
            public ImmutableSegmentedHashSet<T> ToImmutable()
            {
                var self = this;
                self._set = new ImmutableSegmentedHashSet<T>(self.ReadOnlySet);
                self._mutableSet = null;
                this = self;
                return self._set;
            }

            void ICollection<T>.Add(T item)
                => ((ICollection<T>)this.GetOrCreateMutableSet()).Add(item);

            readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
                => ((ICollection<T>)this.ReadOnlySet).CopyTo(array, arrayIndex);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => this.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => this.GetEnumerator();
        }
    }
}
