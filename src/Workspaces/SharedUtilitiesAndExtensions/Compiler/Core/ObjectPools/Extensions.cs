// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal static class SharedPoolExtensions
{
    private const int Threshold = 512;

    extension(ObjectPool<StringBuilder> pool)
    {
        public PooledObject<StringBuilder> GetPooledObject()
        => PooledObject<StringBuilder>.Create(pool);

        public PooledObject<StringBuilder> GetPooledObject(out StringBuilder builder)
        {
            var pooledObject = PooledObject<StringBuilder>.Create(pool);
            builder = pooledObject.Object;
            return pooledObject;
        }

        public StringBuilder AllocateAndClear()
        {
            var sb = pool.Allocate();
            sb.Clear();

            return sb;
        }

        public void ClearAndFree(StringBuilder sb)
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
    }

    extension<TItem>(ObjectPool<Stack<TItem>> pool)
    {
        public PooledObject<Stack<TItem>> GetPooledObject()
        => PooledObject<Stack<TItem>>.Create(pool);

        public PooledObject<Stack<TItem>> GetPooledObject(out Stack<TItem> stack)
        {
            var pooledObject = PooledObject<Stack<TItem>>.Create(pool);
            stack = pooledObject.Object;
            return pooledObject;
        }
    }

    extension<TItem>(ObjectPool<Queue<TItem>> pool)
    {
        public PooledObject<Queue<TItem>> GetPooledObject()
        => PooledObject<Queue<TItem>>.Create(pool);
    }

    extension<TItem>(ObjectPool<HashSet<TItem>> pool)
    {
        public PooledObject<HashSet<TItem>> GetPooledObject()
        => PooledObject<HashSet<TItem>>.Create(pool);

        public PooledObject<HashSet<TItem>> GetPooledObject(out HashSet<TItem> set)
        {
            var pooledObject = PooledObject<HashSet<TItem>>.Create(pool);
            set = pooledObject.Object;
            return pooledObject;
        }
    }

    extension<TKey, TValue>(ObjectPool<Dictionary<TKey, TValue>> pool) where TKey : notnull
    {
        public PooledObject<Dictionary<TKey, TValue>> GetPooledObject() => PooledObject<Dictionary<TKey, TValue>>.Create(pool);

        public PooledObject<Dictionary<TKey, TValue>> GetPooledObject(out Dictionary<TKey, TValue> dictionary)
        {
            var pooledObject = PooledObject<Dictionary<TKey, TValue>>.Create(pool);
            dictionary = pooledObject.Object;
            return pooledObject;
        }

        public Dictionary<TKey, TValue> AllocateAndClear()
        {
            var map = pool.Allocate();
            map.Clear();

            return map;
        }

        public void ClearAndFree(Dictionary<TKey, TValue> map)
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
    }

    extension<TItem>(ObjectPool<List<TItem>> pool)
    {
        public PooledObject<List<TItem>> GetPooledObject()
        => PooledObject<List<TItem>>.Create(pool);

        public PooledObject<List<TItem>> GetPooledObject(out List<TItem> list)
        {
            var pooledObject = PooledObject<List<TItem>>.Create(pool);
            list = pooledObject.Object;
            return pooledObject;
        }
    }

    extension<TItem>(ObjectPool<SegmentedList<TItem>> pool)
    {
        public PooledObject<SegmentedList<TItem>> GetPooledObject()
        => PooledObject<SegmentedList<TItem>>.Create(pool);

        public PooledObject<SegmentedList<TItem>> GetPooledObject(out SegmentedList<TItem> list)
        {
            var pooledObject = PooledObject<SegmentedList<TItem>>.Create(pool);
            list = pooledObject.Object;
            return pooledObject;
        }
    }

    extension<T>(ObjectPool<ConcurrentSet<T>> pool) where T : notnull
    {
        public PooledObject<ConcurrentSet<T>> GetPooledObject(out ConcurrentSet<T> set)
        {
            var pooledObject = PooledObject<ConcurrentSet<T>>.Create(pool);
            set = pooledObject.Object;
            return pooledObject;
        }

        public ConcurrentSet<T> AllocateAndClear()
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public void ClearAndFree(ConcurrentSet<T> set)
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
    }

    extension<T>(ObjectPool<T> pool) where T : class
    {
        public PooledObject<T> GetPooledObject() => new(pool, p => p.Allocate(), (p, o) => p.Free(o));
    }

    extension<T>(ObjectPool<Stack<T>> pool)
    {
        public Stack<T> AllocateAndClear()
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public void ClearAndFree(Stack<T> stack)
        {
            if (stack == null)
                return;

            var count = stack.Count;
            stack.Clear();

            if (count > Threshold && pool.TrimOnFree)
                stack.TrimExcess();

            pool.Free(stack);
        }
    }

    extension<T>(ObjectPool<Queue<T>> pool)
    {
        public Queue<T> AllocateAndClear()
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public void ClearAndFree(Queue<T> set)
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
    }

    extension<T>(ObjectPool<HashSet<T>> pool)
    {
        public HashSet<T> AllocateAndClear()
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public void ClearAndFree(HashSet<T> set)
        {
            if (set == null)
            {
                return;
            }

            var count = set.Count;
            set.Clear();

            if (count > Threshold && pool.TrimOnFree)
            {
                set.TrimExcess();
            }

            pool.Free(set);
        }
    }

    extension<T>(ObjectPool<SegmentedHashSet<T>> pool)
    {
        public SegmentedHashSet<T> AllocateAndClear()
        {
            var set = pool.Allocate();
            set.Clear();

            return set;
        }

        public void ClearAndFree(SegmentedHashSet<T> set)
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
    }

    extension<T>(ObjectPool<List<T>> pool)
    {
        public List<T> AllocateAndClear()
        {
            var list = pool.Allocate();
            list.Clear();

            return list;
        }

        public void ClearAndFree(List<T> list, bool trim = true)
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

    extension<T>(ObjectPool<SegmentedList<T>> pool)
    {
        public SegmentedList<T> AllocateAndClear()
        {
            var list = pool.Allocate();
            list.Clear();

            return list;
        }

        public void ClearAndFree(SegmentedList<T> list, bool trim = true)
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

    extension<T>(ObjectPool<ConcurrentStack<T>> pool)
    {
        public void ClearAndFree(ConcurrentStack<T> stack)
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
    }

    extension<TKey, TValue>(ObjectPool<ConcurrentDictionary<TKey, TValue>> pool) where TKey : notnull
    {
        public void ClearAndFree(ConcurrentDictionary<TKey, TValue> map)
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
    }
}
