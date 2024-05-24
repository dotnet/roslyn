// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Completion;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.Completion.CompletionListCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion
{
    /// <summary>
    /// Caches completion lists in between calls to CompletionHandler and
    /// CompletionResolveHandler. Used to avoid unnecessary recomputation.
    /// </summary>
    internal class CompletionListCache : ResolveCache<CacheEntry>
    {
        public CompletionListCache() : base(maxCacheSize: 3)
        {
        }

        public record CacheEntry(CompletionList CompletionList);
    }
}
