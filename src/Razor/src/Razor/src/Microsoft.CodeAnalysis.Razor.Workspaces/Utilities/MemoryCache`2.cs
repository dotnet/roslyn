// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Razor.Utilities;

/// <summary>
///  A thread-safe, size-limited cache with approximate LRU (Least Recently Used)
///  eviction policy. When the cache reaches its size limit, it removes approximately
///  half of the least recently used entries.
/// </summary>
/// <typeparam name="TKey">The type of keys in the cache.</typeparam>
/// <typeparam name="TValue">The type of values in the cache.</typeparam>
/// <param name="sizeLimit">The maximum number of entries the cache can hold before compaction is triggered.</param>
/// <param name="concurrencyLevel">The estimated number of threads that will update the cache concurrently.</param>
internal sealed partial class MemoryCache<TKey, TValue>(int sizeLimit = 50, int concurrencyLevel = 2)
    where TKey : notnull
    where TValue : class
{
    private readonly ConcurrentDictionary<TKey, Entry> _map = new(concurrencyLevel, capacity: sizeLimit);

    /// <summary>
    ///  Lock used to synchronize cache compaction operations. This prevents multiple threads
    ///  from attempting to compact the cache simultaneously while allowing concurrent reads.
    /// </summary>
    private readonly object _compactLock = new();
    private readonly int _sizeLimit = sizeLimit;

    /// <summary>
    ///  Optional callback invoked after cache compaction completes. Only used by tests.
    /// </summary>
    private Action? _compactedHandler;

    /// <summary>
    ///  Attempts to retrieve a value from the cache and updates its last access time if found.
    /// </summary>
    public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? result)
    {
        if (_map.TryGetValue(key, out var entry))
        {
            entry.UpdateLastAccess();
            result = entry.Value;
            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    ///  Adds or updates a value in the cache. If the cache is at capacity, triggers compaction
    ///  before adding the new entry.
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        CompactIfNeeded();

        _map[key] = new Entry(value);
    }

    /// <summary>
    ///  Removes approximately half of the least recently used entries when the cache reaches capacity.
    /// </summary>
    private void CompactIfNeeded()
    {
        // Fast path: check size without locking
        if (_map.Count < _sizeLimit)
        {
            return;
        }

        lock (_compactLock)
        {
            // Double-check after acquiring lock in case another thread already compacted
            if (_map.Count < _sizeLimit)
            {
                return;
            }

            // Create a snapshot with last access times to implement approximate LRU eviction.
            // This captures each entry's access time to determine which entries were least recently used.
            var orderedItems = _map.ToArray().SelectAndOrderByAsArray(
                selector: static x => (x.Key, x.Value.LastAccess),
                keySelector: static x => x.LastAccess);

            var toRemove = Math.Max(_sizeLimit / 2, 1);

            // Remove up to half of the oldest entries using an atomic remove-then-check pattern.
            // This ensures we don't remove entries that were accessed after our snapshot was taken.
            foreach (var (itemKey, itemLastAccess) in orderedItems)
            {
                // Atomic remove-then-check pattern eliminates race conditions
                // Note: If TryRemove fails, another thread already removed this entry.
                if (_map.TryRemove(itemKey, out var removedEntry))
                {
                    if (removedEntry.LastAccess == itemLastAccess)
                    {
                        // Entry was still old when removed - successful eviction
                        toRemove--;

                        // Stop early if we've removed enough entries
                        if (toRemove == 0)
                        {
                            break;
                        }
                    }
                    else
                    {
                        // Entry was accessed after snapshot - try to restore it
                        // If TryAdd fails, another thread already added a new entry with this key,
                        // which is acceptable - we preserve the hot entry's data either way
                        _map.TryAdd(itemKey, removedEntry);
                    }
                }
            }

            _compactedHandler?.Invoke();
        }
    }

    /// <summary>
    ///  Removes all entries from the cache.
    /// </summary>
    public void Clear()
        => _map.Clear();
}
