// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    // Implements simple cache of limited size that could hold 
    // a number of previously created/mapped items.
    //
    // These caches do not grow or shrink and need no rehashing
    // Maximum size of a cache is set at construction.
    // Items are inserted at locations that correspond to the hash code of the item
    // New item displaces anything that previously used the same slot.
    // 
    // Cache needs to know 3 functions: 
    //  keyHash - maps a key to a hashcode. 
    //
    //  keyValueEquality - compares key and a value and figures if the value could have been created using same key.
    //                  NOTE: it does not compare two keys.
    //                  The assumption is that value's key could be inferred from the value so we do not want to store it.
    //                  We also do not want to pass in the new value as the whole purpose of the cache is to avoid creating
    //                  a new instance if cached one can be used.
    //                
    //  valueFactory - creates a new value from a key. Needed only in GetOrMakeValue.
    //                  in a case where it is not possible to create a static valueFactory, it is advisable
    //                  to set valueFactory to null and use TryGetValue/Add pattern instead of GetOrMakeValue.
    //
    internal class CachingFactory<TKey, TValue> : CachingBase<CachingFactory<TKey, TValue>.Entry>
        where TKey : notnull
    {
        internal struct Entry
        {
            internal int hash;
            internal TValue value;
        }

        private readonly int _size;
        private readonly Func<TKey, TValue> _valueFactory;
        private readonly Func<TKey, int> _keyHash;
        private readonly Func<TKey, TValue, bool> _keyValueEquality;

        public CachingFactory(int size,
                Func<TKey, TValue> valueFactory,
                Func<TKey, int> keyHash,
                Func<TKey, TValue, bool> keyValueEquality) :
            base(size)
        {
            _size = size;
            _valueFactory = valueFactory;
            _keyHash = keyHash;
            _keyValueEquality = keyValueEquality;
        }

        public void Add(TKey key, TValue value)
        {
            var hash = GetKeyHash(key);
            var idx = hash & mask;

            Entries[idx].hash = hash;
            Entries[idx].value = value;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            int hash = GetKeyHash(key);
            int idx = hash & mask;

            var entries = this.Entries;
            if (entries[idx].hash == hash)
            {
                var candidate = entries[idx].value;
                if (_keyValueEquality(key, candidate))
                {
                    value = candidate;
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public TValue GetOrMakeValue(TKey key)
        {
            int hash = GetKeyHash(key);
            int idx = hash & mask;

            var entries = this.Entries;
            if (entries[idx].hash == hash)
            {
                var candidate = entries[idx].value;
                if (_keyValueEquality(key, candidate))
                {
                    return candidate;
                }
            }

            var value = _valueFactory(key);
            entries[idx].hash = hash;
            entries[idx].value = value;

            return value;
        }

        private int GetKeyHash(TKey key)
        {
            // Ensure result is non-zero to avoid
            // treating an empty entry as valid.
            int result = _keyHash(key) | _size;
            Debug.Assert(result != 0);
            return result;
        }
    }

    // special case for a situation where the key is a reference type with object identity 
    // in this case:
    //      keyHash             is assumed to be RuntimeHelpers.GetHashCode
    //      keyValueEquality    is an object == for the new and old keys 
    //                          NOTE: we do store the key in this case 
    //                          reference comparison of keys is as cheap as comparing hash codes.
    internal class CachingIdentityFactory<TKey, TValue> : CachingBase<CachingIdentityFactory<TKey, TValue>.Entry>
        where TKey : class
    {
        private readonly Func<TKey, TValue> _valueFactory;
        private readonly ObjectPool<CachingIdentityFactory<TKey, TValue>>? _pool;

        internal struct Entry
        {
            internal TKey key;
            internal TValue value;
        }

        public CachingIdentityFactory(int size, Func<TKey, TValue> valueFactory) :
            base(size)
        {
            _valueFactory = valueFactory;
        }

        public CachingIdentityFactory(int size, Func<TKey, TValue> valueFactory, ObjectPool<CachingIdentityFactory<TKey, TValue>> pool) :
            this(size, valueFactory)
        {
            _pool = pool;
        }

        public void Add(TKey key, TValue value)
        {
            var hash = RuntimeHelpers.GetHashCode(key);
            var idx = hash & mask;

            Entries[idx].key = key;
            Entries[idx].value = value;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(returnValue: false)] out TValue value)
        {
            int hash = RuntimeHelpers.GetHashCode(key);
            int idx = hash & mask;

            var entries = this.Entries;
            if ((object)entries[idx].key == (object)key)
            {
                value = entries[idx].value;
                return true;
            }

            value = default!;
            return false;
        }

        public TValue GetOrMakeValue(TKey key)
        {
            int hash = RuntimeHelpers.GetHashCode(key);
            int idx = hash & mask;

            var entries = this.Entries;
            if ((object)entries[idx].key == (object)key)
            {
                return entries[idx].value;
            }

            var value = _valueFactory(key);
            entries[idx].key = key;
            entries[idx].value = value;

            return value;
        }

        // if someone needs to create a pool;
        public static ObjectPool<CachingIdentityFactory<TKey, TValue>> CreatePool(int size, Func<TKey, TValue> valueFactory)
        {
            var pool = new ObjectPool<CachingIdentityFactory<TKey, TValue>>(
                pool => new CachingIdentityFactory<TKey, TValue>(size, valueFactory, pool),
                Environment.ProcessorCount * 2);

            return pool;
        }

        public void Free()
        {
            var pool = _pool;

            // Array.Clear(this.entries, 0, this.entries.Length);

            pool?.Free(this);
        }
    }

    // Just holds the data for the derived caches.
    internal abstract class CachingBase<TEntry>
    {
        private readonly int _alignedSize;
        private TEntry[]? _entries;

        // cache size is always ^2. 
        // items are placed at [hash ^ mask]
        // new item will displace previous one at the same location.
        protected readonly int mask;

        // See docs for createBackingArray on the constructor for why using the non-threadsafe ??= is ok here.
        protected TEntry[] Entries => _entries ??= new TEntry[_alignedSize];

        /// <param name="createBackingArray">Whether or not the backing array should be created immediately, or should
        /// be deferred until the first time that <see cref="Entries"/> is used.  Note: if <paramref
        /// name="createBackingArray"/> is <see langword="false"/> then the array will be created in a non-threadsafe
        /// fashion (effectively different threads might observe a small window of time when different arrays could be
        /// returned.  Derived types should only pass <see langword="false"/> here if that behavior is acceptable for
        /// their use case.</param>
        internal CachingBase(int size, bool createBackingArray = true)
        {
            _alignedSize = AlignSize(size);
            this.mask = _alignedSize - 1;
            _entries = createBackingArray ? new TEntry[_alignedSize] : null;
        }

        private static int AlignSize(int size)
        {
            Debug.Assert(size > 0);

            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            return size + 1;
        }
    }
}
