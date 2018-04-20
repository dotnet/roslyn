// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal static class SharedPoolExtensions
    {
        private const int Threshold = 512;

        public static PooledObject<StringBuilder> GetPooledObject(this ObjectPool<StringBuilder> pool)
        {
            return PooledObject<StringBuilder>.Create(pool);
        }

        public static PooledObject<Stopwatch> GetPooledObject(this ObjectPool<Stopwatch> pool)
        {
            return PooledObject<Stopwatch>.Create(pool);
        }

        public static PooledObject<Stack<TItem>> GetPooledObject<TItem>(this ObjectPool<Stack<TItem>> pool)
        {
            return PooledObject<Stack<TItem>>.Create(pool);
        }

        public static PooledObject<Queue<TItem>> GetPooledObject<TItem>(this ObjectPool<Queue<TItem>> pool)
        {
            return PooledObject<Queue<TItem>>.Create(pool);
        }

        public static PooledObject<HashSet<TItem>> GetPooledObject<TItem>(this ObjectPool<HashSet<TItem>> pool)
        {
            return PooledObject<HashSet<TItem>>.Create(pool);
        }

        public static PooledObject<Dictionary<TKey, TValue>> GetPooledObject<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool)
        {
            return PooledObject<Dictionary<TKey, TValue>>.Create(pool);
        }

        public static PooledObject<List<TItem>> GetPooledObject<TItem>(this ObjectPool<List<TItem>> pool)
        {
            return PooledObject<List<TItem>>.Create(pool);
        }

        public static PooledObject<T> GetPooledObject<T>(this ObjectPool<T> pool) where T : class
        {
            return new PooledObject<T>(pool, p => p.Allocate(), (p, o) => p.Free(o));
        }

        public static StringBuilder AllocateAndClear(this ObjectPool<StringBuilder> pool)
        {
            var sb = pool.Allocate();
            sb.Clear();

            return sb;
        }

        public static Stopwatch AllocateAndClear(this ObjectPool<Stopwatch> pool)
        {
            var watch = pool.Allocate();
            watch.Reset();

            return watch;
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

        public static Dictionary<TKey, TValue> AllocateAndClear<TKey, TValue>(this ObjectPool<Dictionary<TKey, TValue>> pool)
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

        public static void ClearAndFree(this ObjectPool<Stopwatch> pool, Stopwatch watch)
        {
            if (watch == null)
            {
                return;
            }

            watch.Reset();
            pool.Free(watch);
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

        public static void ClearAndFree<T>(this ObjectPool<List<T>> pool, List<T> list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();

            if (list.Capacity > Threshold)
            {
                list.Capacity = Threshold;
            }

            pool.Free(list);
        }
    }
}
