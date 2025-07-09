// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Roslyn.Utilities
{
    /// <summary>
    /// This is basically a lossy cache of strings that is searchable by
    /// strings, string sub ranges, character array ranges or string-builder.
    /// </summary>
    internal class StringTable
    {
        // entry in the caches
        private struct Entry
        {
            // hash code of the entry
            public int HashCode;

            // full text of the item
            public string Text;
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

        // local (L1) cache
        // simple fast and not threadsafe cache 
        // with limited size and "last add wins" expiration policy
        //
        // The main purpose of the local cache is to use in long lived
        // single threaded operations with lots of locality (like parsing).
        // Local cache is smaller (and thus faster) and is not affected
        // by cache misses on other threads.
        private readonly Entry[] _localTable = new Entry[LocalSize];

        // shared (L2) threadsafe cache
        // slightly slower than local cache
        // we read this cache when having a miss in local cache
        // writes to local cache will update shared cache as well.
        private static readonly SegmentedArray<Entry> s_sharedTable = new SegmentedArray<Entry>(SharedSize);

        // essentially a random number 
        // the usage pattern will randomly use and increment this
        // the counter is not static to avoid interlocked operations and cross-thread traffic
        private int _localRandom = Environment.TickCount;

        // same as above but for users that go directly with unbuffered shared cache.
        private static int s_sharedRandom = Environment.TickCount;

        internal StringTable() :
            this(null)
        {
        }

        // implement Poolable object pattern
        #region "Poolable"

        private StringTable(ObjectPool<StringTable>? pool)
        {
            _pool = pool;
        }

        private readonly ObjectPool<StringTable>? _pool;
        private static readonly ObjectPool<StringTable> s_staticPool = CreatePool();

        private static ObjectPool<StringTable> CreatePool()
        {
            var pool = new ObjectPool<StringTable>(pool => new StringTable(pool), Environment.ProcessorCount * 2);
            return pool;
        }

        public static StringTable GetInstance()
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
        internal string Add(char[] chars)
            => Add(chars.AsSpan());

        /// <summary>
        /// Legacy entrypoint for VB.
        /// </summary>
        internal string Add(char[] chars, int start, int len)
            => Add(chars.AsSpan(start, len));

        internal string Add(ReadOnlySpan<char> chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            // capture array to avoid extra range checks
            var arr = _localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (TextEquals(result, chars))
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                // PERF: the following code does element-wise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, hashCode);
        }

        internal string Add(string chars, int start, int len)
            => Add(chars.AsSpan(start, len));

        internal string Add(char chars)
            => Add([chars]);

        internal string Add(StringBuilder chars)
        {
            var hashCode = Hash.GetFNVHashCode(chars);

            // capture array to avoid extra range checks
            var arr = _localTable;
            var idx = LocalIdxFromHash(hashCode);

            var text = arr[idx].Text;

            if (text != null && arr[idx].HashCode == hashCode)
            {
                var result = arr[idx].Text;
                if (StringTable.TextEquals(result, chars))
                {
                    return result;
                }
            }

            string? shared = FindSharedEntry(chars, hashCode);
            if (shared != null)
            {
                // PERF: the following code does element-wise assignment of a struct
                //       because current JIT produces better code compared to
                //       arr[idx] = new Entry(...)
                arr[idx].HashCode = hashCode;
                arr[idx].Text = shared;

                return shared;
            }

            return AddItem(chars, hashCode);
        }

        internal string Add(string chars)
            => Add(chars.AsSpan());

        private static string? FindSharedEntry(ReadOnlySpan<char> chars, int hashCode)
        {
            var arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars))
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

        private static string? FindSharedEntry(string chars, int start, int len, int hashCode)
            => FindSharedEntry(chars.AsSpan(start, len), hashCode);

        private static string? FindSharedEntryASCII(int hashCode, ReadOnlySpan<byte> asciiChars)
        {
            var arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEqualsASCII(e, asciiChars))
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

        private static string? FindSharedEntry(char chars, int hashCode)
            => FindSharedEntry([chars], hashCode);

        private static string? FindSharedEntry(StringBuilder chars, int hashCode)
        {
            var arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            string? e = null;
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                e = arr[idx].Text;
                int hash = arr[idx].HashCode;

                if (e != null)
                {
                    if (hash == hashCode && TextEquals(e, chars))
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

        private static string? FindSharedEntry(string chars, int hashCode)
            => FindSharedEntry(chars.AsSpan(), hashCode);

        private string AddItem(ReadOnlySpan<char> chars, int hashCode)
        {
            var text = chars.ToString();
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(string chars, int start, int len, int hashCode)
        {
            // Don't defer to ReadOnlySpan<char> here, as it would cause an extra allocation
            // in the case where start/len exactly match the full span of chars.

            var text = chars.Substring(start, len);
            AddCore(text, hashCode);
            return text;
        }

        private string AddItem(char chars, int hashCode)
            => AddItem([chars], hashCode);

        private string AddItem(StringBuilder chars, int hashCode)
        {
            var text = chars.ToString();
            AddCore(text, hashCode);
            return text;
        }

        private void AddCore(string chars, int hashCode)
        {
            // add to the shared table first (in case someone looks for same item)
            AddSharedEntry(hashCode, chars);

            // add to the local table too
            var arr = _localTable;
            var idx = LocalIdxFromHash(hashCode);
            arr[idx].HashCode = hashCode;
            arr[idx].Text = chars;
        }

        private void AddSharedEntry(int hashCode, string text)
        {
            var arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            // or pick a random victim within the bucket range
            // and replace with new entry
            var i1 = LocalNextRandom() & SharedBucketSizeMask;
            idx = (idx + ((i1 * i1 + i1) / 2)) & SharedSizeMask;

foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);
        }

        private static string AddSharedSlow(int hashCode, StringBuilder builder)
        {
            string text = builder.ToString();
            AddSharedSlow(hashCode, text);
            return text;
        }

        internal static string AddSharedUtf8(ReadOnlySpan<byte> bytes)
        {
            int hashCode = Hash.GetFNVHashCode(bytes, out bool isAscii);

            if (isAscii)
            {
                string? shared = FindSharedEntryASCII(hashCode, bytes);
                if (shared != null)
                {
                    return shared;
                }
            }

            return AddSharedSlow(hashCode, bytes, isAscii);
        }

        private static string AddSharedSlow(int hashCode, ReadOnlySpan<byte> utf8Bytes, bool isAscii)
        {
            string text;

            unsafe
            {
                fixed (byte* bytes = &utf8Bytes.GetPinnableReference())
                {
                    text = Encoding.UTF8.GetString(bytes, utf8Bytes.Length);
                }
            }

            // Don't add non-ascii strings to table. The hashCode we have here is not correct and we won't find them again.
            // Non-ascii in UTF-8 encoded parts of metadata (the only use of this at the moment) is assumed to be rare in 
            // practice. If that turns out to be wrong, we could decode to pooled memory and rehash here.
            if (isAscii)
            {
                AddSharedSlow(hashCode, text);
            }

            return text;
        }

        private static void AddSharedSlow(int hashCode, string text)
        {
            var arr = s_sharedTable;
            int idx = SharedIdxFromHash(hashCode);

            // try finding an empty spot in the bucket
            // we use quadratic probing here
            // bucket positions are (n^2 + n)/2 relative to the masked hashcode
            int curIdx = idx;
            for (int i = 1; i < SharedBucketSize + 1; i++)
            {
                if (arr[curIdx].Text == null)
                {
                    idx = curIdx;
                    goto foundIdx;
                }

                curIdx = (curIdx + i) & SharedSizeMask;
            }

            // or pick a random victim within the bucket range
            // and replace with new entry
            var i1 = SharedNextRandom() & SharedBucketSizeMask;
            idx = (idx + ((i1 * i1 + i1) / 2)) & SharedSizeMask;

foundIdx:
            arr[idx].HashCode = hashCode;
            Volatile.Write(ref arr[idx].Text, text);
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

        private int LocalNextRandom()
        {
            return _localRandom++;
        }

        private static int SharedNextRandom()
        {
            return Interlocked.Increment(ref StringTable.s_sharedRandom);
        }

        internal static bool TextEquals(string array, string text, int start, int length)
        {
            if (array.Length != length)
            {
                return false;
            }

            // use array.Length to eliminate the range check
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] != text[start + i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TextEquals(string array, StringBuilder text)
        {
            if (array.Length != text.Length)
            {
                return false;
            }

#if NETCOREAPP3_1_OR_GREATER
            int chunkOffset = 0;
            foreach (var chunk in text.GetChunks())
            {
                if (!chunk.Span.Equals(array.AsSpan().Slice(chunkOffset, chunk.Length), StringComparison.Ordinal))
                    return false;

                chunkOffset += chunk.Length;
            }
#else
            // interestingly, stringbuilder holds the list of chunks by the tail
            // so accessing positions at the beginning may cost more than those at the end.
            for (var i = array.Length - 1; i >= 0; i--)
            {
                if (array[i] != text[i])
                {
                    return false;
                }
            }
#endif

            return true;
        }

        internal static bool TextEqualsASCII(string text, ReadOnlySpan<byte> ascii)
        {
#if DEBUG
            for (var i = 0; i < ascii.Length; i++)
            {
                RoslynDebug.Assert((ascii[i] & 0x80) == 0, $"The {nameof(ascii)} input to this method must be valid ASCII.");
            }
#endif

            if (ascii.Length != text.Length)
            {
                return false;
            }

            for (var i = 0; i < ascii.Length; i++)
            {
                if (ascii[i] != text[i])
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TextEquals(string array, ReadOnlySpan<char> text)
            => text.Equals(array.AsSpan(), StringComparison.Ordinal);
    }
}
