// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal static class SharedPoolExtensions
    {
        private const int Threshold = 512;

        public static PooledObject<StringBuilder> GetPooledObject(this ObjectPool<StringBuilder> pool)
            => PooledObject<StringBuilder>.Create(pool);

        public static PooledObject<StringBuilder> GetPooledObject(this ObjectPool<StringBuilder> pool, out StringBuilder builder)
        {
            var pooledObject = PooledObject<StringBuilder>.Create(pool);
            builder = pooledObject.Object;
            return pooledObject;
        }

        public static PooledObject<Stack<TItem>> GetPooledObject<TItem>(this ObjectPool<Stack<TItem>> pool)
            => PooledObject<Stack<TItem>>.Create(pool);

        public static PooledObject<Queue<TItem>> GetPooledObject<TItem>(this ObjectPool<Queue<TItem>> pool)
            => PooledObject<Queue<TItem>>.Create(pool);

        public static PooledObject<HashSet<TItem>> GetPooledObject<TItem>(this ObjectPool<HashSet<TItem>> pool)
            => PooledObject<HashSet<TItem>>.Create(pool);

        public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool) where TKey : notnull
            => PooledObject<Dictionary<TKey, TValue>>.Create(pool);

        public static PooledObject<List<TItem>> GetPooledObject<TItem>(this ObjectPool<List<TItem>> pool)
            => PooledObject<List<TItem>>.Create(pool);

        public static PooledObject<SegmentedList<TItem>> GetPooledObject<TItem>(this ObjectPool<SegmentedList<TItem>> pool)
            => PooledObject<SegmentedList<TItem>>.Create(pool);

        public static PooledObject<List<TItem>> GetPooledObject<TItem>(this ObjectPool<List<TItem>> pool, out List<TItem> list)
        {
            var pooledObject = PooledObject<List<TItem>>.Create(pool);
            list = pooledObject.Object;
            return pooledObject;
        }

        public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool) where T : class
            => new(pool, p => p.Allocate(), (p, o) => p.Free(o));

        public static StringBuilder AllocateAndClear(this ObjectPool<StringBuilder> pool)
        {
            var sb = pool.Allocate();
            sb.Clear();

            return sb;
        }

        public static Stack<T> AllocateAndClear<T>(this ObjectPool<Stack<T>> pool)
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public static Queue<T> AllocateAndClear<T>(this ObjectPool<Queue<T>> pool)
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public static HashSet<T> AllocateAndClear<T>(this ObjectPool<HashSet<T>> pool)
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public static SegmentedHashSet<T> AllocateAndClear<T>(this ObjectPool<SegmentedHashSet<T>> pool)
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public static Dictionary<TKey, TValue> AllocateAndClear<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool)
            where TKey : notnull
        {
            var map = pool.Allocate();
            map.Clear();

            return map;
        }

        public static List<T> AllocateAndClear<T>(this ObjectPool<List<T>> pool)
        {
            var list = pool.Allocate();
            list.Clear();

            return list;
        }

        public static SegmentedList<T> AllocateAndClear<T>(this ObjectPool<SegmentedList<T>> pool)
        {
            var list = pool.Allocate();
            list.Clear();

            return list;
        }

        public static void ClearAndFree(this ObjectPool<StringBuilder> pool, StringBuilder sb)
        {
            if (sb == null)
            {
                return;
            }

            sb.Clear();

            if (sb.Capacity > Threshold)
            {
                sb.Capacity = Threshold;
            }

            pool.Free(sb);
        }

        public static void ClearAndFree<T>(this ObjectPool<HashSet<T>> pool, HashSet<T> set)
        {
            if (set == null)
            {
                return;
            }

            var count = set.Count;
            set.Clear();

            if (count > Threshold)
            {
                set.TrimExcess();
            }

            pool.Free(set);
        }

        public static void ClearAndFree<T>(this ObjectPool<SegmentedHashSet<T>> pool, SegmentedHashSet<T> set)
        {
            if (set == null)
            {
                return;
            }

            var count = set.Count;
            set.Clear();

            if (count > Threshold)
            {
                set.TrimExcess();
            }

            pool.Free(set);
        }

        public static void ClearAndFree<T>(this ObjectPool<ConcurrentSet<T>> pool, ConcurrentSet<T> set) where T : notnull
        {
            if (set == null)
                return;

            // if set grew too big, don't put it back to pool
            if (set.Count > Threshold)
            {
                pool.ForgetTrackedObject(set);
                return;
            }

            set.Clear();
            pool.Free(set);
        }

        public static void ClearAndFree<T>(this ObjectPool<Stack<T>> pool, Stack<T> set)
        {
            if (set == null)
            {
                return;
            }

            var count = set.Count;
            set.Clear();

            if (count > Threshold)
            {
                set.TrimExcess();
            }

            pool.Free(set);
        }

        public static void ClearAndFree<T>(this ObjectPool<ConcurrentStack<T>> pool, ConcurrentStack<T> stack)
        {
            if (stack == null)
                return;

            // if stack grew too big, don't put it back to pool
            if (stack.Count > Threshold)
            {
                pool.ForgetTrackedObject(stack);
                return;
            }

            stack.Clear();
            pool.Free(stack);
        }

        public static void ClearAndFree<T>(this ObjectPool<Queue<T>> pool, Queue<T> set)
        {
            if (set == null)
            {
                return;
            }

            var count = set.Count;
            set.Clear();

            if (count > Threshold)
            {
                set.TrimExcess();
            }

            pool.Free(set);
        }

        public static void ClearAndFree<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool, Dictionary<TKey, TValue> map)
            where TKey : notnull
        {
            if (map == null)
            {
                return;
            }

            // if map grew too big, don't put it back to pool
            if (map.Count > Threshold)
            {
                pool.ForgetTrackedObject(map);
                return;
            }

            map.Clear();
            pool.Free(map);
        }

        public static void ClearAndFree<TKey, TValue>(this ObjectPool<ConcurrentDictionary<TKey, TValue>> pool, ConcurrentDictionary<TKey, TValue> map)
            where TKey : notnull
        {
            if (map == null)
                return;

            // if map grew too big, don't put it back to pool
            if (map.Count > Threshold)
            {
                pool.ForgetTrackedObject(map);
                return;
            }

            map.Clear();
            pool.Free(map);
        }

        public static void ClearAndFree<T>(this ObjectPool<List<T>> pool, List<T> list, bool trim = true)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();

            if (trim && list.Capacity > Threshold)
            {
                list.Capacity = Threshold;
            }

            pool.Free(list);
        }

        public static void ClearAndFree<T>(this ObjectPool<SegmentedList<T>> pool, SegmentedList<T> list, bool trim = true)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();

            if (trim && list.Capacity > Threshold)
            {
                list.Capacity = Threshold;
            }

            pool.Free(list);
        }
    }
}
