// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteFindAllReferencesService : IRemoteJsonService
{
    ValueTask<RemoteResponse<SumType<VSInternalReferenceItem, LspLocation>[]?>> FindAllReferencesAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        Position position,
        CancellationToken cancellationToken);
}
