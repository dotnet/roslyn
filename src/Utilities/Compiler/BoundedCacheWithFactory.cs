// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

#pragma warning disable CA1000 // Do not declare static members on generic types

namespace Analyzer.Utilities
{
    /// <summary>
    /// Provides bounded static cache for analyzers.
    /// Acts as a good alternative to <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey, TValue}"/>
    /// when the cached value has a cyclic reference to the key preventing early garbage collection of entries.
    /// </summary>
    internal static class BoundedCacheWithFactory<TKey, TValue>
        where TKey : class
    {
        // Bounded weak reference cache.
        // Size 5 is an arbitrarily chosen bound, which can be tuned in future as required.
        private static readonly WeakReference<Entry>[] s_weakReferencedEntries
            = new[] {
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
                new WeakReference<Entry>(null),
            };

        public static TValue GetOrCreateValue(TKey key, Func<TKey, TValue> valueFactory)
        {
            var indexToSetTarget = -1;
            for (var i = 0; i < s_weakReferencedEntries.Length; i++)
            {
                var weakReferencedEntry = s_weakReferencedEntries[i];
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
                    return cachedEntry.Value;
                }
            }

            if (indexToSetTarget == -1)
            {
                indexToSetTarget = 0;
            }

            var newEntry = new Entry(key, valueFactory(key));
            s_weakReferencedEntries[indexToSetTarget].SetTarget(newEntry);
            return newEntry.Value;
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
