// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteRemoveAndSortUsingsService
{
    ValueTask<ImmutableArray<TextChange>> GetRemoveAndSortUsingsEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<TextChange>> GetSortUsingsEditsAsync(
        RazorPinnedSolutionInfoWrapper solutionInfo,
        DocumentId documentId,
        CancellationToken cancellationToken);
}
