// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.InlineHints;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint.InlayHintCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

[ExportCSharpVisualBasicLspService(typeof(InlayHintCache)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class InlayHintCache : ResolveCache<InlayHintCacheEntry>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Constructed directly by Razor")]
    public InlayHintCache() : base(maxCacheSize: 3)
    {
    }

    /// <summary>
    /// Cached data need to resolve a specific inlay hint item.
    /// </summary>
    internal sealed record InlayHintCacheEntry(ImmutableArray<InlineHint> InlayHintMembers);
}
