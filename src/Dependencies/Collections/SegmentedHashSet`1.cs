// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// NOTE: This code is derived from an implementation originally in dotnet/runtime:
// https://github.com/dotnet/runtime/blob/v5.0.7/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/HashSet.cs
//
// See the commentary in https://github.com/dotnet/roslyn/pull/50156 for notes on incorporating changes made to the
// reference implementation.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Collections.Internal;

namespace Microsoft.CodeAnalysis.Collections
{
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    internal class SegmentedHashSet<T> : ICollection<T>, ISet<T>, IReadOnlyCollection<T>
#if NET5_0_OR_GREATER
        , IReadOnlySet<T>
#endif
    {
        private const bool SupportsComparerDevirtualization
#if NETCOREAPP
            = true;
#else
            = false;
#endif

        // This uses the same array-based implementation as Dictionary<TKey, TValue>.

        /// <summary>Cutoff point for stackallocs. This corresponds to the number of ints.</summary>
        private const int StackAllocThreshold = 100;

        /// <summary>
        /// When constructing a hashset from an existing collection, it may contain duplicates,
        /// so this is used as the max acceptable excess ratio of capacity to count. Note that
        /// this is only used on the ctor and not to automatically shrink if the hashset has, e.g,
        /// a lot of adds followed by removes. Users must explicitly shrink by calling TrimExcess.
        /// This is set to 3 because capacity is acceptable as 2x rounded up to nearest prime.
        /// </summary>
        private const int ShrinkThreshold = 3;
        private const int StartOfFreeList = -3;

        private SegmentedArray<int> _buckets;
        private SegmentedArray<Entry> _entries;
        private ulong _fastModMultiplier;
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
#if NETCOREAPP
        private readonly IEqualityComparer<T>? _comparer;
#else
        /// <summary>
        /// <see cref="EqualityComparer{T}.Default"/> doesn't devirtualize on .NET Framework, so we always ensure
        /// <see cref="_comparer"/> is initialized to a non-<see langword="null"/> value.
        /// </summary>
        private readonly IEqualityComparer<T> _comparer;
#endif

        #region Constructors

        public SegmentedHashSet() : this((IEqualityComparer<T>?)null) { }

        public SegmentedHashSet(IEqualityComparer<T>? comparer)
        {
            if (comparer != null && comparer != EqualityComparer<T>.Default) // first check for null to avoid forcing default comparer instantiation unnecessarily
            {
                _comparer = comparer;
            }

#if !NETCOREAPP
            // .NET Framework doesn't support devirtualization, so we always initialize comparer to a non-null value
            _comparer ??= EqualityComparer<T>.Default;
#endif
        }

        public SegmentedHashSet(int capacity) : this(capacity, null) { }

        public SegmentedHashSet(IEnumerable<T> collection) : this(collection, null) { }

        public SegmentedHashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer) : this(comparer)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            if (collection is SegmentedHashSet<T> otherAsHashSet && EqualityComparersAreEqual(this, otherAsHashSet))
            {
                ConstructFrom(otherAsHashSet);
            }
            else
            {
                // To avoid excess resizes, first set size based on collection's count. The collection may
                // contain duplicates, so call TrimExcess if resulting SegmentedHashSet is larger than the threshold.
                if (collection is ICollection<T> coll)
                {
                    var count = coll.Count;
                    if (count > 0)
                    {
                        Initialize(count);
                    }
                }

                UnionWith(collection);

                if (_count > 0 && _entries.Length / _count > ShrinkThreshold)
                {
                    TrimExcess();
                }
            }
        }

        public SegmentedHashSet(int capacity, IEqualityComparer<T>? comparer) : this(comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            if (capacity > 0)
            {
                Initialize(capacity);
            }
        }

        /// <summary>Initializes the SegmentedHashSet from another SegmentedHashSet with the same element type and equality comparer.</summary>
        private void ConstructFrom(SegmentedHashSet<T> source)
        {
            if (source.Count == 0)
            {
                // As well as short-circuiting on the rest of the work done,
                // this avoids errors from trying to access source._buckets
                // or source._entries when they aren't initialized.
                return;
            }

            var capacity = source._buckets.Length;
            var threshold = HashHelpers.ExpandPrime(source.Count + 1);

            if (threshold >= capacity)
            {
                _buckets = (SegmentedArray<int>)source._buckets.Clone();
                _entries = (SegmentedArray<Entry>)source._entries.Clone();
                _freeList = source._freeList;
                _freeCount = source._freeCount;
                _count = source._count;
                _fastModMultiplier = source._fastModMultiplier;
            }
            else
            {
                Initialize(source.Count);

                var entries = source._entries;
                for (var i = 0; i < source._count; i++)
                {
                    ref var entry = ref entries[i];
                    if (entry._next >= -1)
                    {
                        AddIfNotPresent(entry._value, out _);
                    }
                }
            }

            Debug.Assert(Count == source.Count);
        }

        #endregion

        #region ICollection<T> methods

        void ICollection<T>.Add(T item) => AddIfNotPresent(item, out _);

        /// <summary>Removes all elements from the <see cref="SegmentedHashSet{T}"/> object.</summary>
        public void Clear()
        {
            var count = _count;
            if (count > 0)
            {
                Debug.Assert(_buckets.Length > 0, "_buckets should be non-empty");
                Debug.Assert(_entries.Length > 0, "_entries should be non-empty");

                SegmentedArray.Clear(_buckets, 0, _buckets.Length);
                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                SegmentedArray.Clear(_entries, 0, count);
            }
        }

        /// <summary>Determines whether the <see cref="SegmentedHashSet{T}"/> contains the specified element.</summary>
        /// <param name="item">The element to locate in the <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object contains the specified element; otherwise, false.</returns>
        public bool Contains(T item) => FindItemIndex(item) >= 0;

        /// <summary>Gets the index of the item in <see cref="_entries"/>, or -1 if it's not in the set.</summary>
        private int FindItemIndex(T item)
        {
            var buckets = _buckets;
            if (buckets.Length > 0)
            {
                var entries = _entries;
                Debug.Assert(entries.Length > 0, "Expected _entries to be initialized");

                uint collisionCount = 0;
                var comparer = _comparer;

                if (SupportsComparerDevirtualization && comparer == null)
                {
                    var hashCode = item != null ? item.GetHashCode() : 0;
                    if (typeof(T).IsValueType)
                    {
                        // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                        var i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                        while (i >= 0)
                        {
                            ref var entry = ref entries[i];
                            if (entry._hashCode == hashCode && EqualityComparer<T>.Default.Equals(entry._value, item))
                            {
                                return i;
                            }
                            i = entry._next;

                            collisionCount++;
                            if (collisionCount > (uint)entries.Length)
                            {
                                // The chain of entries forms a loop, which means a concurrent update has happened.
                                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                            }
                        }
                    }
                    else
                    {
                        // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize (https://github.com/dotnet/runtime/issues/10050),
                        // so cache in a local rather than get EqualityComparer per loop iteration.
                        var defaultComparer = EqualityComparer<T>.Default;
                        var i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                        while (i >= 0)
                        {
                            ref var entry = ref entries[i];
                            if (entry._hashCode == hashCode && defaultComparer.Equals(entry._value, item))
                            {
                                return i;
                            }
                            i = entry._next;

                            collisionCount++;
                            if (collisionCount > (uint)entries.Length)
                            {
                                // The chain of entries forms a loop, which means a concurrent update has happened.
                                ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                            }
                        }
                    }
                }
                else
                {
                    var hashCode = item != null ? comparer.GetHashCode(item) : 0;
                    var i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                    while (i >= 0)
                    {
                        ref var entry = ref entries[i];
                        if (entry._hashCode == hashCode && comparer.Equals(entry._value, item))
                        {
                            return i;
                        }
                        i = entry._next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop, which means a concurrent update has happened.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
            }

            return -1;
        }

        /// <summary>Gets a reference to the specified hashcode's bucket, containing an index into <see cref="_entries"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref int GetBucketRef(int hashCode)
        {
            var buckets = _buckets;
            return ref buckets[(int)HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
        }

        public bool Remove(T item)
        {
            if (_buckets.Length > 0)
            {
                var entries = _entries;
                Debug.Assert(entries.Length > 0, "entries should be non-empty");

                uint collisionCount = 0;
                var last = -1;
                var hashCode = item != null ? (_comparer?.GetHashCode(item) ?? item.GetHashCode()) : 0;

                ref var bucket = ref GetBucketRef(hashCode);
                var i = bucket - 1; // Value in buckets is 1-based

                while (i >= 0)
                {
                    ref var entry = ref entries[i];

                    if (entry._hashCode == hashCode && (_comparer?.Equals(entry._value, item) ?? EqualityComparer<T>.Default.Equals(entry._value, item)))
                    {
                        if (last < 0)
                        {
                            bucket = entry._next + 1; // Value in buckets is 1-based
                        }
                        else
                        {
                            entries[last]._next = entry._next;
                        }

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry._next = StartOfFreeList - _freeList;

#if NETCOREAPP
                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
#endif
                        {
                            entry._value = default!;
                        }

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry._next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop; which means a concurrent update has happened.
                        // Break out of the loop and throw, rather than looping forever.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            return false;
        }

        /// <summary>Gets the number of elements that are contained in the set.</summary>
        public int Count => _count - _freeCount;

        bool ICollection<T>.IsReadOnly => false;

        #endregion

        #region IEnumerable methods

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region SegmentedHashSet methods

        /// <summary>Adds the specified element to the <see cref="SegmentedHashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="SegmentedHashSet{T}"/> object; false if the element is already present.</returns>
        public bool Add(T item) => AddIfNotPresent(item, out _);

        /// <summary>Searches the set for a given value and returns the equal value it finds, if any.</summary>
        /// <param name="equalValue">The value to search for.</param>
        /// <param name="actualValue">The value from the set that the search found, or the default value of <typeparamref name="T"/> when the search yielded no match.</param>
        /// <returns>A value indicating whether the search was successful.</returns>
        /// <remarks>
        /// This can be useful when you want to reuse a previously stored reference instead of
        /// a newly constructed one (so that more sharing of references can occur) or to look up
        /// a value that has more complete data than the value you currently have, although their
        /// comparer functions indicate they are equal.
        /// </remarks>
        public bool TryGetValue(T equalValue, [MaybeNullWhen(false)] out T actualValue)
        {
            if (_buckets.Length > 0)
            {
                var index = FindItemIndex(equalValue);
                if (index >= 0)
                {
                    actualValue = _entries[index]._value;
                    return true;
                }
            }

            actualValue = default;
            return false;
        }

        /// <summary>Modifies the current <see cref="SegmentedHashSet{T}"/> object to contain all elements that are present in itself, the specified collection, or both.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            foreach (var item in other)
            {
                AddIfNotPresent(item, out _);
            }
        }

        /// <summary>Modifies the current <see cref="SegmentedHashSet{T}"/> object to contain only elements that are present in that object and in the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        public void IntersectWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // Intersection of anything with empty set is empty set, so return if count is 0.
            // Same if the set intersecting with itself is the same set.
            if (Count == 0 || other == this)
            {
                return;
            }

            // If other is known to be empty, intersection is empty set; remove all elements, and we're done.
            if (other is ICollection<T> otherAsCollection)
            {
                if (otherAsCollection.Count == 0)
                {
                    Clear();
                    return;
                }

                // Faster if other is a hashset using same equality comparer; so check
                // that other is a hashset using the same equality comparer.
                if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
                {
                    IntersectWithHashSetWithSameComparer(otherAsSet);
                    return;
                }
            }

            IntersectWithEnumerable(other);
        }

        /// <summary>Removes all elements in the specified collection from the current <see cref="SegmentedHashSet{T}"/> object.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        public void ExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // This is already the empty set; return.
            if (Count == 0)
            {
                return;
            }

            // Special case if other is this; a set minus itself is the empty set.
            if (other == this)
            {
                Clear();
                return;
            }

            // Remove every element in other from this.
            foreach (var element in other)
            {
                Remove(element);
            }
        }

        /// <summary>Modifies the current <see cref="SegmentedHashSet{T}"/> object to contain only elements that are present either in that object or in the specified collection, but not both.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // If set is empty, then symmetric difference is other.
            if (Count == 0)
            {
                UnionWith(other);
                return;
            }

            // Special-case this; the symmetric difference of a set with itself is the empty set.
            if (other == this)
            {
                Clear();
                return;
            }

            // If other is a SegmentedHashSet, it has unique elements according to its equality comparer,
            // but if they're using different equality comparers, then assumption of uniqueness
            // will fail. So first check if other is a hashset using the same equality comparer;
            // symmetric except is a lot faster and avoids bit array allocations if we can assume
            // uniqueness.
            if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
            {
                SymmetricExceptWithUniqueHashSet(otherAsSet);
            }
            else
            {
                SymmetricExceptWithEnumerable(other);
            }
        }

        /// <summary>Determines whether a <see cref="SegmentedHashSet{T}"/> object is a subset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object is a subset of <paramref name="other"/>; otherwise, false.</returns>
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // The empty set is a subset of any set, and a set is a subset of itself.
            // Set is always a subset of itself
            if (Count == 0 || other == this)
            {
                return true;
            }

            // Faster if other has unique elements according to this equality comparer; so check
            // that other is a hashset using the same equality comparer.
            if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
            {
                // if this has more elements then it can't be a subset
                if (Count > otherAsSet.Count)
                {
                    return false;
                }

                // already checked that we're using same equality comparer. simply check that
                // each element in this is contained in other.
                return IsSubsetOfHashSetWithSameComparer(otherAsSet);
            }

            (var uniqueCount, var unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount >= 0;
        }

        /// <summary>Determines whether a <see cref="SegmentedHashSet{T}"/> object is a proper subset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object is a proper subset of <paramref name="other"/>; otherwise, false.</returns>
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // No set is a proper subset of itself.
            if (other == this)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // No set is a proper subset of an empty set.
                if (otherAsCollection.Count == 0)
                {
                    return false;
                }

                // The empty set is a proper subset of anything but the empty set.
                if (Count == 0)
                {
                    return otherAsCollection.Count > 0;
                }

                // Faster if other is a hashset (and we're using same equality comparer).
                if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
                {
                    if (Count >= otherAsSet.Count)
                    {
                        return false;
                    }

                    // This has strictly less than number of items in other, so the following
                    // check suffices for proper subset.
                    return IsSubsetOfHashSetWithSameComparer(otherAsSet);
                }
            }

            (var uniqueCount, var unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        /// <summary>Determines whether a <see cref="SegmentedHashSet{T}"/> object is a proper superset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object is a superset of <paramref name="other"/>; otherwise, false.</returns>
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // A set is always a superset of itself.
            if (other == this)
            {
                return true;
            }

            // Try to fall out early based on counts.
            if (other is ICollection<T> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    return true;
                }

                // Try to compare based on counts alone if other is a hashset with same equality comparer.
                if (other is SegmentedHashSet<T> otherAsSet &&
                    EqualityComparersAreEqual(this, otherAsSet) &&
                    otherAsSet.Count > Count)
                {
                    return false;
                }
            }

            return ContainsAllElements(other);
        }

        /// <summary>Determines whether a <see cref="SegmentedHashSet{T}"/> object is a proper superset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object is a proper superset of <paramref name="other"/>; otherwise, false.</returns>
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // The empty set isn't a proper superset of any set, and a set is never a strict superset of itself.
            if (Count == 0 || other == this)
            {
                return false;
            }

            if (other is ICollection<T> otherAsCollection)
            {
                // If other is the empty set then this is a superset.
                if (otherAsCollection.Count == 0)
                {
                    // Note that this has at least one element, based on above check.
                    return true;
                }

                // Faster if other is a hashset with the same equality comparer
                if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
                {
                    if (otherAsSet.Count >= Count)
                    {
                        return false;
                    }

                    // Now perform element check.
                    return ContainsAllElements(otherAsSet);
                }
            }

            // Couldn't fall out in the above cases; do it the long way
            (var uniqueCount, var unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount < Count && unfoundCount == 0;
        }

        /// <summary>Determines whether the current <see cref="SegmentedHashSet{T}"/> object and a specified collection share common elements.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object and <paramref name="other"/> share at least one common element; otherwise, false.</returns>
        public bool Overlaps(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            if (Count == 0)
            {
                return false;
            }

            // Set overlaps itself
            if (other == this)
            {
                return true;
            }

            foreach (var element in other)
            {
                if (Contains(element))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Determines whether a <see cref="SegmentedHashSet{T}"/> object and the specified collection contain the same elements.</summary>
        /// <param name="other">The collection to compare to the current <see cref="SegmentedHashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="SegmentedHashSet{T}"/> object is equal to <paramref name="other"/>; otherwise, false.</returns>
        public bool SetEquals(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            // A set is equal to itself.
            if (other == this)
            {
                return true;
            }

            // Faster if other is a hashset and we're using same equality comparer.
            if (other is SegmentedHashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
            {
                // Attempt to return early: since both contain unique elements, if they have
                // different counts, then they can't be equal.
                if (Count != otherAsSet.Count)
                {
                    return false;
                }

                // Already confirmed that the sets have the same number of distinct elements, so if
                // one is a superset of the other then they must be equal.
                return ContainsAllElements(otherAsSet);
            }
            else
            {
                // If this count is 0 but other contains at least one element, they can't be equal.
                if (Count == 0 &&
                    other is ICollection<T> otherAsCollection &&
                    otherAsCollection.Count > 0)
                {
                    return false;
                }

                (var uniqueCount, var unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
                return uniqueCount == Count && unfoundCount == 0;
            }
        }

        public void CopyTo(T[] array) => CopyTo(array, 0, Count);

        /// <summary>Copies the elements of a <see cref="SegmentedHashSet{T}"/> object to an array, starting at the specified array index.</summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex) => CopyTo(array, arrayIndex, Count);

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (array == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.array);
            }

            // Check array index valid index into array.
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), arrayIndex, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            // Also throw if count less than 0.
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            // Will the array, starting at arrayIndex, be able to hold elements? Note: not
            // checking arrayIndex >= array.Length (consistency with list of allowing
            // count of 0; subsequent check takes care of the rest)
            if (arrayIndex > array.Length || count > array.Length - arrayIndex)
            {
                ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_ArrayPlusOffTooSmall);
            }

            var entries = _entries;
            for (var i = 0; i < _count && count != 0; i++)
            {
                ref var entry = ref entries[i];
                if (entry._next >= -1)
                {
                    array[arrayIndex++] = entry._value;
                    count--;
                }
            }
        }

        /// <summary>Removes all elements that match the conditions defined by the specified predicate from a <see cref="SegmentedHashSet{T}"/> collection.</summary>
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            var entries = _entries;
            var numRemoved = 0;
            for (var i = 0; i < _count; i++)
            {
                ref var entry = ref entries[i];
                if (entry._next >= -1)
                {
                    // Cache value in case delegate removes it
                    var value = entry._value;
                    if (match(value))
                    {
                        // Check again that remove actually removed it.
                        if (Remove(value))
                        {
                            numRemoved++;
                        }
                    }
                }
            }

            return numRemoved;
        }

        /// <summary>Gets the <see cref="IEqualityComparer"/> object that is used to determine equality for the values in the set.</summary>
        public IEqualityComparer<T> Comparer
        {
            get
            {
                return _comparer ?? EqualityComparer<T>.Default;
            }
        }

        /// <summary>Ensures that this hash set can hold the specified number of elements without growing.</summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            var currentCapacity = _entries.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            if (_buckets.Length == 0)
            {
                return Initialize(capacity);
            }

            var newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize);
            return newSize;
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count));

        private void Resize(int newSize)
        {
            Debug.Assert(_entries.Length > 0, "_entries should be non-empty");
            Debug.Assert(newSize >= _entries.Length);

            var entries = new SegmentedArray<Entry>(newSize);

            var count = _count;
            SegmentedArray.Copy(_entries, entries, count);

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _buckets = new SegmentedArray<int>(newSize);
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
            for (var i = 0; i < count; i++)
            {
                ref var entry = ref entries[i];
                if (entry._next >= -1)
                {
                    ref var bucket = ref GetBucketRef(entry._hashCode);
                    entry._next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entries = entries;
        }

        /// <summary>
        /// Sets the capacity of a <see cref="SegmentedHashSet{T}"/> object to the actual number of elements it contains,
        /// rounded up to a nearby, implementation-specific value.
        /// </summary>
        public void TrimExcess()
        {
            var capacity = Count;

            var newSize = HashHelpers.GetPrime(capacity);
            var oldEntries = _entries;
            var currentCapacity = oldEntries.Length;
            if (newSize >= currentCapacity)
            {
                return;
            }

            var oldCount = _count;
            _version++;
            Initialize(newSize);
            var entries = _entries;
            var count = 0;
            for (var i = 0; i < oldCount; i++)
            {
                var hashCode = oldEntries[i]._hashCode; // At this point, we know we have entries.
                if (oldEntries[i]._next >= -1)
                {
                    ref var entry = ref entries[count];
                    entry = oldEntries[i];
                    ref var bucket = ref GetBucketRef(hashCode);
                    entry._next = bucket - 1; // Value in _buckets is 1-based
                    bucket = count + 1;
                    count++;
                }
            }

            _count = capacity;
            _freeCount = 0;
        }

        #endregion

        #region Helper methods

        /// <summary>Returns an <see cref="IEqualityComparer"/> object that can be used for equality testing of a <see cref="SegmentedHashSet{T}"/> object.</summary>
        public static IEqualityComparer<SegmentedHashSet<T>> CreateSetComparer() => new SegmentedHashSetEqualityComparer<T>();

        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity.
        /// </summary>
        private int Initialize(int capacity)
        {
            var size = HashHelpers.GetPrime(capacity);
            var buckets = new SegmentedArray<int>(size);
            var entries = new SegmentedArray<Entry>(size);

            // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
            _freeList = -1;
            _buckets = buckets;
            _entries = entries;
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);

            return size;
        }

        /// <summary>Adds the specified element to the set if it's not already contained.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <param name="location">The index into <see cref="_entries"/> of the element.</param>
        /// <returns>true if the element is added to the <see cref="SegmentedHashSet{T}"/> object; false if the element is already present.</returns>
        private bool AddIfNotPresent(T value, out int location)
        {
            if (_buckets.Length == 0)
            {
                Initialize(0);
            }
            Debug.Assert(_buckets.Length > 0);

            var entries = _entries;
            Debug.Assert(entries.Length > 0, "expected entries to be non-empty");

            var comparer = _comparer;
            int hashCode;

            uint collisionCount = 0;
            ref var bucket = ref RoslynUnsafe.NullRef<int>();

            if (SupportsComparerDevirtualization && comparer == null)
            {
                hashCode = value != null ? value.GetHashCode() : 0;
                bucket = ref GetBucketRef(hashCode);
                var i = bucket - 1; // Value in _buckets is 1-based
                if (typeof(T).IsValueType)
                {
                    // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                    while (i >= 0)
                    {
                        ref var entry = ref entries[i];
                        if (entry._hashCode == hashCode && EqualityComparer<T>.Default.Equals(entry._value, value))
                        {
                            location = i;
                            return false;
                        }
                        i = entry._next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop, which means a concurrent update has happened.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
                else
                {
                    // Object type: Shared Generic, EqualityComparer<TValue>.Default won't devirtualize (https://github.com/dotnet/runtime/issues/10050),
                    // so cache in a local rather than get EqualityComparer per loop iteration.
                    var defaultComparer = EqualityComparer<T>.Default;
                    while (i >= 0)
                    {
                        ref var entry = ref entries[i];
                        if (entry._hashCode == hashCode && defaultComparer.Equals(entry._value, value))
                        {
                            location = i;
                            return false;
                        }
                        i = entry._next;

                        collisionCount++;
                        if (collisionCount > (uint)entries.Length)
                        {
                            // The chain of entries forms a loop, which means a concurrent update has happened.
                            ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                        }
                    }
                }
            }
            else
            {
                hashCode = value != null ? comparer.GetHashCode(value) : 0;
                bucket = ref GetBucketRef(hashCode);
                var i = bucket - 1; // Value in _buckets is 1-based
                while (i >= 0)
                {
                    ref var entry = ref entries[i];
                    if (entry._hashCode == hashCode && comparer.Equals(entry._value, value))
                    {
                        location = i;
                        return false;
                    }
                    i = entry._next;

                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        // The chain of entries forms a loop, which means a concurrent update has happened.
                        ThrowHelper.ThrowInvalidOperationException_ConcurrentOperationsNotSupported();
                    }
                }
            }

            int index;
            if (_freeCount > 0)
            {
                index = _freeList;
                _freeCount--;
                Debug.Assert((StartOfFreeList - entries[_freeList]._next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList]._next;
            }
            else
            {
                var count = _count;
                if (count == entries.Length)
                {
                    Resize();
                    bucket = ref GetBucketRef(hashCode);
                }
                index = count;
                _count = count + 1;
                entries = _entries;
            }

            {
                ref var entry = ref entries[index];
                entry._hashCode = hashCode;
                entry._next = bucket - 1; // Value in _buckets is 1-based
                entry._value = value;
                bucket = index + 1;
                _version++;
                location = index;
            }

            return true;
        }

        /// <summary>
        /// Checks if this contains of other's elements. Iterates over other's elements and
        /// returns false as soon as it finds an element in other that's not in this.
        /// Used by SupersetOf, ProperSupersetOf, and SetEquals.
        /// </summary>
        private bool ContainsAllElements(IEnumerable<T> other)
        {
            foreach (var element in other)
            {
                if (!Contains(element))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Implementation Notes:
        /// If other is a hashset and is using same equality comparer, then checking subset is
        /// faster. Simply check that each element in this is in other.
        ///
        /// Note: if other doesn't use same equality comparer, then Contains check is invalid,
        /// which is why callers must take are of this.
        ///
        /// If callers are concerned about whether this is a proper subset, they take care of that.
        /// </summary>
        internal bool IsSubsetOfHashSetWithSameComparer(SegmentedHashSet<T> other)
        {
            foreach (var item in this)
            {
                if (!other.Contains(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// If other is a hashset that uses same equality comparer, intersect is much faster
        /// because we can use other's Contains
        /// </summary>
        private void IntersectWithHashSetWithSameComparer(SegmentedHashSet<T> other)
        {
            var entries = _entries;
            for (var i = 0; i < _count; i++)
            {
                ref var entry = ref entries[i];
                if (entry._next >= -1)
                {
                    var item = entry._value;
                    if (!other.Contains(item))
                    {
                        Remove(item);
                    }
                }
            }
        }

        /// <summary>
        /// Iterate over other. If contained in this, mark an element in bit array corresponding to
        /// its position in _slots. If anything is unmarked (in bit array), remove it.
        ///
        /// This attempts to allocate on the stack, if below StackAllocThreshold.
        /// </summary>
        private unsafe void IntersectWithEnumerable(IEnumerable<T> other)
        {
            Debug.Assert(_buckets.Length > 0, "_buckets shouldn't be empty; callers should check first");

            // Keep track of current last index; don't want to move past the end of our bit array
            // (could happen if another thread is modifying the collection).
            var originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            var bitHelper = intArrayLength <= StackAllocThreshold
                ? new BitHelper(span.Slice(0, intArrayLength), clear: true)
                : new BitHelper(new int[intArrayLength], clear: false);

            // Mark if contains: find index of in slots array and mark corresponding element in bit array.
            foreach (var item in other)
            {
                var index = FindItemIndex(item);
                if (index >= 0)
                {
                    bitHelper.MarkBit(index);
                }
            }

            // If anything unmarked, remove it. Perf can be optimized here if BitHelper had a
            // FindFirstUnmarked method.
            for (var i = 0; i < originalCount; i++)
            {
                ref var entry = ref _entries[i];
                if (entry._next >= -1 && !bitHelper.IsMarked(i))
                {
                    Remove(entry._value);
                }
            }
        }

        /// <summary>
        /// if other is a set, we can assume it doesn't have duplicate elements, so use this
        /// technique: if can't remove, then it wasn't present in this set, so add.
        ///
        /// As with other methods, callers take care of ensuring that other is a hashset using the
        /// same equality comparer.
        /// </summary>
        /// <param name="other"></param>
        private void SymmetricExceptWithUniqueHashSet(SegmentedHashSet<T> other)
        {
            foreach (var item in other)
            {
                if (!Remove(item))
                {
                    AddIfNotPresent(item, out _);
                }
            }
        }

        /// <summary>
        /// Implementation notes:
        ///
        /// Used for symmetric except when other isn't a SegmentedHashSet. This is more tedious because
        /// other may contain duplicates. SegmentedHashSet technique could fail in these situations:
        /// 1. Other has a duplicate that's not in this: SegmentedHashSet technique would add then
        /// remove it.
        /// 2. Other has a duplicate that's in this: SegmentedHashSet technique would remove then add it
        /// back.
        /// In general, its presence would be toggled each time it appears in other.
        ///
        /// This technique uses bit marking to indicate whether to add/remove the item. If already
        /// present in collection, it will get marked for deletion. If added from other, it will
        /// get marked as something not to remove.
        ///
        /// </summary>
        /// <param name="other"></param>
        private unsafe void SymmetricExceptWithEnumerable(IEnumerable<T> other)
        {
            var originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> itemsToRemoveSpan = stackalloc int[StackAllocThreshold / 2];
            var itemsToRemove = intArrayLength <= StackAllocThreshold / 2
                ? new BitHelper(itemsToRemoveSpan.Slice(0, intArrayLength), clear: true)
                : new BitHelper(new int[intArrayLength], clear: false);

            Span<int> itemsAddedFromOtherSpan = stackalloc int[StackAllocThreshold / 2];
            var itemsAddedFromOther = intArrayLength <= StackAllocThreshold / 2
                ? new BitHelper(itemsAddedFromOtherSpan.Slice(0, intArrayLength), clear: true)
                : new BitHelper(new int[intArrayLength], clear: false);

            foreach (var item in other)
            {
                if (AddIfNotPresent(item, out var location))
                {
                    // wasn't already present in collection; flag it as something not to remove
                    // *NOTE* if location is out of range, we should ignore. BitHelper will
                    // detect that it's out of bounds and not try to mark it. But it's
                    // expected that location could be out of bounds because adding the item
                    // will increase _lastIndex as soon as all the free spots are filled.
                    itemsAddedFromOther.MarkBit(location);
                }
                else
                {
                    // already there...if not added from other, mark for remove.
                    // *NOTE* Even though BitHelper will check that location is in range, we want
                    // to check here. There's no point in checking items beyond originalCount
                    // because they could not have been in the original collection
                    if (location < originalCount && !itemsAddedFromOther.IsMarked(location))
                    {
                        itemsToRemove.MarkBit(location);
                    }
                }
            }

            // if anything marked, remove it
            for (var i = 0; i < originalCount; i++)
            {
                if (itemsToRemove.IsMarked(i))
                {
                    Remove(_entries[i]._value);
                }
            }
        }

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a SegmentedHashSet. If other is a SegmentedHashSet
        /// these properties can be checked faster without use of marking because we can assume
        /// other has no duplicates.
        ///
        /// The following count checks are performed by callers:
        /// 1. Equals: checks if unfoundCount = 0 and uniqueFoundCount = _count; i.e. everything
        /// in other is in this and everything in this is in other
        /// 2. Subset: checks if unfoundCount >= 0 and uniqueFoundCount = _count; i.e. other may
        /// have elements not in this and everything in this is in other
        /// 3. Proper subset: checks if unfoundCount > 0 and uniqueFoundCount = _count; i.e
        /// other must have at least one element not in this and everything in this is in other
        /// 4. Proper superset: checks if unfound count = 0 and uniqueFoundCount strictly less
        /// than _count; i.e. everything in other was in this and this had at least one element
        /// not contained in other.
        ///
        /// An earlier implementation used delegates to perform these checks rather than returning
        /// an ElementCount struct; however this was changed due to the perf overhead of delegates.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="returnIfUnfound">Allows us to finish faster for equals and proper superset
        /// because unfoundCount must be 0.</param>
        private unsafe (int UniqueCount, int UnfoundCount) CheckUniqueAndUnfoundElements(IEnumerable<T> other, bool returnIfUnfound)
        {
            // Need special case in case this has no elements.
            if (_count == 0)
            {
                var numElementsInOther = 0;
                foreach (var item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return (UniqueCount: 0, UnfoundCount: numElementsInOther);
            }

            Debug.Assert((_buckets.Length > 0) && (_count > 0), "_buckets was empty but count greater than 0");

            var originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            var bitHelper = intArrayLength <= StackAllocThreshold
                ? new BitHelper(span.Slice(0, intArrayLength), clear: true)
                : new BitHelper(new int[intArrayLength], clear: false);

            var unfoundCount = 0; // count of items in other not found in this
            var uniqueFoundCount = 0; // count of unique items in other found in this

            foreach (var item in other)
            {
                var index = FindItemIndex(item);
                if (index >= 0)
                {
                    if (!bitHelper.IsMarked(index))
                    {
                        // Item hasn't been seen yet.
                        bitHelper.MarkBit(index);
                        uniqueFoundCount++;
                    }
                }
                else
                {
                    unfoundCount++;
                    if (returnIfUnfound)
                    {
                        break;
                    }
                }
            }

            return (uniqueFoundCount, unfoundCount);
        }

        /// <summary>
        /// Checks if equality comparers are equal. This is used for algorithms that can
        /// speed up if it knows the other item has unique elements. I.e. if they're using
        /// different equality comparers, then uniqueness assumption between sets break.
        /// </summary>
        internal static bool EqualityComparersAreEqual(SegmentedHashSet<T> set1, SegmentedHashSet<T> set2) => set1.Comparer.Equals(set2.Comparer);

        #endregion

        private struct Entry
        {
            public int _hashCode;
            /// <summary>
            /// 0-based index of next entry in chain: -1 means end of chain
            /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int _next;
            public T _value;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly SegmentedHashSet<T> _hashSet;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(SegmentedHashSet<T> hashSet)
            {
                _hashSet = hashSet;
                _version = hashSet._version;
                _index = 0;
                _current = default!;
            }

            public bool MoveNext()
            {
                if (_version != _hashSet._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                // Use unsigned comparison since we set index to dictionary.count+1 when the enumeration ends.
                // dictionary.count+1 could be negative if dictionary.count is int.MaxValue
                while ((uint)_index < (uint)_hashSet._count)
                {
                    ref var entry = ref _hashSet._entries[_index++];
                    if (entry._next >= -1)
                    {
                        _current = entry._value;
                        return true;
                    }
                }

                _index = _hashSet._count + 1;
                _current = default!;
                return false;
            }

            public readonly T Current => _current;

            public readonly void Dispose() { }

            readonly object? IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || (_index == _hashSet._count + 1))
                    {
                        ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumOpCantHappen();
                    }

                    return _current;
                }
            }

            void IEnumerator.Reset()
            {
                if (_version != _hashSet._version)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }

                _index = 0;
                _current = default!;
            }
        }
    }
}
