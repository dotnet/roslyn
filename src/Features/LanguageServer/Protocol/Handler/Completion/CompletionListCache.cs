// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Completion;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Caches completion lists in between calls to CompletionHandler and
    /// CompletionResolveHandler. Used to avoid unnecessary recomputation.
    /// </summary>
    internal class CompletionListCache
    {
        /// <summary>
        /// Maximum number of completion lists allowed in cache. Must be >= 1.
        /// </summary>
        private const int MaxCacheSize = 3;

        /// <summary>
        /// Multiple cache requests or updates may be received concurrently.
        /// We need this lock to ensure that we aren't making concurrent
        /// modifications to _nextResultId or _resultIdToCompletionList.
        /// </summary>
        private readonly object _accessLock = new object();

        #region protected by _accessLock
        /// <summary>
        /// The next resultId available to use.
        /// </summary>
        private long _nextResultId;

        /// <summary>
        /// Keeps track of the resultIds in the cache and their associated
        /// completion list.
        /// </summary>
        private readonly List<CacheEntry> _resultIdToCompletionList = new();
        #endregion

        /// <summary>
        /// Adds a completion list to the cache. If the cache reaches its maximum size, the oldest completion
        /// list in the cache is removed.
        /// </summary>
        /// <returns>
        /// The generated resultId associated with the passed in completion list.
        /// </returns>
        public long UpdateCache(LSP.TextDocumentIdentifier textDocument, CompletionList completionList)
        {
            lock (_accessLock)
            {
                // If cache exceeds maximum size, remove the oldest list in the cache
                if (_resultIdToCompletionList.Count >= MaxCacheSize)
                {
                    _resultIdToCompletionList.RemoveAt(0);
                }

                // Getting the generated unique resultId
                var resultId = _nextResultId++;

                // Add passed in completion list to cache
                var cacheEntry = new CacheEntry(resultId, textDocument, completionList);
                _resultIdToCompletionList.Add(cacheEntry);

                // Return generated resultId so completion list can later be retrieved from cache
                return resultId;
            }
        }

        /// <summary>
        /// Attempts to return the completion list in the cache associated with the given resultId.
        /// Returns null if no match is found.
        /// </summary>
        public CacheEntry? GetCachedCompletionList(long resultId)
        {
            lock (_accessLock)
            {
                foreach (var cacheEntry in _resultIdToCompletionList)
                {
                    if (cacheEntry.ResultId == resultId)
                    {
                        // We found a match - return completion list
                        return cacheEntry;
                    }
                }

                // A completion list associated with the given resultId was not found
                return null;
            }
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionListCache _completionListCache;

            public static int MaximumCacheSize => MaxCacheSize;

            public TestAccessor(CompletionListCache completionListCache)
                => _completionListCache = completionListCache;

            public List<CacheEntry> GetCacheContents()
                => _completionListCache._resultIdToCompletionList;
        }

        public record CacheEntry(long ResultId, LSP.TextDocumentIdentifier TextDocument, CompletionList CompletionList);
    }
}
