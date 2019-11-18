// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    // very simple cache with a specified size.
    // expiration policy is "new entry wins over old entry if hashed into the same bucket"
    internal class ConcurrentCache<TKey, TValue> : CachingBase<ConcurrentCache<TKey, TValue>.Entry>
    {
        private readonly IEqualityComparer<TKey> _keyComparer;

        // class, to ensure atomic updates.
        internal class Entry
        {
            internal readonly int hash;
            internal readonly TKey key;
            internal readonly TValue value;

            internal Entry(int hash, TKey key, TValue value)
            {
                this.hash = hash;
                this.key = key;
                this.value = value;
            }
        }

        public ConcurrentCache(int size, IEqualityComparer<TKey> keyComparer)
            : base(size)
        {
            _keyComparer = keyComparer;
        }

        public ConcurrentCache(int size)
            : this(size, EqualityComparer<TKey>.Default) { }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = _keyComparer.GetHashCode(key);
            var idx = hash & mask;

            var entry = this.entries[idx];
            if (entry != null && entry.hash == hash && _keyComparer.Equals(entry.key, key))
            {
                return false;
            }

            entries[idx] = new Entry(hash, key, value);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = _keyComparer.GetHashCode(key);
            int idx = hash & mask;

            var entry = this.entries[idx];
            if (entry != null && entry.hash == hash && _keyComparer.Equals(entry.key, key))
            {
                value = entry.value;
                return true;
            }

            value = default(TValue);
            return false;
        }
    }
}
