// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

internal static class IDocumentMappingServiceExtensions
{
    /// <summary>
    /// Maps a range in the specified generated document uri to a range in the Razor document that owns the
    /// generated document. If the uri passed in is not for a generated document, or the range cannot be mapped
    /// for some other reason, the original passed in range is returned unchanged.
    /// </summary>
    public static Task<(Uri MappedDocumentUri, LinePositionSpan MappedRange)> MapToHostDocumentUriAndRangeAsync(
        this IDocumentMappingService service,
        RemoteDocumentSnapshot originSnapshot,
        Uri generatedDocumentUri,
        LinePositionSpan generatedDocumentRange,
        CancellationToken cancellationToken)
    {
        if (service is RemoteDocumentMappingService remoteService)
        {
            return remoteService.MapToHostDocumentUriAndRangeAsync(originSnapshot, generatedDocumentUri, generatedDocumentRange, cancellationToken);
        }

        return Assumed.Unreachable<Task<(Uri, LinePositionSpan)>>();
    }

    /// <summary>
    /// Maps a range in the specified generated document uri to a range in the Razor document that owns the
    /// generated document. If the uri passed in is not for a generated document, or the range cannot be mapped
    /// for some other reason, the original passed in range is returned unchanged.
    /// </summary>
    public static async Task<(Uri MappedDocumentUri, LspRange MappedRange)> MapToHostDocumentUriAndRangeAsync(
        this IDocumentMappingService service,
        RemoteDocumentSnapshot originSnapshot,
        Uri generatedDocumentUri,
        LspRange generatedDocumentRange,
        CancellationToken cancellationToken)
    {
        var (mappedDocumentUri, mappedRange) = await service
            .MapToHostDocumentUriAndRangeAsync(
                originSnapshot,
                generatedDocumentUri,
                generatedDocumentRange.ToLinePositionSpan(),
                cancellationToken)
            .ConfigureAwait(false);

        return (mappedDocumentUri, mappedRange.ToRange());
    }
}
