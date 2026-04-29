// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteDebugInfoService
{
    ValueTask<LinePositionSpan?> ResolveBreakpointRangeAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePosition position,
        CancellationToken cancellationToken);

    ValueTask<string[]?> ResolveProximityExpressionsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePosition position,
        CancellationToken cancellationToken);

    ValueTask<LinePositionSpan?> ValidateBreakableRangeAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        LinePositionSpan span,
        CancellationToken cancellationToken);
}
