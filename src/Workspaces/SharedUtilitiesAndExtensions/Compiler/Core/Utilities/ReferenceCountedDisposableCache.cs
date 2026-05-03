// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities;

/// <summary>
/// Implements a reference-counted cache, where key/value pairs are associated with a count. When the count of a pair goes to zero,
/// the value is evicted. Values can also be explicitly evicted at any time. In that case, any new calls to <see cref="GetOrCreate"/>
/// will return a new value, and the existing holders of the evicted value will still dispose it once they're done with it.
/// </summary>
internal sealed class ReferenceCountedDisposableCache<TKey, TValue> where TValue : class, IDisposable
    where TKey : notnull
{
    private readonly Dictionary<TKey, ReferenceCountedDisposable<Entry>.WeakReference> _cache;
    private readonly object _gate = new();

    public ReferenceCountedDisposableCache()
    {
        _cache = [];
    }

    public ReferenceCountedDisposableCache(IEqualityComparer<TKey> comparer)
    {
        _cache = new(comparer);
    }

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

    private sealed class Entry(ReferenceCountedDisposableCache<TKey, TValue> cache, TKey key, TValue value) : IDisposable, ICacheEntry<TKey, TValue>
    {
        public TKey Key { get; } = key;
        public TValue Value { get; } = value;

        public void Dispose()
        {
            // Evict us out of the cache. We already know that cache entry is going to be expired: any further calls on the WeakReference would give nothing,
            // but we don't want to be holding onto the key either.
            cache.Evict(Key);

            // Dispose the underlying value
            Value.Dispose();
        }
    }

    internal static class TestAccessor
    {
        public static IEnumerable<TKey> GetCacheKeys(ReferenceCountedDisposableCache<TKey, TValue> cache)
        {
            lock (cache._gate)
            {
                return [.. cache._cache.Keys];
            }
        }
    }
}
