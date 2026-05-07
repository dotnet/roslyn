// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal interface ICodeActionProvider
{
    /// <summary>
    /// Takes code actions provided by a child language, and provides code actions that should be returned to the LSP client.
    /// </summary>
    /// <remarks>
    /// The list of code actions returned from all providers will be combined together in a list. A null result and an empty
    /// result are effectively the same.
    /// </remarks>
    Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(RazorCodeActionContext context, ImmutableArray<RazorVSInternalCodeAction> codeActions, CancellationToken cancellationToken);
}
