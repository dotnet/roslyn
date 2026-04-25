// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol.Completion;
using Response = Microsoft.CodeAnalysis.Razor.Remote.RemoteResponse<Roslyn.LanguageServer.Protocol.RazorVSInternalCompletionList?>;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCompletionService : IRemoteJsonService
{
    ValueTask<CompletionPositionInfo?> GetPositionInfoAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        VSInternalCompletionContext completionContext,
        Position position,
        CancellationToken cancellationToken);

    ValueTask<Response> GetCompletionAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId documentId,
        CompletionPositionInfo positionInfo,
        VSInternalCompletionContext completionContext,
        RazorCompletionOptions razorCompletionOptions,
        HashSet<string> existingHtmlCompletions,
        Guid correlationId,
        CancellationToken cancellationToken);

    ValueTask<VSInternalCompletionItem> ResolveCompletionItemAsync(
        JsonSerializableRazorPinnedSolutionInfoWrapper solutionInfo,
        JsonSerializableDocumentId id,
        VSInternalCompletionItem request,
        CancellationToken cancellationToken);
}
