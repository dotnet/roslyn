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
    internal class ConcurrentCache<TKey, TValue> :
        CachingBase<ConcurrentCache<TKey, TValue>.Entry> where TKey : IEquatable<TKey>
    {
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

        public ConcurrentCache(int size)
            : base(size)
        {
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = key.GetHashCode();
            var idx = hash & mask;

            var entry = this.entries[idx];
            if (entry != null && entry.hash == hash && entry.key.Equals(key))
            {
                return false;
            }

            entries[idx] = new Entry(hash, key, value);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int hash = key.GetHashCode();
            int idx = hash & mask;

            var entry = this.entries[idx];
            if (entry != null && entry.hash == hash && entry.key.Equals(key))
            {
                value = entry.value;
                return true;
            }

            value = default(TValue);
            return false;
        }
    }
}
