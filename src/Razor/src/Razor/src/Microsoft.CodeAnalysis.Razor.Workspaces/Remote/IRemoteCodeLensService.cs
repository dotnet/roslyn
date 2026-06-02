// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCodeLensService : IRemoteJsonService
{
    ValueTask<LspCodeLens[]?> GetCodeLensAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        TextDocumentIdentifier textDocumentIdentifier,
        CancellationToken cancellationToken);

    ValueTask<LspCodeLens?> ResolveCodeLensAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        LspCodeLens codeLens,
        CancellationToken cancellationToken);
}
