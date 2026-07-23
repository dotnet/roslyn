// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal interface IRemoteCodeActionsService : IRemoteJsonService
{
    ValueTask<CodeActionRequestInfo> GetCodeActionRequestInfoAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        VSCodeActionParams request,
        CancellationToken cancellationToken);

    ValueTask<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        VSCodeActionParams request,
        RazorVSInternalCodeAction[] delegatedCodeActions,
        CancellationToken cancellationToken);

    ValueTask<CodeAction> ResolveCodeActionAsync(
        JsonSerializableRazorSolutionWrapper solutionInfo,
        JsonSerializableDocumentId razorDocumentId,
        CodeAction request,
        CodeAction? delegatedCodeAction,
        CancellationToken cancellationToken);
}
