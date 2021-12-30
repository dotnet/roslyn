﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Implements a reference-counted cache, where key/value pairs are associated with a count. When the count of a pair goes to zero,
    /// the value is evicted. Values can also be explicitly evicted at any time. In that case, any new calls to <see cref="GetOrCreate"/>
    /// will return a new value, and the existing holders of the evicted value will still dispose it once they're done with it.
    /// </summary>
    internal sealed class ReferenceCountedDisposableCache<TKey, TValue> where TValue : class, IDisposable
        where TKey : notnull
    {
        private readonly Dictionary<TKey, ReferenceCountedDisposable<Entry>.WeakReference> _cache = new();
        private readonly object _gate = new();

        public IReferenceCountedDisposable<ICacheEntry<TKey, TValue>> GetOrCreate<TArg>(TKey key, Func<TKey, TArg, TValue> valueCreator, TArg arg)
        {
            lock (_gate)
            {
                ReferenceCountedDisposable<Entry>? disposable = null;

                // If we already have one in the map to hand out, great
                if (_cache.TryGetValue(key, out var weakReference))
                {
                    disposable = weakReference.TryAddReference();
                }

                if (disposable == null)
                {
                    // We didn't easily get a disposable, so one of two things is the case:
                    //
                    // 1. We have no entry in _cache at all for this.
                    // 2. We had an entry, but it was disposed and is no longer valid. This could happen if
                    //    the underlying value was disposed, but _cache hasn't been updated yet. That could happen
                    //    because the disposal isn't processed under this lock.

                    // In either case, we'll create a new entry and add it to the map
                    disposable = new ReferenceCountedDisposable<Entry>(new Entry(this, key, valueCreator(key, arg)));
                    _cache[key] = new ReferenceCountedDisposable<Entry>.WeakReference(disposable);
                }

                return disposable;
            }
        }

        public void Evict(TKey key)
        {
            lock (_gate)
            {
                _cache.Remove(key);
            }
        }

        private sealed class Entry : IDisposable, ICacheEntry<TKey, TValue>
        {
            private readonly ReferenceCountedDisposableCache<TKey, TValue> _cache;

            public TKey Key { get; }
            public TValue Value { get; }

            public Entry(ReferenceCountedDisposableCache<TKey, TValue> cache, TKey key, TValue value)
            {
                _cache = cache;
                Key = key;
                Value = value;
            }

            public void Dispose()
            {
                // Evict us out of the cache. We already know that cache entry is going to be expired: any further calls on the WeakReference would give nothing,
                // but we don't want to be holding onto the key either.
                _cache.Evict(Key);

                // Dispose the underlying value
                Value.Dispose();
            }
        }
    }
}
