// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Debugging;

[Export(typeof(IRazorBreakpointResolver))]
[method: ImportingConstructor]
internal class RazorBreakpointResolver(
    IRemoteServiceInvoker remoteServiceInvoker) : IRazorBreakpointResolver
{
    private record CohostCacheKey(DocumentId DocumentId, VersionStamp Version, int Line, int Character) : CacheKey;
    private record LspCacheKey(Uri DocumentUri, long? HostDocumentSyncVersion, int Line, int Character) : CacheKey;
    private record CacheKey;

    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    // 4 is a magic number that was determined based on the functionality of VisualStudio. Currently when you set or edit a breakpoint
    // we get called with two different locations for the same breakpoint. Because of this 2 time call our size must be at least 2,
    // we grow it to 4 just to be safe for lesser known scenarios.
    private readonly MemoryCache<CacheKey, LspRange> _cache = new(sizeLimit: 4);

    public async Task<LspRange?> TryResolveBreakpointRangeAsync(ITextBuffer textBuffer, int lineIndex, int characterIndex, CancellationToken cancellationToken)
    {
        if (!textBuffer.TryGetTextDocument(out var razorDocument))
        {
            // Razor document is not in the Roslyn workspace.
            return null;
        }

        if (razorDocument.TryGetTextVersion(out var version))
        {
            version = await razorDocument.GetTextVersionAsync(cancellationToken).ConfigureAwait(false);
        }

        var cacheKey = new CohostCacheKey(razorDocument.Id, version, lineIndex, characterIndex);
        if (_cache.TryGetValue(cacheKey, out var cachedRange))
        {
            // We've seen this request before. Hopefully the TryGetTextVersion call above was successful so this whole path
            // will have been sync, and the cache will have been useful.
            return cachedRange;
        }

        var response = await _remoteServiceInvoker
            .TryInvokeAsync<IRemoteDebugInfoService, LinePositionSpan?>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.ResolveBreakpointRangeAsync(solutionInfo, razorDocument.Id, new(lineIndex, characterIndex), cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response is not { } responseSpan)
        {
            // can't map the position, invalid breakpoint location.
            return null;
        }

        var hostDocumentRange = responseSpan.ToRange();
        cancellationToken.ThrowIfCancellationRequested();

        // Cache range so if we're asked again for this document/line/character we don't have to go async.
        _cache.Set(cacheKey, hostDocumentRange);

        return hostDocumentRange;
    }
}
