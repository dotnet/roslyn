// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal interface ICodeActionsService
{
    Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(
        VSCodeActionParams request,
        RemoteDocumentSnapshot documentSnapshot,
        RazorVSInternalCodeAction[] delegatedCodeActions,
        Uri? delegatedDocumentUri,
        bool supportsCodeActionResolve,
        CancellationToken cancellationToken);

    Task<VSCodeActionParams?> GetCSharpCodeActionsRequestAsync(RemoteDocumentSnapshot documentSnapshot, VSCodeActionParams request, CancellationToken cancellationToken);
}
