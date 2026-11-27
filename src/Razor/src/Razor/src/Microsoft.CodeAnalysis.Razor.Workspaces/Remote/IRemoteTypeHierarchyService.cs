// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteTypeHierarchyService : IRemoteJsonService
{
    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> PrepareTypeHierarchyAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSupertypesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<TypeHierarchyItem[]?>> ResolveSubtypesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TypeHierarchyItem item,
        CancellationToken cancellationToken);
}
