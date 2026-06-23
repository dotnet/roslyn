// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteSpanMappingService
{
    ValueTask<ImmutableArray<RemoteMappedEditResult>> MapTextChangesAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId generatedDocumentId,
        ImmutableArray<TextChange> changes,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<RemoteMappedSpanResult>> MapSpansAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId generatedDocumentId,
        ImmutableArray<TextSpan> spans,
        CancellationToken cancellationToken);

    ValueTask<RemoteExcerptResult?> TryExcerptAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId id,
        TextSpan span,
        ExcerptMode mode,
        ClassificationOptions options,
        CancellationToken cancellationToken);
}
