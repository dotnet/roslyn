// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.InlineHints;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint.InlayHintCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

internal sealed class InlayHintCache : ResolveCache<InlayHintCacheEntry>
{
    public InlayHintCache() : base(maxCacheSize: 3)
    {
    }

    /// <summary>
    /// Cached data need to resolve a specific inlay hint item.
    /// </summary>
    internal sealed record InlayHintCacheEntry(ImmutableArray<InlineHint> InlayHintMembers);
}
