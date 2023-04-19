// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Completion;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.Completion.CompletionListCache;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Caches completion lists in between calls to CompletionHandler and
    /// CompletionResolveHandler. Used to avoid unnecessary recomputation.
    /// </summary>
    internal class CompletionListCache : ResolveCache<CacheEntry, long>
    {
        /// <summary>
        /// The next resultId available to use.
        /// </summary>
        private long _nextResultId;

        public CompletionListCache() : base(maxCacheSize: 3)
        {
        }

        public record CacheEntry(LSP.TextDocumentIdentifier TextDocument, CompletionList CompletionList);

        public long UpdateCache(CacheEntry cacheEntry)
        {
            Action<long> updateCacheIdCallback = (value) => { this._nextResultId = value + 1; };
            return this.UpdateCache(cacheEntry, _nextResultId, updateCacheIdCallback);
        }
    }
}
