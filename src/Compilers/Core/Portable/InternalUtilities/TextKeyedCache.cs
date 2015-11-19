// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace Roslyn.Utilities
{
    internal class TextKeyedCache<T> where T : class
    {
        // entry in the local cache
        private struct LocalEntry
        {
            // full text of the item
            public string Text;

            // hash code of the entry
            public int HashCode;

            // item
            public T Item;
        }

        // entry in the shared cache
        private struct SharedEntry
        {
            public int HashCode;
            public SharedEntryValue Entry;
        }

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
        private readonly LocalEntry[] _localTable = new LocalEntry[LocalSize];

        // shared threadsafe cache
        // slightly slower than local cache
        // we read this cache when having a miss in local cache
        // writes to local cache will update shared cache as well.
        private static readonly SharedEntry[] s_sharedTable = new SharedEntry[SharedSize];

        // store a reference to shared cache locally
        // accessing a static field of a generic type could be nontrivial
        private readonly SharedEntry[] _sharedTableInst = s_sharedTable;

        private readonly StringTable _strings;

        // random - used for selecting a victim in the shared cache.
        // TODO: consider whether a counter is random enough
        private Random _random;

        internal TextKeyedCache() :
            this(null)
        {
        }

        // implement Poolable object pattern
        #region "Poolable"

        private TextKeyedCache(ObjectPool<TextKeyedCache<T>> pool)
        {
            _pool = pool;
            _strings = new StringTable();
        }

        private readonly ObjectPool<TextKeyedCache<T>> _pool;
        private static readonly ObjectPool<TextKeyedCache<T>> s_staticPool = CreatePool();

        private static ObjectPool<TextKeyedCache<T>> CreatePool()
        {
            ObjectPool<TextKeyedCache<T>> pool = null;
            pool = new ObjectPool<TextKeyedCache<T>>(() => new TextKeyedCache<T>(pool), Environment.ProcessorCount * 4);
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

            _pool.Free(this);
        }

        #endregion // Poolable

        internal T FindItem(char[] chars, int start, int len, int hashCode)
        {
            // capture array to avoid extra range checks
            var arr = _localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                if (StringTable.TextEquals(text, chars, start, len))
                {
                    return arr[idx].Item;
                }
            }

            SharedEntryValue e = FindSharedEntry(chars, start, len, hashCode);
            if (e != null)
            {
                // PERF: the following code does element-wise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new LocalEntry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = e.Text;

                var tk = e.Item;
                arr[idx].Item = tk;

                return tk;
            }

            return null;
        }

        private SharedEntryValue FindSharedEntry(char[] chars, int start, int len, int hashCode)
        {
            var arr = _sharedTableInst;
            int idx = SharedIdxFromHash(hashCode);

            SharedEntryValue e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Entry;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && StringTable.TextEquals(e.Text, chars, start, len))
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

        internal void AddItem(char[] chars, int start, int len, int hashCode, T item)
        {
            var text = _strings.Add(chars, start, len);

            // add to the shared table first (in case someone looks for same item)
            var e = new SharedEntryValue(text, item);
            AddSharedEntry(hashCode, e);

            // add to the local table too
            var arr = _localTable;
            var idx = LocalIdxFromHash(hashCode);
            arr[idx].HashCode = hashCode;
            arr[idx].Text = text;
            arr[idx].Item = item;
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
