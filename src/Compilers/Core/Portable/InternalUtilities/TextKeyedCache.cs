// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    internal class TextKeyedCache<T> where T : class
    {
        // immutable tuple - text and corresponding item
        // reference type because we want atomic assignments
        private class SharedEntryValue
        {
            public readonly string Text;
            public readonly T Item;

            public SharedEntryValue(string Text, T item)
            {
                this.Text = Text;
                this.Item = item;
            }
        }

        // TODO: Need to tweak the size with more scenarios.
        //       for now this is what works well enough with 
        //       Roslyn C# compiler project

        // Size of local cache.
        private const int LocalSizeBits = 11;
        private const int LocalSize = (1 << LocalSizeBits);
        private const int LocalSizeMask = LocalSize - 1;

        // max size of shared cache.
        private const int SharedSizeBits = 16;
        private const int SharedSize = (1 << SharedSizeBits);
        private const int SharedSizeMask = SharedSize - 1;

        // size of bucket in shared cache. (local cache has bucket size 1).
        private const int SharedBucketBits = 4;
        private const int SharedBucketSize = (1 << SharedBucketBits);
        private const int SharedBucketSizeMask = SharedBucketSize - 1;

        // local cache
        // simple fast and not threadsafe cache 
        // with limited size and "last add wins" expiration policy
        private readonly (string Text, int HashCode, T Item)[] _localTable = new (string Text, int HashCode, T Item)[LocalSize];

        // shared threadsafe cache
        // slightly slower than local cache
        // we read this cache when having a miss in local cache
        // writes to local cache will update shared cache as well.
        private static readonly (int HashCode, SharedEntryValue Entry)[] s_sharedTable = new (int HashCode, SharedEntryValue Entry)[SharedSize];

        // store a reference to shared cache locally
        // accessing a static field of a generic type could be nontrivial
        private readonly (int HashCode, SharedEntryValue Entry)[] _sharedTableInst = s_sharedTable;

        private readonly StringTable _strings;

        // random - used for selecting a victim in the shared cache.
        // TODO: consider whether a counter is random enough
        private Random? _random;

        internal TextKeyedCache() :
            this(null)
        {
        }

        // implement Poolable object pattern
        #region "Poolable"

        private TextKeyedCache(ObjectPool<TextKeyedCache<T>>? pool)
        {
            _pool = pool;
            _strings = new StringTable();
        }

        private readonly ObjectPool<TextKeyedCache<T>>? _pool;
        private static readonly ObjectPool<TextKeyedCache<T>> s_staticPool = CreatePool();

        private static ObjectPool<TextKeyedCache<T>> CreatePool()
        {
            var pool = new ObjectPool<TextKeyedCache<T>>(
                pool => new TextKeyedCache<T>(pool),
                Environment.ProcessorCount * 4);
            return pool;
        }

        public static TextKeyedCache<T> GetInstance()
        {
            return s_staticPool.Allocate();
        }

        public void Free()
        {
            // leave cache content in the cache, just return it to the pool
            // Array.Clear(this.localTable, 0, this.localTable.Length);
            // Array.Clear(sharedTable, 0, sharedTable.Length);

            _pool?.Free(this);
        }

        #endregion // Poolable

        /// <summary>
        /// Legacy entrypoint for VB.
        /// </summary>
        internal T? FindItem(char[] chars, int start, int len, int hashCode)
            => FindItem(chars.AsSpan(start, len), hashCode);

        internal T? FindItem(ReadOnlySpan<char> chars, int hashCode)
        {
            // get direct element reference to avoid extra range checks
            ref var localSlot = ref _localTable[LocalIdxFromHash(hashCode)];

            var text = localSlot.Text;

            if (text != null && localSlot.HashCode == hashCode)
            {
                if (StringTable.TextEquals(text, chars))
                {
                    return localSlot.Item;
                }
            }

            SharedEntryValue? e = FindSharedEntry(chars, hashCode);
            if (e != null)
            {
                localSlot.HashCode = hashCode;
                localSlot.Text = e.Text;

                var tk = e.Item;
                localSlot.Item = tk;

                return tk;
            }

            return null!;
        }

        private SharedEntryValue? FindSharedEntry(ReadOnlySpan<char> chars, int hashCode)
        {
            var arr = _sharedTableInst;
            int idx = SharedIdxFromHash(hashCode);

            SharedEntryValue? e = null;
            int hash;

            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                (hash, e) = arr[idx];

                if (e != null)
                {
                    if (hash == hashCode && StringTable.TextEquals(e.Text, chars))
                    {
                        break;
                    }

                    // this is not e we are looking for
                    e = null;
                }
                else
                {
                    // once we see unfilled entry, the rest of the bucket will be empty
                    break;
                }

                idx = (idx + i) & SharedSizeMask;
            }

            return e;
        }

        /// <summary>
        /// Legacy entrypoint for VB.
        /// </summary>
        internal void AddItem(char[] chars, int start, int len, int hashCode, T item)
            => AddItem(chars.AsSpan(start, len), hashCode, item);

        internal void AddItem(ReadOnlySpan<char> chars, int hashCode, T item)
        {
            var text = _strings.Add(chars);

            // add to the shared table first (in case someone looks for same item)
            var e = new SharedEntryValue(text, item);
            AddSharedEntry(hashCode, e);

            // add to the local table too
            ref var localSlot = ref _localTable[LocalIdxFromHash(hashCode)];
            localSlot.HashCode = hashCode;
            localSlot.Text = text;
            localSlot.Item = item;
        }

        private void AddSharedEntry(int hashCode, SharedEntryValue e)
        {
            var arr = _sharedTableInst;
            int idx = SharedIdxFromHash(hashCode);

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Entry == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            // or pick a random victim within the bucket range
            // and replace with new entry
            var i1 = NextRandom() & SharedBucketSizeMask;
            idx = (idx + ((i1 * i1 + i1) / 2)) & SharedSizeMask;

foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Entry, e);
        }

        private static int LocalIdxFromHash(int hash)
        {
            return hash & LocalSizeMask;
        }

        private static int SharedIdxFromHash(int hash)
        {
            // we can afford to mix some more hash bits here
            return (hash ^ (hash >> LocalSizeBits)) & SharedSizeMask;
        }

        private int NextRandom()
        {
            var r = _random;
            if (r != null)
            {
                return r.Next();
            }

            r = new Random();
            _random = r;
            return r.Next();
        }
    }
}
