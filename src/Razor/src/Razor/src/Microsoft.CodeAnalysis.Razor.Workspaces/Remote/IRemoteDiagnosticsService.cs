// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteDiagnosticsService : IRemoteJsonService
{
    ValueTask<ImmutableArray<LspDiagnostic>> GetDiagnosticsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        LspDiagnostic[] csharpImplDiagnostics,
        LspDiagnostic[] csharpDeclDiagnostics,
        LspDiagnostic[] htmlDiagnostics,
        CancellationToken cancellationToken);

    ValueTask<ImmutableArray<LspDiagnostic>> GetTaskListDiagnosticsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        LspDiagnostic[] csharpImplTaskItems,
        LspDiagnostic[] csharpDeclTaskItems,
        CancellationToken cancellationToken);
}
