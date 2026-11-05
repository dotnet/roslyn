// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using CompletionResponse = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Microsoft.CodeAnalysis.Razor.Protocol.Completion.CompletionResult>;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCompletionService : IRemoteJsonService
{
    ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionContext completionContext,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<CompletionResponse> GetCompletionAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionPositionInfo positionInfo,
        CompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        Guid correlationId,
        CancellationToken cancellationToken);

    ValueTask<CompletionItem> ResolveCompletionItemAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId id,
        CompletionItem request,
        RazorFormattingOptions formattingOptions,
        CancellationToken cancellationToken);
}
