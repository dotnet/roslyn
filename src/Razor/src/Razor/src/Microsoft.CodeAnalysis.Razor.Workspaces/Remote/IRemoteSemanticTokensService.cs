// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteSemanticTokensService
{
    ValueTask<int[]?> GetSemanticTokensDataAsync(
        RazorSolutionWrapper solutionInfo,
        DocumentId razorDocumentId,
        LinePositionSpan span,
        Guid correlationId,
        CancellationToken cancellationToken);
}
