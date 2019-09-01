// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides bounded cache for analyzers.
    /// Acts as a good alternative to <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>
    /// when the cached value has a cyclic reference to the key preventing early garbage collection of entries.
    /// </summary>
    internal class BoundedCacheWithFactory<TKey, TValue>
        where TKey : class
    {
        // Bounded weak reference cache.
        // Size 5 is an arbitrarily chosen bound, which can be tuned in future as required.
        private readonly List<WeakReference<Entry>> _weakReferencedEntries
            = new List<WeakReference<Entry>> {
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
            };

        public TValue GetOrCreateValue(TKey key, Func<TKey, TValue> valueFactory)
        {
            lock (_weakReferencedEntries)
            {
                var indexToSetTarget = -1;
                for (var i = 0; i < _weakReferencedEntries.Count; i++)
                {
                    var weakReferencedEntry = _weakReferencedEntries[i];
                    if (!weakReferencedEntry.TryGetTarget(out var cachedEntry) ||
                        cachedEntry == null)
                    {
                        if (indexToSetTarget == -1)
                        {
                            indexToSetTarget = i;
                        }

                        continue;
                    }

                    if (Equals(cachedEntry.Key, key))
                    {
                        // Move the cache hit item to the end of the list
                        // so it would be least likely to be evicted on next cache miss.
                        _weakReferencedEntries.RemoveAt(i);
                        _weakReferencedEntries.Add(weakReferencedEntry);
                        return cachedEntry.Value;
                    }
                }

                if (indexToSetTarget == -1)
                {
                    indexToSetTarget = 0;
                }

                var newEntry = new Entry(key, valueFactory(key));
                _weakReferencedEntries[indexToSetTarget].SetTarget(newEntry);
                return newEntry.Value;
            }
        }

        private sealed class Entry
        {
            public Entry(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }

            public TKey Key { get; }

            public TValue Value { get; }
        }
    }
}
