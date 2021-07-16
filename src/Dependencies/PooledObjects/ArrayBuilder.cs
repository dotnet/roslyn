// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.PooledObjects
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
                if (_builder.Capacity < 128)
                {
                    if (this.Count != 0)
                    {
                        this.Clear();
                    }

                    pool.Free(this);
                    return;
                }
                else
                {
                    pool.ForgetTrackedObject(this);
                }
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

        public void SortAndRemoveDuplicates(IComparer<T> comparer)
        {
            if (Count <= 1)
            {
                return;
            }

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
    }
}
