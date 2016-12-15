// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Collections;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("Count = {Count,nq}")]
    [DebuggerTypeProxy(typeof(ArrayBuilder<>.DebuggerProxy))]
    internal sealed partial class ArrayBuilder<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
    {
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
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = _builder[i];
                    }

                    return result;
                }
            }
        }

        #endregion

        private readonly ImmutableArray<T>.Builder _builder;

        private readonly ArrayBuilderPool _pool;

        internal const int DefaultCapacity = 8;

        public ArrayBuilder(int capacity)
        {
            _builder = ImmutableArray.CreateBuilder<T>(capacity);
        }

        public ArrayBuilder() :
            this(DefaultCapacity)
        { }

        private ArrayBuilder(ArrayBuilderPool pool, int capacity)
        {
            _builder = ImmutableArray.CreateBuilder<T>(capacity);
            _pool = pool;
        }

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutable()
        {
            return _builder.ToImmutable();
        }

        internal int Capacity
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

        /// <summary>
        /// Write <paramref name="value"/> to slot <paramref name="index"/>. 
        /// Fills in unallocated slots preceding the <paramref name="index"/>, if any.
        /// </summary>
        public void SetItem(int index, T value)
        {
            while (index > _builder.Count)
            {
                _builder.Add(default(T));
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
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(_builder[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void RemoveAt(int index)
        {
            _builder.RemoveAt(index);
        }

        public void RemoveLast()
        {
            _builder.RemoveAt(_builder.Count - 1);
        }

        public void ReverseContents()
        {
            _builder.Reverse();
        }

        public void Sort()
        {
            _builder.Sort();
        }

        public void Sort(IComparer<T> comparer)
        {
            _builder.Sort(comparer);
        }

        public void Sort(Comparison<T> compare)
            => Sort(Comparer<T>.Create(compare));

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
        {
            return _builder[_builder.Count - 1];
        }

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
                return default(ImmutableArray<T>);
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
                tmp.Add((U)i);
            }

            return tmp.ToImmutableAndFree();
        }

        /// <summary>
        /// Realizes the array and disposes the builder in one operation.
        /// </summary>
        public ImmutableArray<T> ToImmutableAndFree()
        {
            var result = _builder.Capacity == _builder.Count
                ? _builder.MoveToImmutable()
                : _builder.ToImmutable();

            this.Free();
            return result;
        }

        public T[] ToArrayAndFree()
        {
            var result = this.ToArray();
            this.Free();
            return result;
        }

        #region Poolable

        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            var pool = _pool;
            if (pool != null)
            {
                // According to the statistics of a C# compiler self-build, the most commonly used builder size is 0.  (808003 uses).
                // The distant second is the Count == 1 (455619), then 2 (106362) ...
                // After about 50 (just 67) we have a long tail of infrequently used builder sizes.
                // However we have builders with size up to 50K   (just one such thing)
                //
                // We do not want to retain (potentially indefinitely) very large builders 
                // while the chance that we will need their size is diminishingly small.
                // It makes sense to constrain the size to some "not too small" number. 
                // Overall perf does not seem to be very sensitive to this number, so I picked 128 as a limit.
                if (this.Count < 128)
                {
                    if (this.Count != 0)
                    {
                        this.Clear();
                    }

                    pool.Free(this);
                    return;
                }
            }
        }

        // 2) Expose the pool or the way to create a pool or the way to get an instance.
        //    for now we will expose both and figure which way works better
        // we rarely need more than 10
        private static readonly ArrayBuilderPool s_poolInstance = new ArrayBuilderPool(128);

        public static ArrayBuilder<T> GetInstance()
        {
            var builder = s_poolInstance.Allocate();
            Debug.Assert(builder.Count == 0);
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity)
        {
            var builder = s_poolInstance.AllocateExisting(capacity);
            Debug.Assert(builder.Capacity == 0 || builder.Capacity == capacity);
            if (builder.Capacity != capacity)
            {
                builder.Capacity = capacity;
            }
            return builder;
        }

        public static ArrayBuilder<T> GetInstance(int capacity, T fillWithValue)
        {
            var builder = GetInstance(capacity);

            for (int i = 0; i < capacity; i++)
            {
                builder.Add(fillWithValue);
            }

            return builder;
        }

        #endregion

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal Dictionary<K, ImmutableArray<T>> ToDictionary<K>(Func<T, K> keySelector, IEqualityComparer<K> comparer = null)
        {
            if (this.Count == 1)
            {
                var dictionary1 = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = this[0];
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
            for (int i = 0; i < Count; i++)
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

        public void AddRange<U>(ArrayBuilder<U> items) where U : T
        {
            _builder.AddRange(items._builder);
        }

        public void AddRange(ImmutableArray<T> items)
        {
            _builder.AddRange(items);
        }

        public void AddRange(ImmutableArray<T> items, int length)
        {
            _builder.AddRange(items, length);
        }

        public void AddRange<S>(ImmutableArray<S> items) where S : class, T
        {
            AddRange(ImmutableArray<T>.CastUp(items));
        }

        public void AddRange(T[] items, int start, int length)
        {
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
            for (int i = 0; i < count; i++)
            {
                Add(item);
            }
        }

        public void RemoveDuplicates()
        {
            var set = PooledHashSet<T>.GetInstance();

            int j = 0;
            for (int i = 0; i < Count; i++)
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

        /// <summary>
        /// Generic implementation of object pooling pattern with predefined pool size limit. The main
        /// purpose is that limited number of frequently used objects can be kept in the pool for
        /// further recycling.
        /// 
        /// Notes: 
        /// 1) it is not the goal to keep all returned objects. Pool is not meant for storage. If there
        ///    is no space in the pool, extra returned objects will be dropped.
        /// 
        /// 2) it is implied that if object was obtained from a pool, the caller will return it back in
        ///    a relatively short time. Keeping checked out objects for long durations is ok, but 
        ///    reduces usefulness of pooling. Just new up your own.
        /// 
        /// Not returning objects to the pool in not detrimental to the pool's work, but is a bad practice. 
        /// Rationale: 
        ///    If there is no intent for reusing the object, do not use pool - just use "new". 
        /// </summary>
        internal class ArrayBuilderPool
        {
            [DebuggerDisplay("{Value,nq}")]
            private struct Element
            {
                internal ArrayBuilder<T> Value;
            }

            // Storage for the pool objects. The first item is stored in a dedicated field because we
            // expect to be able to satisfy most requests from it.
            private ArrayBuilder<T> _firstItem;
            private readonly Element[] _items;

            internal ArrayBuilderPool(int size)
            {
                Debug.Assert(size >= 1);
                _items = new Element[size - 1];
            }

            private ArrayBuilder<T> CreateInstance(int capacity)
            {
                return new ArrayBuilder<T>(this, capacity);
            }

            /// <summary>
            /// Produces an instance.
            /// </summary>
            /// <remarks>
            /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
            /// Note that Free will try to store recycled objects close to the start thus statistically 
            /// reducing how far we will typically search.
            /// </remarks>
            internal ArrayBuilder<T> Allocate()
            {
                // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                ArrayBuilder<T> inst = _firstItem;
                if (inst == null || inst != Interlocked.CompareExchange(ref _firstItem, null, inst))
                {
                    inst = AllocateSlow();
                }

                return inst;
            }

            private ArrayBuilder<T> AllocateSlow()
            {
                var items = _items;

                for (int i = 0; i < items.Length; i++)
                {
                    // Note that the initial read is optimistically not synchronized. That is intentional. 
                    // We will interlock only when we have a candidate. in a worst case we may miss some
                    // recently returned objects. Not a big deal.
                    ArrayBuilder<T> inst = items[i].Value;
                    if (inst != null)
                    {
                        if (inst == Interlocked.CompareExchange(ref items[i].Value, null, inst))
                        {
                            return inst;
                        }
                    }
                }

                return CreateInstance(DefaultCapacity);
            }

            /// <summary>
            /// Produces an instance.
            /// </summary>
            /// <remarks>
            /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
            /// Note that Free will try to store recycled objects close to the start thus statistically 
            /// reducing how far we will typically search.
            /// </remarks>
            internal ArrayBuilder<T> AllocateExisting(int capacity)
            {
                // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                ArrayBuilder<T> inst = GetIfMatch(ref _firstItem, capacity);
                if (inst != null)
                {
                    return inst;
                }

                return AllocateExistingSlow(capacity);
            }

            private ArrayBuilder<T> GetIfMatch(ref ArrayBuilder<T> builder, int capacity)
            {
                var local = builder;
                if (local == null)
                {
                    return null;
                }

                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                var c = local.Capacity;
                if (c == 0 || c == capacity)
                {
                    if (local == Interlocked.CompareExchange(ref builder, null, local))
                    {
                        c = local.Capacity;
                        if (c == 0 || c == capacity)
                        {
                            return local;
                        }

                        Free(local);
                    }
                }

                return null;
            }

            private ArrayBuilder<T> AllocateExistingSlow(int capacity)
            {
                var items = _items;

                for (int i = 0; i < items.Length; i++)
                {
                    ArrayBuilder<T> inst = GetIfMatch(ref items[i].Value, capacity);
                    if (inst != null)
                    {
                        return inst;
                    }
                }

                return CreateInstance(capacity);
            }

            /// <summary>
            /// Returns objects to the pool.
            /// </summary>
            /// <remarks>
            /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
            /// Note that Free will try to store recycled objects close to the start thus statistically 
            /// reducing how far we will typically search in Allocate.
            /// </remarks>
            internal void Free(ArrayBuilder<T> obj)
            {
                Validate(obj);

                if (_firstItem == null)
                {
                    // Intentionally not using interlocked here. 
                    // In a worst case scenario two objects may be stored into same slot.
                    // It is very unlikely to happen and will only mean that one of the objects will get collected.
                    _firstItem = obj;
                }
                else
                {
                    FreeSlow(obj);
                }
            }

            private void FreeSlow(ArrayBuilder<T> obj)
            {
                var items = _items;
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i].Value == null)
                    {
                        // Intentionally not using interlocked here. 
                        // In a worst case scenario two objects may be stored into same slot.
                        // It is very unlikely to happen and will only mean that one of the objects will get collected.
                        items[i].Value = obj;
                        break;
                    }
                }
            }

            [Conditional("DEBUG")]
            private void Validate(object obj)
            {
                Debug.Assert(obj != null, "freeing null?");

                Debug.Assert(_firstItem != obj, "freeing twice?");

                var items = _items;
                for (int i = 0; i < items.Length; i++)
                {
                    var value = items[i].Value;
                    if (value == null)
                    {
                        return;
                    }

                    Debug.Assert(value != obj, "freeing twice?");
                }
            }
        }
    }
}
