// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Caches completion lists in between calls to CompletionHandler and
    /// CompletionResolveHandler. Used to avoid unnecessary recomputation.
    /// </summary>
    [Export(typeof(CompletionListCache)), Shared]
    internal class CompletionListCache
    {
        /// <summary>
        /// Maximum number of completion lists allowed in cache. Must be >= 1.
        /// </summary>
        private const int MaxCacheSize = 3;

        /// <summary>
        /// Multiple cache requests or updates may be received concurrently.
        /// We need this sempahore to ensure that we aren't making concurrent
        /// modifications to _nextResultId or the _resultIdToCompletionList
        /// dictionary.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new(1);

        #region protected by _semaphore
        /// <summary>
        /// The next resultId available to use.
        /// </summary>
        private long _nextResultId;

        /// <summary>
        /// Maps a resultId to its completion list.
        /// </summary>
        private readonly Dictionary<long, CompletionList> _resultIdToCompletionList = new();
        #endregion

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionListCache()
        {
        }

        /// <summary>
        /// Adds a completion list to the cache. If the cache reaches its maximum size, the oldest completion
        /// list in the cache is removed.
        /// </summary>
        /// <returns>
        /// The generated resultId associated with the passed in completion list.
        /// </returns>
        internal async Task<long> UpdateCacheAsync(CompletionList completionList, CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Getting the generated unique resultId
                var resultId = _nextResultId++;

                // If cache exceeds maximum size, remove the oldest list in the cache
                if (_resultIdToCompletionList.Count >= MaxCacheSize)
                {
                    var oldestCachedResultId = resultId - MaxCacheSize;
                    _resultIdToCompletionList.Remove(oldestCachedResultId);
                }

                // Add passed in completion list to cache
                _resultIdToCompletionList[resultId] = completionList;

                // Return generated resultId so completion list can later be retrieved from cache
                return resultId;
            }
        }

        /// <summary>
        /// Attempts to return the completion list in the cache associated with the given resultId.
        /// Returns null if no match is found.
        /// </summary>
        internal async Task<CompletionList?> GetCachedCompletionListAsync(long resultId, CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // A completion list associated with the given resultId was not found
                if (!_resultIdToCompletionList.ContainsKey(resultId))
                {
                    return null;
                }

                // We found a match - return completion list
                return _resultIdToCompletionList[resultId];
            }
        }

        internal TestAccessor GetTestAccessor() => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CompletionListCache _completionListCache;

            public static int MaximumCacheSize => MaxCacheSize;

            public TestAccessor(CompletionListCache completionListCache)
                => _completionListCache = completionListCache;

            public Dictionary<long, CompletionList> GetCacheContents()
                => _completionListCache._resultIdToCompletionList;
        }
    }
}
