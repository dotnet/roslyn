// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCallHierarchyService : IRemoteJsonService
{
    ValueTask<RemoteResponse<CallHierarchyItem[]?>> PrepareCallHierarchyAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<CallHierarchyIncomingCall[]?>> GetIncomingCallsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken);

    ValueTask<RemoteResponse<CallHierarchyOutgoingCall[]?>> GetOutgoingCallsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CallHierarchyItem item,
        CancellationToken cancellationToken);
}
