// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol.CodeActions;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal interface ICodeActionsService
{
    Task<SumType<Command, CodeAction>[]?> GetCodeActionsAsync(
        VSCodeActionParams request,
        IDocumentSnapshot documentSnapshot,
        RazorVSInternalCodeAction[] delegatedCodeActions,
        Uri? delegatedDocumentUri,
        bool supportsCodeActionResolve,
        CancellationToken cancellationToken);

    Task<VSCodeActionParams?> GetCSharpCodeActionsRequestAsync(IDocumentSnapshot documentSnapshot, VSCodeActionParams request, CancellationToken cancellationToken);
}
