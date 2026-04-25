// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Remote;
using Microsoft.CodeAnalysis.Razor.Utilities;
using Microsoft.VisualStudio.Razor.Debugging;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Debugging;

[Export(typeof(IRazorProximityExpressionResolver))]
[method: ImportingConstructor]
internal class RazorProximityExpressionResolver(
    IRemoteServiceInvoker remoteServiceInvoker) : IRazorProximityExpressionResolver
{
    private record CohostCacheKey(DocumentId DocumentId, VersionStamp Version, int Line, int Character) : CacheKey;
    private record LspCacheKey(Uri DocumentUri, long? HostDocumentSyncVersion, int Line, int Character) : CacheKey;
    private record CacheKey;

    private readonly IRemoteServiceInvoker _remoteServiceInvoker = remoteServiceInvoker;

    // 10 is a magic number where this effectively represents our ability to cache the last 10 "hit" breakpoint locations
    // corresponding proximity expressions which enables us not to go "async" in those re-hit scenarios.
    private readonly MemoryCache<CacheKey, IReadOnlyList<string>> _cache = new(sizeLimit: 10);

    public async Task<IReadOnlyList<string>?> TryResolveProximityExpressionsAsync(ITextBuffer textBuffer, int lineIndex, int characterIndex, CancellationToken cancellationToken)
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
            .TryInvokeAsync<IRemoteDebugInfoService, string[]?>(
                razorDocument.Project.Solution,
                (service, solutionInfo, cancellationToken) =>
                    service.ResolveProximityExpressionsAsync(solutionInfo, razorDocument.Id, new(lineIndex, characterIndex), cancellationToken),
                cancellationToken)
            .ConfigureAwait(false);

        if (response is null)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Cache range so if we're asked again for this document/line/character we don't have to go async.
        _cache.Set(cacheKey, response);

        return response;
    }
}
