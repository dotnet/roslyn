// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using static Microsoft.CodeAnalysis.LanguageServer.Handler.Completion.CompletionListCache;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

/// <summary>
/// Caches completion lists in between calls to CompletionHandler and
/// CompletionResolveHandler. Used to avoid unnecessary recomputation.
/// </summary>
[ExportCSharpVisualBasicLspService(typeof(CompletionListCache)), Shared(LspServiceComposition.SharingBoundary)]
internal sealed class CompletionListCache : ResolveCache<CacheEntry>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Constructed directly by Razor tests")]
    public CompletionListCache() : base(maxCacheSize: 3)
    {
    }

    public sealed record CacheEntry(CompletionList CompletionList);
}
