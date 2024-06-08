// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// A common LSP pattern is an initial request to the server that returns some set of partially filled out items.
/// Then the client issues a xyz/resolve request to fully resolve a specific item when actually needed.
/// 
/// On the server side we often need to cache non-serializable data that can't be included in the typical
/// 'data' field on the actual item.  This type is a general cache that helps keep track of data between requests.
///
/// This cache is generally only written to as part of the initial request to store data for later resolution.
/// It is only read from as part of a resolve request for some data sent in the initial request to restore state.
/// </summary>
internal abstract class ResolveCache<TCacheEntry> : ILspService where TCacheEntry : class
{
    /// <summary>
    /// Maximum number of cache entries allowed in cache. Must be >= 1.
    /// Typically a resolve request will only ask about the most recent cache entry so
    /// it is not important to cache a lot of entries.  If there are document changes
    /// the client is responsible for not asking to resolve invalid items.
    /// </summary>
    private readonly int _maxCacheSize;

    /// <summary>
    /// Multiple cache requests or updates may be received concurrently.
    /// We need this lock to ensure that we aren't making concurrent
    /// modifications to <see cref="_nextResultId"/> or <see cref="_resultIdToCachedItem"/>
    /// </summary>
    private readonly object _accessLock = new();

    #region protected by _accessLock
    /// <summary>
    /// The next resultId available to use.
    /// </summary>
    private long _nextResultId;

    /// <summary>
    /// Keeps track of the resultIds in the cache and their associated cache entry.
    /// </summary>
    private readonly List<(long ResultId, TCacheEntry CacheEntry)> _resultIdToCachedItem = [];

    #endregion

    public ResolveCache(int maxCacheSize)
    {
        _maxCacheSize = maxCacheSize;
    }

    /// <summary>
    /// Adds a completion list to the cache. If the cache reaches its maximum size, the oldest completion
    /// list in the cache is removed.
    /// </summary>
    /// <returns>
    /// The generated resultId associated with the passed in completion list.
    /// </returns>
    public long UpdateCache(TCacheEntry cacheEntry)
    {
        lock (_accessLock)
        {
            // If cache exceeds maximum size, remove the oldest item in the cache
            if (_resultIdToCachedItem.Count >= _maxCacheSize)
            {
                _resultIdToCachedItem.RemoveAt(0);
            }

            // Getting the generated unique resultId
            var resultId = _nextResultId++;

            // Add passed in entry to cache
            _resultIdToCachedItem.Add((resultId, cacheEntry));

            // Return generated resultId so entry can later be retrieved from cache
            return resultId;
        }
    }

    /// <summary>
    /// Attempts to return the completion list in the cache associated with the given resultId.
    /// Returns null if no match is found.
    /// </summary>
    public TCacheEntry? GetCachedEntry(long resultId)
    {
        lock (_accessLock)
        {
            foreach (var item in _resultIdToCachedItem)
            {
                if (item.ResultId == resultId)
                {
                    // We found a match - return entry
                    return item.CacheEntry;
                }
            }

            // An entry associated with the given resultId was not found
            return null;
        }
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly ResolveCache<TCacheEntry> _resolveCache;

        public int MaximumCacheSize => _resolveCache._maxCacheSize;

        public TestAccessor(ResolveCache<TCacheEntry> resolveCache)
            => _resolveCache = resolveCache;

        public List<(long ResultId, TCacheEntry CacheEntry)> GetCacheContents()
            => _resolveCache._resultIdToCachedItem;
    }
}
