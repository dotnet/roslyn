// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using CompletionResponse = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.Completion.CompletionResult>;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCompletionService : IRemoteJsonService
{
    ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        VSInternalCompletionContext completionContext,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<CompletionResponse> GetCompletionAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken);

    ValueTask<VSInternalCompletionItem> ResolveCompletionItemAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId id,
        VSInternalCompletionItem request,
        CancellationToken cancellationToken);
}
