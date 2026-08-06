// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteInlayHintService : IRemoteJsonService
{
    ValueTask<InlayHint[]?> GetInlayHintsAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHintParams inlayHintParams, bool displayAllOverride, CancellationToken cancellationToken);

    ValueTask<InlayHint> ResolveHintAsync(JsonSerializableRazorSolutionWrapper solutionInfo, JsonSerializableDocumentId razorDocumentId, InlayHint inlayHint, CancellationToken cancellationToken);
}
