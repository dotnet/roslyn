// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Protocol.Folding;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteFoldingRangeService
{
    ValueTask<ImmutableArray<RemoteFoldingRange>> GetFoldingRangesAsync(RazorSolutionWrapper solutionInfo, DocumentId documentId, ImmutableArray<RemoteFoldingRange> htmlRanges, CancellationToken cancellationToken);
}
