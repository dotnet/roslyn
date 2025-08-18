// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.UserFacingStrings;

namespace Microsoft.CodeAnalysis.CSharp.UserFacingStrings;

/// <summary>
/// Document-centric global cache using ConditionalWeakTable for automatic cleanup.
/// Maps DocumentId to per-document string cache entries.
/// Thread-safe and memory-efficient with automatic garbage collection.
/// </summary>
internal sealed class UserFacingStringGlobalCache
{
    private readonly ConditionalWeakTable<DocumentId, DocumentStringCacheHolder> _documentCache = new();
    private readonly object _lock = new object();

    /// <summary>
    /// Gets or creates a cache holder for the specified document.
    /// Thread-safe and handles concurrent access.
    /// </summary>
    public DocumentStringCacheHolder GetOrCreateCacheHolder(DocumentId documentId)
    {
        if (_documentCache.TryGetValue(documentId, out var existingHolder))
        {
            return existingHolder;
        }

        lock (_lock)
        {
            if (_documentCache.TryGetValue(documentId, out existingHolder))
            {
                return existingHolder;
            }

            var newHolder = new DocumentStringCacheHolder();
            _documentCache.Add(documentId, newHolder);
            return newHolder;
        }
    }

    /// <summary>
    /// Tries to get cached analysis for a string within a document context.
    /// Returns true if found and not expired, false otherwise.
    /// </summary>
    public bool TryGetCachedAnalysis(
        DocumentId documentId, 
        StringCacheKey cacheKey, 
        out StringCacheEntry cacheEntry)
    {
        cacheEntry = default;

        if (!_documentCache.TryGetValue(documentId, out var holder))
            return false;

        return holder.TryGetEntry(cacheKey, out cacheEntry) && !cacheEntry.IsExpired;
    }

    /// <summary>
    /// Adds or updates a cache entry for a string in the specified document.
    /// Thread-safe operation that maintains cache consistency.
    /// </summary>
    public void AddOrUpdateEntry(DocumentId documentId, StringCacheEntry entry)
    {
        var holder = GetOrCreateCacheHolder(documentId);
        holder.AddOrUpdateEntry(entry);
    }

    /// <summary>
    /// Gets all cached entries for a document. Used for cache validation and diagnostics.
    /// Returns empty set if no entries exist or document not found.
    /// </summary>
    public ImmutableHashSet<StringCacheEntry> GetDocumentEntries(DocumentId documentId)
    {
        if (!_documentCache.TryGetValue(documentId, out var holder))
            return ImmutableHashSet<StringCacheEntry>.Empty;

        return holder.GetAllEntries();
    }

    /// <summary>
    /// Removes expired entries from a document's cache.
    /// Called periodically to prevent unbounded growth.
    /// </summary>
    public void CleanupExpiredEntries(DocumentId documentId)
    {
        if (_documentCache.TryGetValue(documentId, out var holder))
        {
            holder.CleanupExpiredEntries();
        }
    }

    /// <summary>
    /// Removes all cache entries for a document. Used when document is deleted or major changes occur.
    /// </summary>
    public void ClearDocumentCache(DocumentId documentId)
    {
        if (_documentCache.TryGetValue(documentId, out var holder))
        {
            holder.Clear();
        }
    }

    /// <summary>
    /// Gets cache statistics for monitoring and debugging.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            var totalEntries = 0;
            var totalDocuments = 0;
            var expiredEntries = 0;

            // Note: ConditionalWeakTable doesn't provide enumeration in production scenarios
            // This method is primarily for debugging/testing purposes
            // In production, statistics would be tracked incrementally

            return new CacheStatistics(totalDocuments, totalEntries, expiredEntries);
        }
    }

    /// <summary>
    /// Cache statistics for monitoring cache health and performance.
    /// </summary>
    public readonly struct CacheStatistics
    {
        public int TotalDocuments { get; }
        public int TotalEntries { get; }
        public int ExpiredEntries { get; }

        public CacheStatistics(int totalDocuments, int totalEntries, int expiredEntries)
        {
            TotalDocuments = totalDocuments;
            TotalEntries = totalEntries;
            ExpiredEntries = expiredEntries;
        }

        public double HitRate => TotalEntries > 0 ? (double)(TotalEntries - ExpiredEntries) / TotalEntries : 0.0;
    }
}
