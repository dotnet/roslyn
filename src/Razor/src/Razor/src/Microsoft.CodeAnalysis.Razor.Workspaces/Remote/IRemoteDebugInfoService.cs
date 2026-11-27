// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteDebugInfoService
{
    ValueTask<LinePositionSpan?> ResolveBreakpointRangeAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        LinePosition position,
        CancellationToken cancellationToken);

    ValueTask<string[]?> ResolveProximityExpressionsAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        LinePosition position,
        CancellationToken cancellationToken);

    ValueTask<LinePositionSpan?> ValidateBreakableRangeAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId documentId,
        LinePositionSpan span,
        CancellationToken cancellationToken);
}
