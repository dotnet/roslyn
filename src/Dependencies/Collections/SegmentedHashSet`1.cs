// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

using Internal.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    [DebuggerTypeProxy(typeof(ICollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    [TypeForwardedFrom("System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class HashSet<T> : ICollection<T>, ISet<T>, IReadOnlyCollection<T>, IReadOnlySet<T>, ISerializable, IDeserializationCallback
    {
        // This uses the same array-based implementation as Dictionary<TKey, TValue>.

        // Constants for serialization
        private const string CapacityName = "Capacity"; // Do not rename (binary serialization)
        private const string ElementsName = "Elements"; // Do not rename (binary serialization)
        private const string ComparerName = "Comparer"; // Do not rename (binary serialization)
        private const string VersionName = "Version"; // Do not rename (binary serialization)

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

        private int[]? _buckets;
        private Entry[]? _entries;
#if TARGET_64BIT
        private ulong _fastModMultiplier;
#endif
        private int _count;
        private int _freeList;
        private int _freeCount;
        private int _version;
        private IEqualityComparer<T>? _comparer;
        private SerializationInfo? _siInfo; // temporary variable needed during deserialization

        #region Constructors

        public HashSet() : this((IEqualityComparer<T>?)null) { }

        public HashSet(IEqualityComparer<T>? comparer)
        {
            if (comparer != null && comparer != EqualityComparer<T>.Default) // first check for null to avoid forcing default comparer instantiation unnecessarily
            {
                _comparer = comparer;
            }

            // Special-case EqualityComparer<string>.Default, StringComparer.Ordinal, and StringComparer.OrdinalIgnoreCase.
            // We use a non-randomized comparer for improved perf, falling back to a randomized comparer if the
            // hash buckets become unbalanced.

            if (typeof(T) == typeof(string))
            {
                if (_comparer is null)
                {
                    _comparer = (IEqualityComparer<T>)NonRandomizedStringEqualityComparer.WrappedAroundDefaultComparer;
                }
                else if (ReferenceEquals(_comparer, StringComparer.Ordinal))
                {
                    _comparer = (IEqualityComparer<T>)NonRandomizedStringEqualityComparer.WrappedAroundStringComparerOrdinal;
                }
                else if (ReferenceEquals(_comparer, StringComparer.OrdinalIgnoreCase))
                {
                    _comparer = (IEqualityComparer<T>)NonRandomizedStringEqualityComparer.WrappedAroundStringComparerOrdinalIgnoreCase;
                }
            }
        }

        public HashSet(int capacity) : this(capacity, null) { }

        public HashSet(IEnumerable<T> collection) : this(collection, null) { }

        public HashSet(IEnumerable<T> collection, IEqualityComparer<T>? comparer) : this(comparer)
        {
            if (collection == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.collection);
            }

            if (collection is HashSet<T> otherAsHashSet && EqualityComparersAreEqual(this, otherAsHashSet))
            {
                ConstructFrom(otherAsHashSet);
            }
            else
            {
                // To avoid excess resizes, first set size based on collection's count. The collection may
                // contain duplicates, so call TrimExcess if resulting HashSet is larger than the threshold.
                if (collection is ICollection<T> coll)
                {
                    int count = coll.Count;
                    if (count > 0)
                    {
                        Initialize(count);
                    }
                }

                UnionWith(collection);

                if (_count > 0 && _entries!.Length / _count > ShrinkThreshold)
                {
                    TrimExcess();
                }
            }
        }

        public HashSet(int capacity, IEqualityComparer<T>? comparer) : this(comparer)
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

        protected HashSet(SerializationInfo info, StreamingContext context)
        {
            // We can't do anything with the keys and values until the entire graph has been
            // deserialized and we have a reasonable estimate that GetHashCode is not going to
            // fail.  For the time being, we'll just cache this.  The graph is not valid until
            // OnDeserialization has been called.
            _siInfo = info;
        }

        /// <summary>Initializes the HashSet from another HashSet with the same element type and equality comparer.</summary>
        private void ConstructFrom(HashSet<T> source)
        {
            if (source.Count == 0)
            {
                // As well as short-circuiting on the rest of the work done,
                // this avoids errors from trying to access source._buckets
                // or source._entries when they aren't initialized.
                return;
            }

            int capacity = source._buckets!.Length;
            int threshold = HashHelpers.ExpandPrime(source.Count + 1);

            if (threshold >= capacity)
            {
                _buckets = (int[])source._buckets.Clone();
                _entries = (Entry[])source._entries!.Clone();
                _freeList = source._freeList;
                _freeCount = source._freeCount;
                _count = source._count;
#if TARGET_64BIT
                _fastModMultiplier = source._fastModMultiplier;
#endif
            }
            else
            {
                Initialize(source.Count);

                Entry[]? entries = source._entries;
                for (int i = 0; i < source._count; i++)
                {
                    ref Entry entry = ref entries![i];
                    if (entry.Next >= -1)
                    {
                        AddIfNotPresent(entry.Value, out _);
                    }
                }
            }

            Debug.Assert(Count == source.Count);
        }

        #endregion

        #region ICollection<T> methods

        void ICollection<T>.Add(T item) => AddIfNotPresent(item, out _);

        /// <summary>Removes all elements from the <see cref="HashSet{T}"/> object.</summary>
        public void Clear()
        {
            int count = _count;
            if (count > 0)
            {
                Debug.Assert(_buckets != null, "_buckets should be non-null");
                Debug.Assert(_entries != null, "_entries should be non-null");

                Array.Clear(_buckets, 0, _buckets.Length);
                _count = 0;
                _freeList = -1;
                _freeCount = 0;
                Array.Clear(_entries, 0, count);
            }
        }

        /// <summary>Determines whether the <see cref="HashSet{T}"/> contains the specified element.</summary>
        /// <param name="item">The element to locate in the <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object contains the specified element; otherwise, false.</returns>
        public bool Contains(T item) => FindItemIndex(item) >= 0;

        /// <summary>Gets the index of the item in <see cref="_entries"/>, or -1 if it's not in the set.</summary>
        private int FindItemIndex(T item)
        {
            int[]? buckets = _buckets;
            if (buckets != null)
            {
                Entry[]? entries = _entries;
                Debug.Assert(entries != null, "Expected _entries to be initialized");

                uint collisionCount = 0;
                IEqualityComparer<T>? comparer = _comparer;

                if (comparer == null)
                {
                    int hashCode = item != null ? item.GetHashCode() : 0;
                    if (typeof(T).IsValueType)
                    {
                        // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                        int i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                        while (i >= 0)
                        {
                            ref Entry entry = ref entries[i];
                            if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, item))
                            {
                                return i;
                            }
                            i = entry.Next;

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
                        EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;
                        int i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                        while (i >= 0)
                        {
                            ref Entry entry = ref entries[i];
                            if (entry.HashCode == hashCode && defaultComparer.Equals(entry.Value, item))
                            {
                                return i;
                            }
                            i = entry.Next;

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
                    int hashCode = item != null ? comparer.GetHashCode(item) : 0;
                    int i = GetBucketRef(hashCode) - 1; // Value in _buckets is 1-based
                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];
                        if (entry.HashCode == hashCode && comparer.Equals(entry.Value, item))
                        {
                            return i;
                        }
                        i = entry.Next;

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
            int[] buckets = _buckets!;
#if TARGET_64BIT
            return ref buckets[HashHelpers.FastMod((uint)hashCode, (uint)buckets.Length, _fastModMultiplier)];
#else
            return ref buckets[(uint)hashCode % (uint)buckets.Length];
#endif
        }

        public bool Remove(T item)
        {
            if (_buckets != null)
            {
                Entry[]? entries = _entries;
                Debug.Assert(entries != null, "entries should be non-null");

                uint collisionCount = 0;
                int last = -1;
                int hashCode = item != null ? (_comparer?.GetHashCode(item) ?? item.GetHashCode()) : 0;

                ref int bucket = ref GetBucketRef(hashCode);
                int i = bucket - 1; // Value in buckets is 1-based

                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];

                    if (entry.HashCode == hashCode && (_comparer?.Equals(entry.Value, item) ?? EqualityComparer<T>.Default.Equals(entry.Value, item)))
                    {
                        if (last < 0)
                        {
                            bucket = entry.Next + 1; // Value in buckets is 1-based
                        }
                        else
                        {
                            entries[last].Next = entry.Next;
                        }

                        Debug.Assert((StartOfFreeList - _freeList) < 0, "shouldn't underflow because max hashtable length is MaxPrimeArrayLength = 0x7FEFFFFD(2146435069) _freelist underflow threshold 2147483646");
                        entry.Next = StartOfFreeList - _freeList;

                        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
                        {
                            entry.Value = default!;
                        }

                        _freeList = i;
                        _freeCount++;
                        return true;
                    }

                    last = i;
                    i = entry.Next;

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

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region ISerializable methods

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.info);
            }

            info.AddValue(VersionName, _version); // need to serialize version to avoid problems with serializing while enumerating
            info.AddValue(ComparerName, Comparer, typeof(IEqualityComparer<T>));
            info.AddValue(CapacityName, _buckets == null ? 0 : _buckets.Length);

            if (_buckets != null)
            {
                var array = new T[Count];
                CopyTo(array);
                info.AddValue(ElementsName, array, typeof(T[]));
            }
        }

        #endregion

        #region IDeserializationCallback methods

        public virtual void OnDeserialization(object? sender)
        {
            if (_siInfo == null)
            {
                // It might be necessary to call OnDeserialization from a container if the
                // container object also implements OnDeserialization. We can return immediately
                // if this function is called twice. Note we set _siInfo to null at the end of this method.
                return;
            }

            int capacity = _siInfo.GetInt32(CapacityName);
            _comparer = (IEqualityComparer<T>)_siInfo.GetValue(ComparerName, typeof(IEqualityComparer<T>))!;
            _freeList = -1;
            _freeCount = 0;

            if (capacity != 0)
            {
                _buckets = new int[capacity];
                _entries = new Entry[capacity];
#if TARGET_64BIT
                _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)capacity);
#endif

                T[]? array = (T[]?)_siInfo.GetValue(ElementsName, typeof(T[]));
                if (array == null)
                {
                    ThrowHelper.ThrowSerializationException(ExceptionResource.Serialization_MissingKeys);
                }

                // There are no resizes here because we already set capacity above.
                for (int i = 0; i < array.Length; i++)
                {
                    AddIfNotPresent(array[i], out _);
                }
            }
            else
            {
                _buckets = null;
            }

            _version = _siInfo.GetInt32(VersionName);
            _siInfo = null;
        }

        #endregion

        #region HashSet methods

        /// <summary>Adds the specified element to the <see cref="HashSet{T}"/>.</summary>
        /// <param name="item">The element to add to the set.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
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
            if (_buckets != null)
            {
                int index = FindItemIndex(equalValue);
                if (index >= 0)
                {
                    actualValue = _entries![index].Value;
                    return true;
                }
            }

            actualValue = default;
            return false;
        }

        /// <summary>Modifies the current <see cref="HashSet{T}"/> object to contain all elements that are present in itself, the specified collection, or both.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        public void UnionWith(IEnumerable<T> other)
        {
            if (other == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.other);
            }

            foreach (T item in other)
            {
                AddIfNotPresent(item, out _);
            }
        }

        /// <summary>Modifies the current <see cref="HashSet{T}"/> object to contain only elements that are present in that object and in the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
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
                if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
                {
                    IntersectWithHashSetWithSameComparer(otherAsSet);
                    return;
                }
            }

            IntersectWithEnumerable(other);
        }

        /// <summary>Removes all elements in the specified collection from the current <see cref="HashSet{T}"/> object.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
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
            foreach (T element in other)
            {
                Remove(element);
            }
        }

        /// <summary>Modifies the current <see cref="HashSet{T}"/> object to contain only elements that are present either in that object or in the specified collection, but not both.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
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

            // If other is a HashSet, it has unique elements according to its equality comparer,
            // but if they're using different equality comparers, then assumption of uniqueness
            // will fail. So first check if other is a hashset using the same equality comparer;
            // symmetric except is a lot faster and avoids bit array allocations if we can assume
            // uniqueness.
            if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
            {
                SymmetricExceptWithUniqueHashSet(otherAsSet);
            }
            else
            {
                SymmetricExceptWithEnumerable(other);
            }
        }

        /// <summary>Determines whether a <see cref="HashSet{T}"/> object is a subset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object is a subset of <paramref name="other"/>; otherwise, false.</returns>
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
            if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
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

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount >= 0;
        }

        /// <summary>Determines whether a <see cref="HashSet{T}"/> object is a proper subset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object is a proper subset of <paramref name="other"/>; otherwise, false.</returns>
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
                if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
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

            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: false);
            return uniqueCount == Count && unfoundCount > 0;
        }

        /// <summary>Determines whether a <see cref="HashSet{T}"/> object is a proper superset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object is a superset of <paramref name="other"/>; otherwise, false.</returns>
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
                if (other is HashSet<T> otherAsSet &&
                    EqualityComparersAreEqual(this, otherAsSet) &&
                    otherAsSet.Count > Count)
                {
                    return false;
                }
            }

            return ContainsAllElements(other);
        }

        /// <summary>Determines whether a <see cref="HashSet{T}"/> object is a proper superset of the specified collection.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object is a proper superset of <paramref name="other"/>; otherwise, false.</returns>
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
                if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
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
            (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
            return uniqueCount < Count && unfoundCount == 0;
        }

        /// <summary>Determines whether the current <see cref="HashSet{T}"/> object and a specified collection share common elements.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object and <paramref name="other"/> share at least one common element; otherwise, false.</returns>
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

            foreach (T element in other)
            {
                if (Contains(element))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Determines whether a <see cref="HashSet{T}"/> object and the specified collection contain the same elements.</summary>
        /// <param name="other">The collection to compare to the current <see cref="HashSet{T}"/> object.</param>
        /// <returns>true if the <see cref="HashSet{T}"/> object is equal to <paramref name="other"/>; otherwise, false.</returns>
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
            if (other is HashSet<T> otherAsSet && EqualityComparersAreEqual(this, otherAsSet))
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

                (int uniqueCount, int unfoundCount) = CheckUniqueAndUnfoundElements(other, returnIfUnfound: true);
                return uniqueCount == Count && unfoundCount == 0;
            }
        }

        public void CopyTo(T[] array) => CopyTo(array, 0, Count);

        /// <summary>Copies the elements of a <see cref="HashSet{T}"/> object to an array, starting at the specified array index.</summary>
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

            Entry[]? entries = _entries;
            for (int i = 0; i < _count && count != 0; i++)
            {
                ref Entry entry = ref entries![i];
                if (entry.Next >= -1)
                {
                    array[arrayIndex++] = entry.Value;
                    count--;
                }
            }
        }

        /// <summary>Removes all elements that match the conditions defined by the specified predicate from a <see cref="HashSet{T}"/> collection.</summary>
        public int RemoveWhere(Predicate<T> match)
        {
            if (match == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.match);
            }

            Entry[]? entries = _entries;
            int numRemoved = 0;
            for (int i = 0; i < _count; i++)
            {
                ref Entry entry = ref entries![i];
                if (entry.Next >= -1)
                {
                    // Cache value in case delegate removes it
                    T value = entry.Value;
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
                if (typeof(T) == typeof(string))
                {
                    return (IEqualityComparer<T>)IInternalStringEqualityComparer.GetUnderlyingEqualityComparer((IEqualityComparer<string?>?)_comparer);
                }
                else
                {
                    return _comparer ?? EqualityComparer<T>.Default;
                }
            }
        }

        /// <summary>Ensures that this hash set can hold the specified number of elements without growing.</summary>
        public int EnsureCapacity(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.capacity);
            }

            int currentCapacity = _entries == null ? 0 : _entries.Length;
            if (currentCapacity >= capacity)
            {
                return currentCapacity;
            }

            if (_buckets == null)
            {
                return Initialize(capacity);
            }

            int newSize = HashHelpers.GetPrime(capacity);
            Resize(newSize, forceNewHashCodes: false);
            return newSize;
        }

        private void Resize() => Resize(HashHelpers.ExpandPrime(_count), forceNewHashCodes: false);

        private void Resize(int newSize, bool forceNewHashCodes)
        {
            // Value types never rehash
            Debug.Assert(!forceNewHashCodes || !typeof(T).IsValueType);
            Debug.Assert(_entries != null, "_entries should be non-null");
            Debug.Assert(newSize >= _entries.Length);

            var entries = new Entry[newSize];

            int count = _count;
            Array.Copy(_entries, entries, count);

            if (!typeof(T).IsValueType && forceNewHashCodes)
            {
                Debug.Assert(_comparer is NonRandomizedStringEqualityComparer);
                _comparer = (IEqualityComparer<T>)((NonRandomizedStringEqualityComparer)_comparer).GetRandomizedEqualityComparer();

                for (int i = 0; i < count; i++)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.Next >= -1)
                    {
                        entry.HashCode = entry.Value != null ? _comparer!.GetHashCode(entry.Value) : 0;
                    }
                }

                if (ReferenceEquals(_comparer, EqualityComparer<T>.Default))
                {
                    _comparer = null;
                }
            }

            // Assign member variables after both arrays allocated to guard against corruption from OOM if second fails
            _buckets = new int[newSize];
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)newSize);
#endif
            for (int i = 0; i < count; i++)
            {
                ref Entry entry = ref entries[i];
                if (entry.Next >= -1)
                {
                    ref int bucket = ref GetBucketRef(entry.HashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = i + 1;
                }
            }

            _entries = entries;
        }

        /// <summary>
        /// Sets the capacity of a <see cref="HashSet{T}"/> object to the actual number of elements it contains,
        /// rounded up to a nearby, implementation-specific value.
        /// </summary>
        public void TrimExcess()
        {
            int capacity = Count;

            int newSize = HashHelpers.GetPrime(capacity);
            Entry[]? oldEntries = _entries;
            int currentCapacity = oldEntries == null ? 0 : oldEntries.Length;
            if (newSize >= currentCapacity)
            {
                return;
            }

            int oldCount = _count;
            _version++;
            Initialize(newSize);
            Entry[]? entries = _entries;
            int count = 0;
            for (int i = 0; i < oldCount; i++)
            {
                int hashCode = oldEntries![i].HashCode; // At this point, we know we have entries.
                if (oldEntries[i].Next >= -1)
                {
                    ref Entry entry = ref entries![count];
                    entry = oldEntries[i];
                    ref int bucket = ref GetBucketRef(hashCode);
                    entry.Next = bucket - 1; // Value in _buckets is 1-based
                    bucket = count + 1;
                    count++;
                }
            }

            _count = capacity;
            _freeCount = 0;
        }

        #endregion

        #region Helper methods

        /// <summary>Returns an <see cref="IEqualityComparer"/> object that can be used for equality testing of a <see cref="HashSet{T}"/> object.</summary>
        public static IEqualityComparer<HashSet<T>> CreateSetComparer() => new HashSetEqualityComparer<T>();

        /// <summary>
        /// Initializes buckets and slots arrays. Uses suggested capacity by finding next prime
        /// greater than or equal to capacity.
        /// </summary>
        private int Initialize(int capacity)
        {
            int size = HashHelpers.GetPrime(capacity);
            var buckets = new int[size];
            var entries = new Entry[size];

            // Assign member variables after both arrays are allocated to guard against corruption from OOM if second fails.
            _freeList = -1;
            _buckets = buckets;
            _entries = entries;
#if TARGET_64BIT
            _fastModMultiplier = HashHelpers.GetFastModMultiplier((uint)size);
#endif

            return size;
        }

        /// <summary>Adds the specified element to the set if it's not already contained.</summary>
        /// <param name="value">The element to add to the set.</param>
        /// <param name="location">The index into <see cref="_entries"/> of the element.</param>
        /// <returns>true if the element is added to the <see cref="HashSet{T}"/> object; false if the element is already present.</returns>
        private bool AddIfNotPresent(T value, out int location)
        {
            if (_buckets == null)
            {
                Initialize(0);
            }
            Debug.Assert(_buckets != null);

            Entry[]? entries = _entries;
            Debug.Assert(entries != null, "expected entries to be non-null");

            IEqualityComparer<T>? comparer = _comparer;
            int hashCode;

            uint collisionCount = 0;
            ref int bucket = ref Unsafe.NullRef<int>();

            if (comparer == null)
            {
                hashCode = value != null ? value.GetHashCode() : 0;
                bucket = ref GetBucketRef(hashCode);
                int i = bucket - 1; // Value in _buckets is 1-based
                if (typeof(T).IsValueType)
                {
                    // ValueType: Devirtualize with EqualityComparer<TValue>.Default intrinsic
                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];
                        if (entry.HashCode == hashCode && EqualityComparer<T>.Default.Equals(entry.Value, value))
                        {
                            location = i;
                            return false;
                        }
                        i = entry.Next;

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
                    EqualityComparer<T> defaultComparer = EqualityComparer<T>.Default;
                    while (i >= 0)
                    {
                        ref Entry entry = ref entries[i];
                        if (entry.HashCode == hashCode && defaultComparer.Equals(entry.Value, value))
                        {
                            location = i;
                            return false;
                        }
                        i = entry.Next;

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
                int i = bucket - 1; // Value in _buckets is 1-based
                while (i >= 0)
                {
                    ref Entry entry = ref entries[i];
                    if (entry.HashCode == hashCode && comparer.Equals(entry.Value, value))
                    {
                        location = i;
                        return false;
                    }
                    i = entry.Next;

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
                Debug.Assert((StartOfFreeList - entries![_freeList].Next) >= -1, "shouldn't overflow because `next` cannot underflow");
                _freeList = StartOfFreeList - entries[_freeList].Next;
            }
            else
            {
                int count = _count;
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
                ref Entry entry = ref entries![index];
                entry.HashCode = hashCode;
                entry.Next = bucket - 1; // Value in _buckets is 1-based
                entry.Value = value;
                bucket = index + 1;
                _version++;
                location = index;
            }

            // Value types never rehash
            if (!typeof(T).IsValueType && collisionCount > HashHelpers.HashCollisionThreshold && comparer is NonRandomizedStringEqualityComparer)
            {
                // If we hit the collision threshold we'll need to switch to the comparer which is using randomized string hashing
                // i.e. EqualityComparer<string>.Default.
                Resize(entries.Length, forceNewHashCodes: true);
                location = FindItemIndex(value);
                Debug.Assert(location >= 0);
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
            foreach (T element in other)
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
        internal bool IsSubsetOfHashSetWithSameComparer(HashSet<T> other)
        {
            foreach (T item in this)
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
        private void IntersectWithHashSetWithSameComparer(HashSet<T> other)
        {
            Entry[]? entries = _entries;
            for (int i = 0; i < _count; i++)
            {
                ref Entry entry = ref entries![i];
                if (entry.Next >= -1)
                {
                    T item = entry.Value;
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
            Debug.Assert(_buckets != null, "_buckets shouldn't be null; callers should check first");

            // Keep track of current last index; don't want to move past the end of our bit array
            // (could happen if another thread is modifying the collection).
            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            BitHelper bitHelper = intArrayLength <= StackAllocThreshold ?
                new BitHelper(span.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            // Mark if contains: find index of in slots array and mark corresponding element in bit array.
            foreach (T item in other)
            {
                int index = FindItemIndex(item);
                if (index >= 0)
                {
                    bitHelper.MarkBit(index);
                }
            }

            // If anything unmarked, remove it. Perf can be optimized here if BitHelper had a
            // FindFirstUnmarked method.
            for (int i = 0; i < originalCount; i++)
            {
                ref Entry entry = ref _entries![i];
                if (entry.Next >= -1 && !bitHelper.IsMarked(i))
                {
                    Remove(entry.Value);
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
        private void SymmetricExceptWithUniqueHashSet(HashSet<T> other)
        {
            foreach (T item in other)
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
        /// Used for symmetric except when other isn't a HashSet. This is more tedious because
        /// other may contain duplicates. HashSet technique could fail in these situations:
        /// 1. Other has a duplicate that's not in this: HashSet technique would add then
        /// remove it.
        /// 2. Other has a duplicate that's in this: HashSet technique would remove then add it
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
            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> itemsToRemoveSpan = stackalloc int[StackAllocThreshold / 2];
            BitHelper itemsToRemove = intArrayLength <= StackAllocThreshold / 2 ?
                new BitHelper(itemsToRemoveSpan.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            Span<int> itemsAddedFromOtherSpan = stackalloc int[StackAllocThreshold / 2];
            BitHelper itemsAddedFromOther = intArrayLength <= StackAllocThreshold / 2 ?
                new BitHelper(itemsAddedFromOtherSpan.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            foreach (T item in other)
            {
                int location;
                if (AddIfNotPresent(item, out location))
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
            for (int i = 0; i < originalCount; i++)
            {
                if (itemsToRemove.IsMarked(i))
                {
                    Remove(_entries![i].Value);
                }
            }
        }

        /// <summary>
        /// Determines counts that can be used to determine equality, subset, and superset. This
        /// is only used when other is an IEnumerable and not a HashSet. If other is a HashSet
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
                int numElementsInOther = 0;
                foreach (T item in other)
                {
                    numElementsInOther++;
                    break; // break right away, all we want to know is whether other has 0 or 1 elements
                }

                return (UniqueCount: 0, UnfoundCount: numElementsInOther);
            }

            Debug.Assert((_buckets != null) && (_count > 0), "_buckets was null but count greater than 0");

            int originalCount = _count;
            int intArrayLength = BitHelper.ToIntArrayLength(originalCount);

            Span<int> span = stackalloc int[StackAllocThreshold];
            BitHelper bitHelper = intArrayLength <= StackAllocThreshold ?
                new BitHelper(span.Slice(0, intArrayLength), clear: true) :
                new BitHelper(new int[intArrayLength], clear: false);

            int unfoundCount = 0; // count of items in other not found in this
            int uniqueFoundCount = 0; // count of unique items in other found in this

            foreach (T item in other)
            {
                int index = FindItemIndex(item);
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
        internal static bool EqualityComparersAreEqual(HashSet<T> set1, HashSet<T> set2) => set1.Comparer.Equals(set2.Comparer);

#endregion

        private struct Entry
        {
            public int HashCode;
            /// <summary>
            /// 0-based index of next entry in chain: -1 means end of chain
            /// also encodes whether this entry _itself_ is part of the free list by changing sign and subtracting 3,
            /// so -2 means end of free list, -3 means index 0 but on free list, -4 means index 1 but on free list, etc.
            /// </summary>
            public int Next;
            public T Value;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly HashSet<T> _hashSet;
            private readonly int _version;
            private int _index;
            private T _current;

            internal Enumerator(HashSet<T> hashSet)
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
                    ref Entry entry = ref _hashSet._entries![_index++];
                    if (entry.Next >= -1)
                    {
                        _current = entry.Value;
                        return true;
                    }
                }

                _index = _hashSet._count + 1;
                _current = default!;
                return false;
            }

            public T Current => _current;

            public void Dispose() { }

            object? IEnumerator.Current
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
