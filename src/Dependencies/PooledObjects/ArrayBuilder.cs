// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.PooledObjects
{
    [DebuggerDisplay("Count = {Count,nq}")]
    [DebuggerTypeProxy(typeof(ArrayBuilder<>.DebuggerProxy))]
    internal sealed partial class ArrayBuilder<T> : IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>
#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER
        , IPooled
#endif
    {
        /// <summary>
        /// See <see cref="Free()"/> for an explanation of this constant value.
        /// </summary>
        public const int PooledArrayLengthLimitExclusive = 128;

        #region DebuggerProxy

        private sealed class DebuggerProxy
        {
            private readonly ArrayBuilder<T> _builder;

            public DebuggerProxy(ArrayBuilder<T> builder)
            {
                _builder = builder;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] A
            {
                get
                {
                    var result = new T[_builder.Count];
                    for (var i = 0; i < result.Length; i++)
                    {
                        result[i] = _builder[i];
                    }

                    return result;
                }
            }
        }

        #endregion

        private readonly ImmutableArray<T>.Builder _builder;

        private readonly ObjectPool<ArrayBuilder<T>>? _pool;

        public ArrayBuilder(int size)
        {
            _builder = ImmutableArray.CreateBuilder<T>(size);
        }

        public ArrayBuilder()
            : this(8)
        { }

        private ArrayBuilder(ObjectPool<ArrayBuilder<T>> pool)
            : this()
        {
            _pool = pool;
        }

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutable()
        {
            return _builder.ToImmutable();
        }

        /// <summary>
        /// Realizes the array and clears the collection.
        /// </summary>
        public ImmutableArray<T> ToImmutableAndClear()
        {
            ImmutableArray<T> result;
            if (Count == 0)
            {
                result = ImmutableArray<T>.Empty;
            }
            else if (_builder.Capacity == Count)
            {
                result = _builder.MoveToImmutable();
            }
            else
            {
                result = ToImmutable();
                Clear();
            }

            return result;
        }

        public int Count
        {
            get
            {
                return _builder.Count;
            }
            set
            {
                _builder.Count = value;
            }
        }

        public int Capacity
        {
            get
            {
                return _builder.Capacity;
            }

            set
            {
                _builder.Capacity = value;
            }
        }

        public T this[int index]
        {
            get
            {
                return _builder[index];
            }

            set
            {
                _builder[index] = value;
            }
        }

        public bool IsReadOnly
            => false;

        public bool IsEmpty
            => Count == 0;

        /// <summary>
        /// Write <paramref name="value"/> to slot <paramref name="index"/>. 
        /// Fills in unallocated slots preceding the <paramref name="index"/>, if any.
        /// </summary>
        public void SetItem(int index, T value)
        {
            while (index > _builder.Count)
            {
                _builder.Add(default!);
            }

            if (index == _builder.Count)
            {
                _builder.Add(value);
            }
            else
            {
                _builder[index] = value;
            }
        }

        public void Add(T item)
        {
            _builder.Add(item);
        }

        public void Insert(int index, T item)
        {
            _builder.Insert(index, item);
        }

        public void EnsureCapacity(int capacity)
        {
            if (_builder.Capacity < capacity)
            {
                _builder.Capacity = capacity;
            }
        }

        public void Clear()
        {
            _builder.Clear();
        }

        public bool Contains(T item)
        {
            return _builder.Contains(item);
        }

        public int IndexOf(T item)
        {
            return _builder.IndexOf(item);
        }

        public int IndexOf(T item, IEqualityComparer<T> equalityComparer)
        {
            return _builder.IndexOf(item, 0, _builder.Count, equalityComparer);
        }

        public int IndexOf(T item, int startIndex, int count)
        {
            return _builder.IndexOf(item, startIndex, count);
        }

        public int FindIndex(Predicate<T> match)
            => FindIndex(0, this.Count, match);

        public int FindIndex(int startIndex, Predicate<T> match)
            => FindIndex(startIndex, this.Count - startIndex, match);

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(_builder[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindIndex<TArg>(Func<T, TArg, bool> match, TArg arg)
            => FindIndex(0, Count, match, arg);

        public int FindIndex<TArg>(int startIndex, Func<T, TArg, bool> match, TArg arg)
            => FindIndex(startIndex, Count - startIndex, match, arg);

        public int FindIndex<TArg>(int startIndex, int count, Func<T, TArg, bool> match, TArg arg)
        {
            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
            {
                if (match(_builder[i], arg))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Remove(T element)
        {
            return _builder.Remove(element);
        }

        public void RemoveAt(int index)
        {
            _builder.RemoveAt(index);
        }

        public void RemoveRange(int index, int length)
        {
            _builder.RemoveRange(index, length);
        }

        public void RemoveLast()
        {
            _builder.RemoveAt(_builder.Count - 1);
        }

        public void RemoveAll(Predicate<T> match)
        {
            var i = 0;
            for (var j = 0; j < _builder.Count; j++)
            {
                if (!match(_builder[j]))
                {
                    if (i != j)
                    {
                        _builder[i] = _builder[j];
                    }

                    i++;
                }
            }

            Clip(i);
        }

        public void RemoveAll<TArg>(Func<T, TArg, bool> match, TArg arg)
        {
            var i = 0;
            for (var j = 0; j < _builder.Count; j++)
            {
                if (!match(_builder[j], arg))
                {
                    if (i != j)
                    {
                        _builder[i] = _builder[j];
                    }

                    i++;
                }
            }

            Clip(i);
        }

        public void RemoveAll<TArg>(Func<T, int, TArg, bool> match, TArg arg)
        {
            var i = 0;
            for (var j = 0; j < _builder.Count; j++)
            {
                if (!match(_builder[j], i, arg))
                {
                    if (i != j)
                    {
                        _builder[i] = _builder[j];
                    }

                    i++;
                }
            }

            Clip(i);
        }

        public void ReverseContents()
        {
            _builder.Reverse();
        }

        public void Sort()
        {
            _builder.Sort();
        }

        public void Sort(IComparer<T>? comparer)
        {
            _builder.Sort(comparer);
        }

        public void Sort(Comparison<T> compare)
        {
            if (this.Count <= 1)
                return;

            Sort(Comparer<T>.Create(compare));
        }

        public void Sort(int startIndex, IComparer<T> comparer)
        {
            _builder.Sort(startIndex, _builder.Count - startIndex, comparer);
        }

        public T[] ToArray()
        {
            return _builder.ToArray();
        }

        public void CopyTo(T[] array, int start)
        {
            _builder.CopyTo(array, start);
        }

        public T Last()
            => _builder[_builder.Count - 1];

        internal T? LastOrDefault()
            => Count == 0 ? default : Last();

        public T First()
        {
            return _builder[0];
        }

        public bool Any()
        {
            return _builder.Count > 0;
        }

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutableOrNull()
        {
            if (Count == 0)
            {
                return default;
            }

            return this.ToImmutable();
        }

        /// <summary>
        /// Realizes the array, downcasting each element to a derived type.
        /// </summary>
        public ImmutableArray<U> ToDowncastedImmutable<U>()
            where U : T
        {
            if (Count == 0)
            {
                return ImmutableArray<U>.Empty;
            }

            var tmp = ArrayBuilder<U>.GetInstance(Count);
            foreach (var i in this)
            {
                tmp.Add((U)i!);
            }

            return tmp.ToImmutableAndFree();
        }

        public ImmutableArray<U> ToDowncastedImmutableAndFree<U>() where U : T
        {
            var result = ToDowncastedImmutable<U>();
            this.Free();
            return result;
        }

        /// <summary>
        /// Realizes the array and disposes the builder in one operation.
        /// </summary>
        public ImmutableArray<T> ToImmutableAndFree()
        {
            // This is mostly the same as 'MoveToImmutable', but avoids delegating to that method since 'Free' contains
            // fast paths to avoid caling 'Clear' in some cases.
            ImmutableArray<T> result;
            if (Count == 0)
            {
                result = ImmutableArray<T>.Empty;
            }
            else if (_builder.Capacity == Count)
            {
                result = _builder.MoveToImmutable();
            }
            else
            {
                result = ToImmutable();
            }

            this.Free();
            return result;
        }

        public T[] ToArrayAndFree()
        {
            var result = this.ToArray();
            this.Free();
            return result;
        }

        public void FreeAll(Func<T, ArrayBuilder<T>?> getNested)
        {
            foreach (var item in this)
            {
                getNested(item)?.FreeAll(getNested);
            }
            Free();
        }

        #region Poolable

        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            var pool = _pool;
            if (pool != null)
            {
                // We do not want to retain (potentially indefinitely) very large builders 
                // while the chance that we will need their capacity is diminishingly small.
                // It makes sense to constrain the capacity to some "not too small" number.
                if (_builder.Capacity < PooledArrayLengthLimitExclusive)
                {
                    if (this.Count != 0)
                    {
                        this.Clear();
                    }
                }
                else
                {
                    // Set the ImmutableArray<T>.Builder's _count to it's _capacity. This
                    // allows the MoveToImmutable call to succeed, resetting itself to an
                    // empty builder without allocating.
                    this.Count = this.Capacity;
                    _ = _builder.MoveToImmutable();

                    // Reset to our default capacity, leaving a now empty builder
                    // (with default capacity) in the pool.
                    this.Capacity = 8;
                }

                pool.Free(this);
            }
        }

        // 2) Expose the pool or the way to create a pool or the way to get an instance.
        //    for now we will expose both and figure which way works better
        private static readonly ObjectPool<ArrayBuilder<T>> s_poolInstance = CreatePool();
        public static ArrayBuilder<T> GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Count == 0);
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity)
        {
            var builder = GetInstance();
            builder.EnsureCapacity(capacity);
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity, T fillWithValue)
        {
            var builder = GetInstance();
            builder.EnsureCapacity(capacity);

            for (var i = 0; i < capacity; i++)
            {
                builder.Add(fillWithValue);
            }

            return builder;
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool()
        {
            return CreatePool(128); // we rarely need more than 10
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool(int size)
        {
            ObjectPool<ArrayBuilder<T>>? pool = null;
            pool = new ObjectPool<ArrayBuilder<T>>(() => new ArrayBuilder<T>(pool!), size);
            return pool;
        }

        #endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _builder.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _builder.GetEnumerator();
        }

        internal Dictionary<K, ImmutableArray<T>> ToDictionary<K>(Func<T, K> keySelector, IEqualityComparer<K>? comparer = null)
            where K : notnull
        {
            if (this.Count == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                var value = this[0];
                dictionary1.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary1;
            }

            if (this.Count == 0)
            {
                return new Dictionary<K, ImmutableArray<T>>(comparer);
            }

            // bucketize
            // prevent reallocation. it may not have 'count' entries, but it won't have more. 
            var accumulator = new Dictionary<K, ArrayBuilder<T>>(Count, comparer);
            for (var i = 0; i < Count; i++)
            {
                var item = this[i];
                var key = keySelector(item);
                if (!accumulator.TryGetValue(key, out var bucket))
                {
                    bucket = ArrayBuilder<T>.GetInstance();
                    accumulator.Add(key, bucket);
                }

                bucket.Add(item);
            }

            var dictionary = new Dictionary<K, ImmutableArray<T>>(accumulator.Count, comparer);

            // freeze
            foreach (var pair in accumulator)
            {
                dictionary.Add(pair.Key, pair.Value.ToImmutableAndFree());
            }

            return dictionary;
        }

        public void AddRange(ArrayBuilder<T> items)
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange<U>(ArrayBuilder<U> items, Func<U, T> selector)
        {
            foreach (var item in items)
            {
                _builder.Add(selector(item));
            }
        }

        public void AddRange<U>(ArrayBuilder<U> items) where U : T
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange<U>(ArrayBuilder<U> items, int start, int length) where U : T
        {
            Debug.Assert(start >= 0 && length >= 0);
            Debug.Assert(start + length <= items.Count);
            for (int i = start, end = start + length; i < end; i++)
            {
                Add(items[i]);
            }
        }

        public void AddRange(ImmutableArray<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(ImmutableArray<T> items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void AddRange(ImmutableArray<T> items, int start, int length)
        {
            Debug.Assert(start >= 0 && length >= 0);
            Debug.Assert(start + length <= items.Length);
            for (int i = start, end = start + length; i < end; i++)
            {
                Add(items[i]);
            }
        }

        public void AddRange<S>(ImmutableArray<S> items) where S : class, T
        {
            AddRange(ImmutableArray<T>.CastUp(items));
        }

        public void AddRange(T[] items, int start, int length)
        {
            Debug.Assert(start >= 0 && length >= 0);
            Debug.Assert(start + length <= items.Length);
            for (int i = start, end = start + length; i < end; i++)
            {
                Add(items[i]);
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(params T[] items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(T[] items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void Clip(int limit)
        {
            Debug.Assert(limit <= Count);
            _builder.Count = limit;
        }

        public void ZeroInit(int count)
        {
            _builder.Clear();
            _builder.Count = count;
        }

        public void AddMany(T item, int count)
        {
            EnsureCapacity(Count + count);

            for (var i = 0; i < count; i++)
            {
                Add(item);
            }
        }

        public void RemoveDuplicates()
        {
            var set = PooledHashSet<T>.GetInstance();

            var j = 0;
            for (var i = 0; i < Count; i++)
            {
                if (set.Add(this[i]))
                {
                    this[j] = this[i];
                    j++;
                }
            }

            Clip(j);
            set.Free();
        }

        public void SortAndRemoveDuplicates(IComparer<T>? comparer = null)
        {
            if (Count <= 1)
            {
                return;
            }

            comparer ??= Comparer<T>.Default;

            Sort(comparer);

            int j = 0;
            for (int i = 1; i < Count; i++)
            {
                if (comparer.Compare(this[j], this[i]) < 0)
                {
                    j++;
                    this[j] = this[i];
                }
            }

            Clip(j + 1);
        }

        public ImmutableArray<S> SelectDistinct<S>(Func<T, S> selector)
        {
            var result = ArrayBuilder<S>.GetInstance(Count);
            var set = PooledHashSet<S>.GetInstance();

            foreach (var item in this)
            {
                var selected = selector(item);
                if (set.Add(selected))
                {
                    result.Add(selected);
                }
            }

            set.Free();
            return result.ToImmutableAndFree();
        }

        public bool Any(Func<T, bool> predicate)
        {
            foreach (var item in this)
            {
                if (predicate(item))
                {
                    return true;
                }
            }
            return false;
        }

        public bool Any<A>(Func<T, A, bool> predicate, A arg)
        {
            foreach (var item in this)
            {
                if (predicate(item, arg))
                {
                    return true;
                }
            }
            return false;
        }

        public bool All(Func<T, bool> predicate)
        {
            foreach (var item in this)
            {
                if (!predicate(item))
                {
                    return false;
                }
            }
            return true;
        }

        public bool All<A>(Func<T, A, bool> predicate, A arg)
        {
            foreach (var item in this)
            {
                if (!predicate(item, arg))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Maps an array builder to immutable array.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="map">The mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array</returns>
        public ImmutableArray<TResult> SelectAsArray<TResult>(Func<T, TResult> map)
        {
            switch (Count)
            {
                case 0:
                    return [];

                case 1:
                    return [map(this[0])];

                case 2:
                    return [map(this[0]), map(this[1])];

                case 3:
                    return [map(this[0]), map(this[1]), map(this[2])];

                case 4:
                    return [map(this[0]), map(this[1]), map(this[2]), map(this[3])];

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(Count);
                    foreach (var item in this)
                    {
                        builder.Add(map(item));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Maps an array builder to immutable array.
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="map">The mapping delegate</param>
        /// <param name="arg">The extra input used by mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public ImmutableArray<TResult> SelectAsArray<TArg, TResult>(Func<T, TArg, TResult> map, TArg arg)
        {
            switch (Count)
            {
                case 0:
                    return [];

                case 1:
                    return [map(this[0], arg)];

                case 2:
                    return [map(this[0], arg), map(this[1], arg)];

                case 3:
                    return [map(this[0], arg), map(this[1], arg), map(this[2], arg)];

                case 4:
                    return [map(this[0], arg), map(this[1], arg), map(this[2], arg), map(this[3], arg)];

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(Count);
                    foreach (var item in this)
                    {
                        builder.Add(map(item, arg));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        /// <summary>
        /// Maps an array builder to immutable array.
        /// </summary>
        /// <typeparam name="TArg"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="map">The mapping delegate</param>
        /// <param name="arg">The extra input used by mapping delegate</param>
        /// <returns>If the items's length is 0, this will return an empty immutable array.</returns>
        public ImmutableArray<TResult> SelectAsArrayWithIndex<TArg, TResult>(Func<T, int, TArg, TResult> map, TArg arg)
        {
            switch (Count)
            {
                case 0:
                    return [];

                case 1:
                    return [map(this[0], 0, arg)];

                case 2:
                    return [map(this[0], 0, arg), map(this[1], 1, arg)];

                case 3:
                    return [map(this[0], 0, arg), map(this[1], 1, arg), map(this[2], 2, arg)];

                case 4:
                    return [map(this[0], 0, arg), map(this[1], 1, arg), map(this[2], 2, arg), map(this[3], 3, arg)];

                default:
                    var builder = ArrayBuilder<TResult>.GetInstance(Count);
                    foreach (var item in this)
                    {
                        builder.Add(map(item, builder.Count, arg));
                    }

                    return builder.ToImmutableAndFree();
            }
        }

        // The following extension methods allow an ArrayBuilder to be used as a stack. 
        // Note that the order of an IEnumerable from a List is from bottom to top of stack. An IEnumerable 
        // from the framework Stack is from top to bottom.
        public void Push(T e)
            => Add(e);

        public T Pop()
        {
            var e = Peek();
            RemoveAt(Count - 1);
            return e;
        }

        public bool TryPop([MaybeNullWhen(false)] out T result)
        {
            if (Count > 0)
            {
                result = Pop();
                return true;
            }

            result = default;
            return false;
        }

        public T Peek()
            => this[Count - 1];

#if !MICROSOFT_CODEANALYSIS_POOLEDOBJECTS_NO_POOLED_DISPOSER

        private static readonly ObjectPool<ArrayBuilder<T>> s_keepLargeInstancesPool = CreatePool();

        public static PooledDisposer<ArrayBuilder<T>> GetInstance(out ArrayBuilder<T> instance)
            => GetInstance(discardLargeInstances: true, out instance);

        public static PooledDisposer<ArrayBuilder<T>> GetInstance(int capacity, out ArrayBuilder<T> instance)
        {
            instance = GetInstance(capacity);
            return new PooledDisposer<ArrayBuilder<T>>(instance);
        }

        public static PooledDisposer<ArrayBuilder<T>> GetInstance(int capacity, T fillWithValue, out ArrayBuilder<T> instance)
        {
            instance = GetInstance(capacity, fillWithValue);
            return new PooledDisposer<ArrayBuilder<T>>(instance);
        }

        public static PooledDisposer<ArrayBuilder<T>> GetInstance(bool discardLargeInstances, out ArrayBuilder<T> instance)
        {
            // If we're discarding large instances (the default behavior), then just use the normal pool.  If we're not, use
            // a specific pool so that *other* normal callers don't accidentally get it and discard it.
            instance = discardLargeInstances ? GetInstance() : s_keepLargeInstancesPool.Allocate();
            return new PooledDisposer<ArrayBuilder<T>>(instance, discardLargeInstances);
        }

        void IPooled.Free(bool discardLargeInstances)
        {
            // If we're discarding large instances, use the default behavior (which already does that).  Otherwise, always
            // clear and free the instance back to its originating pool.
            if (discardLargeInstances)
            {
                Free();
            }
            else
            {
                this.Clear();
                _pool?.Free(this);
            }
        }

#endif
    }
}
