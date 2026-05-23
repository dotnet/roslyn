// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteSpanMappingService
{
    ValueTask<ImmutableArray<RazorMappedEditResult>> MapTextChangesAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId generatedDocumentId,
        ImmutableArray<TextChange> changes,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<RazorMappedSpanResult>> MapSpansAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId generatedDocumentId,
        ImmutableArray<TextSpan> spans,
        CancellationToken cancellationToken);

    ValueTask<RemoteExcerptResult?> TryExcerptAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId id,
        TextSpan span,
        RazorExcerptMode mode,
        RazorClassificationOptionsWrapper options,
        CancellationToken cancellationToken);
}
