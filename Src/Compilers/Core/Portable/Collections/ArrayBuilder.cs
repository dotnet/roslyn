// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    [DebuggerDisplay("Count = {Count,nq}")]
    [DebuggerTypeProxy(typeof(ArrayBuilder<>.DebuggerProxy))]
    internal sealed partial class ArrayBuilder<T> : IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        #region DebuggerProxy

        private sealed class DebuggerProxy
        {
            private readonly ArrayBuilder<T> builder;

            public DebuggerProxy(ArrayBuilder<T> builder)
            {
                this.builder = builder;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public T[] A
            {
                get
                {
                    var result = new T[builder.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = builder[i];
                    }

                    return result;
                }
            }
        }

        #endregion

        private readonly ImmutableArray<T>.Builder builder;

        private readonly ObjectPool<ArrayBuilder<T>> pool;

        public ArrayBuilder(int size)
        {
            this.builder = ImmutableArray.CreateBuilder<T>(size);
        }

        public ArrayBuilder() :
            this(8)
        { }

        private ArrayBuilder(ObjectPool<ArrayBuilder<T>> pool) :
            this()
        {
            this.pool = pool;
        }

        /// <summary>
        /// Realizes the array.
        /// </summary>
        public ImmutableArray<T> ToImmutable()
        {
            return this.builder.ToImmutable();
        }

        public int Count
        {
            get
            {
                return this.builder.Count;
            }
        }

        public T this[int index]
        {
            get
            {
                return this.builder[index];
            }
            set
            {
                this.builder[index] = value;
            }
        }

        public void Add(T item)
        {
            this.builder.Add(item);
        }

        public void Insert(int index, T item)
        {
            this.builder.Insert(index, item);
        }

        public void EnsureCapacity(int capacity)
        {
            this.builder.EnsureCapacity(capacity);
        }

        public void Clear()
        {
            this.builder.Clear();
        }

        public bool Contains(T item)
        {
            return this.builder.Contains(item);
        }

        public void RemoveAt(int index)
        {
            this.builder.RemoveAt(index);
        }

        public void RemoveLast()
        {
            this.builder.RemoveAt(this.builder.Count - 1);
        }

        public void ReverseContents()
        {
            this.builder.ReverseContents();
        }

        public void Sort()
        {
            this.builder.Sort();
        }

        public void Sort(IComparer<T> comparer)
        {
            this.builder.Sort(comparer);
        }

        public void Sort(int startIndex, IComparer<T> comparer)
        {
            this.builder.Sort(startIndex, this.builder.Count - startIndex, comparer);
        }

        public T[] ToArray()
        {
            return this.builder.ToArray();
        }

        public void CopyTo(T[] array, int start)
        {
            this.builder.CopyTo(array, start);
        }

        public T Last()
        {
            return this.builder[this.builder.Count - 1];
        }

        public T First()
        {
            return this.builder[0];
        }

        public bool Any()
        {
            return this.builder.Count > 0;
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
            var result = this.ToImmutable();
            this.Free();
            return result;
        }

        public T[] ToArrayAndFree()
        {
            var result = this.ToArray();
            this.Free();
            return result;
        }


        #region "Poolable"

        //
        // To implement Poolable, you need two things:
        // 1) Expose Freeing primitive. 
        public void Free()
        {
            var pool = this.pool;
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
                // Overal perf does not seem to be very sensitive to this number, so I picked 128 as a limit.
                if (this.Count < 128)
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

        //2) Expose the pool or the way to create a pool or the way to get an instance.
        //       for now we will expose both and figure which way works better
        private static readonly ObjectPool<ArrayBuilder<T>> PoolInstance = CreatePool();
        public static ArrayBuilder<T> GetInstance()
        {
            var builder = PoolInstance.Allocate();
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

            for (int i = 0; i < capacity; i++)
            {
                builder.Add(fillWithValue);
            }

            return builder;
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool()
        {
            return CreatePool(128); // we rarily need more than 10
        }

        public static ObjectPool<ArrayBuilder<T>> CreatePool(int size)
        {
            ObjectPool<ArrayBuilder<T>> pool = null;
            pool = new ObjectPool<ArrayBuilder<T>>(() => new ArrayBuilder<T>(pool), size);
            return pool;
        }

        #endregion

        public ArrayBuilderEnumerator GetEnumerator()
        {
            return new ArrayBuilderEnumerator(this);
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
                var dictionary = new Dictionary<K, ImmutableArray<T>>(1, comparer);
                T value = this[0];
                dictionary.Add(keySelector(value), ImmutableArray.Create(value));
                return dictionary;
            }
            else
            {
                // bucketize
                // prevent reallocation. it may not have 'count' entries, but it won't have more. 
                var accumulator = new Dictionary<K, ArrayBuilder<T>>(Count, comparer);
                for (int i = 0; i < Count; i++)
                {
                    var item = this[i];
                    var key = keySelector(item);

                    ArrayBuilder<T> bucket;
                    if (!accumulator.TryGetValue(key, out bucket))
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
        }

        public void AddRange(ArrayBuilder<T> items)
        {
            this.builder.AddRange(items.builder);
        }

        public void AddRange<U>(ArrayBuilder<U> items) where U : T
        {
            this.builder.AddRange(items.builder);
        }

        public void AddRange(ImmutableArray<T> items)
        {
            this.builder.AddRange(items);
        }

        public void AddRange(ImmutableArray<T> items, int length)
        {
            this.builder.AddRange(items, length);
        }

        public void AddRange(IEnumerable<T> items)
        {
            this.builder.AddRange(items);
        }

        public void AddRange(params T[] items)
        {
            this.builder.AddRange(items);
        }

        public void AddRange(T[] items, int length)
        {
            this.builder.AddRange(items, length);
        }

        public void Clip(int limit)
        {
            Debug.Assert(limit <= Count);
            this.builder.Count = limit;
        }

        public void ZeroInit(int count)
        {
            this.builder.Clear();
            this.builder.Count = count;
        }

        public void AddMany(T item, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Add(item);
            }
        }
    }
}
