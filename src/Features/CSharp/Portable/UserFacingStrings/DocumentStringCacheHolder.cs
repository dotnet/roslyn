// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Per-document cache holder that maintains string analysis results using ImmutableHashSet.
/// Thread-safe operations with copy-on-write semantics for high concurrency scenarios.
/// Automatically handles expiration and cleanup of stale entries.
/// </summary>
internal sealed class DocumentStringCacheHolder
{
    private volatile ImmutableHashSet<StringCacheEntry> _entries = ImmutableHashSet<StringCacheEntry>.Empty;
    private readonly object _lock = new object();

    /// <summary>
    /// Tries to get a cached entry by cache key.
    /// Returns true if found, false if not found or expired.
    /// </summary>
    public bool TryGetEntry(StringCacheKey cacheKey, out StringCacheEntry entry)
    {
        var currentEntries = _entries; // Atomic read
        
        foreach (var candidate in currentEntries)
        {
            if (candidate.CacheKey.Equals(cacheKey))
            {
                if (!candidate.IsExpired)
                {
                    entry = candidate;
                    return true;
                }
                
                // Found but expired - will be cleaned up later
                break;
            }
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// Adds or updates a cache entry. Thread-safe with copy-on-write semantics.
    /// If entry already exists, it will be replaced with the new version.
    /// </summary>
    public void AddOrUpdateEntry(StringCacheEntry newEntry)
    {
        lock (_lock)
        {
            var currentEntries = _entries;
            
            // Remove existing entry with same cache key (if any)
            var updatedEntries = currentEntries.Where(e => !e.CacheKey.Equals(newEntry.CacheKey));
            
            // Add the new entry
            _entries = updatedEntries.ToImmutableHashSet().Add(newEntry);
        }
    }

    /// <summary>
    /// Gets all current cache entries. Returns a snapshot at the time of call.
    /// </summary>
    public ImmutableHashSet<StringCacheEntry> GetAllEntries()
    {
        return _entries; // Atomic read of immutable structure
    }

    /// <summary>
    /// Removes all expired entries from the cache.
    /// Called periodically to prevent unbounded growth.
    /// </summary>
    public void CleanupExpiredEntries()
    {
        lock (_lock)
        {
            var currentEntries = _entries;
            var validEntries = currentEntries.Where(e => !e.IsExpired);
            _entries = validEntries.ToImmutableHashSet();
        }
    }

    /// <summary>
    /// Clears all entries from this document's cache.
    /// Used when document is deleted or major changes require full invalidation.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _entries = ImmutableHashSet<StringCacheEntry>.Empty;
        }
    }

    /// <summary>
    /// Gets the number of cached entries (including potentially expired ones).
    /// </summary>
    public int Count => _entries.Count;

    /// <summary>
    /// Gets the number of valid (non-expired) entries.
    /// </summary>
    public int ValidCount
    {
        get
        {
            var currentEntries = _entries;
            return currentEntries.Count(e => !e.IsExpired);
        }
    }

    /// <summary>
    /// Checks if any entries exist for the given basic context type.
    /// Useful for determining if AI analysis is needed for specific contexts.
    /// </summary>
    public bool HasEntriesForContext(string contextType)
    {
        var currentEntries = _entries;
        return currentEntries.Any(e => !e.IsExpired && e.CacheKey.ContextType == contextType);
    }

    /// <summary>
    /// Gets all entries that match a specific context type.
    /// Useful for context-specific cache invalidation or analysis.
    /// </summary>
    public ImmutableArray<StringCacheEntry> GetEntriesForContext(string contextType)
    {
        var currentEntries = _entries;
        return currentEntries
            .Where(e => !e.IsExpired && e.CacheKey.ContextType == contextType)
            .ToImmutableArray();
    }
}
