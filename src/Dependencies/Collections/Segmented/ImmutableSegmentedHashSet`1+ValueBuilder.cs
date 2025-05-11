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
                _set = set;
                _mutableSet = null;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.KeyComparer"/>
            public IEqualityComparer<T> KeyComparer
            {
                readonly get
                {
                    return ReadOnlySet.Comparer;
                }

                set
                {
                    if (Equals(KeyComparer, value ?? EqualityComparer<T>.Default))
                        return;

                    _mutableSet = new SegmentedHashSet<T>(ReadOnlySet, value ?? EqualityComparer<T>.Default);
                    _set = default;
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Count"/>
            public readonly int Count => ReadOnlySet.Count;

            internal readonly SegmentedHashSet<T> ReadOnlySet => _mutableSet ?? _set._set;

            readonly bool ICollection<T>.IsReadOnly => false;

            private SegmentedHashSet<T> GetOrCreateMutableSet()
            {
                if (_mutableSet is null)
                {
                    var originalSet = RoslynImmutableInterlocked.InterlockedExchange(ref _set, default);
                    if (originalSet.IsDefault)
                        throw new InvalidOperationException($"Unexpected concurrent access to {GetType()}");

                    _mutableSet = new SegmentedHashSet<T>(originalSet._set, originalSet.KeyComparer);
                }

                return _mutableSet;
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Add(T)"/>
            public bool Add(T item)
            {
                if (_mutableSet is null && Contains(item))
                    return false;

                return GetOrCreateMutableSet().Add(item);
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Clear()"/>
            public void Clear()
            {
                if (ReadOnlySet.Count != 0)
                {
                    if (_mutableSet is null)
                    {
                        _mutableSet = new SegmentedHashSet<T>(KeyComparer);
                        _set = default;
                    }
                    else
                    {
                        _mutableSet.Clear();
                    }
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Contains(T)"/>
            public readonly bool Contains(T item)
                => ReadOnlySet.Contains(item);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ExceptWith(IEnumerable{T})"/>
            public void ExceptWith(IEnumerable<T> other)
            {
                if (other is null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);

                if (_mutableSet is not null)
                {
                    _mutableSet.ExceptWith(other);
                    return;
                }

                // ValueBuilder is not a public API, so there shouldn't be any callers trying to pass a boxed instance
                // to this method.
                Debug.Assert(other is not ValueBuilder);

                if (other == ReadOnlySet)
                {
                    Clear();
                    return;
                }
                else if (other is ImmutableSegmentedHashSet<T> otherSet)
                {
                    if (otherSet == _set)
                    {
                        Clear();
                        return;
                    }
                    else if (otherSet.IsEmpty)
                    {
                        // No action required
                        return;
                    }
                    else
                    {
                        GetOrCreateMutableSet().ExceptWith(otherSet._set);
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
                            if (!ReadOnlySet.Contains(item))
                                continue;

                            mutableSet = GetOrCreateMutableSet();
                        }

                        mutableSet.Remove(item);
                    }

                    return;
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.GetEnumerator()"/>
            public Enumerator GetEnumerator()
                => new(GetOrCreateMutableSet());

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IntersectWith(IEnumerable{T})"/>
            public void IntersectWith(IEnumerable<T> other)
                => GetOrCreateMutableSet().IntersectWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSubsetOf(IEnumerable{T})"/>
            public readonly bool IsProperSubsetOf(IEnumerable<T> other)
                => ReadOnlySet.IsProperSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsProperSupersetOf(IEnumerable{T})"/>
            public readonly bool IsProperSupersetOf(IEnumerable<T> other)
                => ReadOnlySet.IsProperSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSubsetOf(IEnumerable{T})"/>
            public readonly bool IsSubsetOf(IEnumerable<T> other)
                => ReadOnlySet.IsSubsetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.IsSupersetOf(IEnumerable{T})"/>
            public readonly bool IsSupersetOf(IEnumerable<T> other)
                => ReadOnlySet.IsSupersetOf(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Overlaps(IEnumerable{T})"/>
            public readonly bool Overlaps(IEnumerable<T> other)
                => ReadOnlySet.Overlaps(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.Remove(T)"/>
            public bool Remove(T item)
            {
                if (_mutableSet is null && !Contains(item))
                    return false;

                return GetOrCreateMutableSet().Remove(item);
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SetEquals(IEnumerable{T})"/>
            public readonly bool SetEquals(IEnumerable<T> other)
                => ReadOnlySet.SetEquals(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.SymmetricExceptWith(IEnumerable{T})"/>
            public void SymmetricExceptWith(IEnumerable<T> other)
                => GetOrCreateMutableSet().SymmetricExceptWith(other);

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.TryGetValue(T, out T)"/>
            public readonly bool TryGetValue(T equalValue, out T actualValue)
            {
                if (ReadOnlySet.TryGetValue(equalValue, out var value))
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
                if (other is null)
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);

                if (_mutableSet is not null)
                {
                    _mutableSet.UnionWith(other);
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
                            if (ReadOnlySet.Contains(item))
                                continue;

                            mutableSet = GetOrCreateMutableSet();
                        }

                        mutableSet.Add(item);
                    }

                    return;
                }
            }

            /// <inheritdoc cref="ImmutableHashSet{T}.Builder.ToImmutable()"/>
            public ImmutableSegmentedHashSet<T> ToImmutable()
            {
                _set = new ImmutableSegmentedHashSet<T>(ReadOnlySet);
                _mutableSet = null;
                return _set;
            }

            void ICollection<T>.Add(T item)
                => ((ICollection<T>)GetOrCreateMutableSet()).Add(item);

            readonly void ICollection<T>.CopyTo(T[] array, int arrayIndex)
                => ((ICollection<T>)ReadOnlySet).CopyTo(array, arrayIndex);

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
                => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
