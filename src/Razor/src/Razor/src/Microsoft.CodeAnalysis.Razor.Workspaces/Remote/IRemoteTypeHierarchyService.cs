// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteTypeHierarchyService : IRemoteJsonService
{
    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSupertypesAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSubtypesAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken);
}
