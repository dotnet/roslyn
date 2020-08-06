// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens
{
    /// <summary>
    /// Caches the semantic token information that needs to be preserved between multiple
    /// semantic token requests.
    /// Multiple token sets can be cached per document. The number of token sets cached
    /// per document is determined by the _maxCachesPerDoc field.
    /// </summary>
    [Export(typeof(SemanticTokensCache)), Shared]
    internal class SemanticTokensCache
    {
        /// <summary>
        /// Multiple cache requests or updates may be received concurrently.
        /// We need this sempahore to ensure that we aren't making concurrent
        /// modifications to the Tokens or NextResultId dictionaries.
        /// </summary>
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        /// Number of cached token sets we store per document. Must be >= 1.
        /// </summary>
        private readonly int _maxCachesPerDoc = 5;

        /// <summary>
        /// The next resultId available to use.
        /// </summary>
        private long _nextResultId;

        /// <summary>
        /// Maps a document URI to its n most recently cached token sets.
        /// </summary>
        private Dictionary<Uri, List<LSP.SemanticTokens>> Tokens { get; }

        /// <summary>
        /// Maps a LSP token type to its respective index recognized by LSP.
        /// </summary>
        public Dictionary<string, int> TokenTypesToIndex { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SemanticTokensCache()
        {
            Tokens = new Dictionary<Uri, List<LSP.SemanticTokens>>();
            TokenTypesToIndex = ComputeTokenTypesToIndex();
        }

        /// <summary>
        /// Updates the given document's token set cache. Removes old cache results if the document's
        /// cache is full.
        /// </summary>
        public async Task UpdateCacheAsync(
            Uri uri,
            LSP.SemanticTokens tokens,
            CancellationToken cancellationToken)
        {
            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                // Case 1: Document does not currently have any token sets cached. Create a cache
                // for the document and return.
                if (!Tokens.TryGetValue(uri, out var tokenSets))
                {
                    Tokens.Add(uri, new List<LSP.SemanticTokens> { tokens });
                    return;
                }

                // Case 2: Document already has the maximum number of token sets cached. Remove the
                // oldest token set from the cache, and then add the new token set (see case 3).
                if (tokenSets.Count >= _maxCachesPerDoc)
                {
                    tokenSets.RemoveAt(0);
                }

                // Case 3: Document has less than the maximum number of token sets cached.
                // Add new token set to cache.
                tokenSets.Add(tokens);
            }
        }

        /// <summary>
        /// Returns the cached tokens for a given document URI and resultId.
        /// Returns null if no match is found.
        /// </summary>
        public async Task<LSP.SemanticTokens?> GetCachedTokensAsync(
            Uri uri,
            string? resultId,
            CancellationToken cancellationToken)
        {
            if (resultId == null)
            {
                return null;
            }

            using (await _semaphore.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!Tokens.TryGetValue(uri, out var tokenSets))
                {
                    return null;
                }

                // Return a non-null value only if the document's cache contains a token set with the resultId
                // that the user is searching for.
                return tokenSets.FirstOrDefault(t => t.ResultId != null && t.ResultId == resultId);
            }
        }

        /// <summary>
        /// Returns the next available resultId.
        /// </summary>
        public string GetNextResultId() => Interlocked.Increment(ref _nextResultId).ToString();

        /// <summary>
        /// Computes the mapping between a LSP token type and its respective index recognized by LSP.
        /// </summary>
        private static Dictionary<string, int> ComputeTokenTypesToIndex()
        {
            var tokenTypeToIndex = new Dictionary<string, int>();
            for (var i = 0; i < LSP.SemanticTokenTypes.AllTypes.Count; i++)
            {
                tokenTypeToIndex.Add(LSP.SemanticTokenTypes.AllTypes[i], i);
            }

            return tokenTypeToIndex;
        }
    }
}
