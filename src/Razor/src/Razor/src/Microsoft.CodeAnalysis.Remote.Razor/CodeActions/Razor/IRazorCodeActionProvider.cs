// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.CodeActions.Models;

namespace Microsoft.CodeAnalysis.Razor.CodeActions;

internal interface IRazorCodeActionProvider
{
    Task<ImmutableArray<RazorVSInternalCodeAction>> ProvideAsync(
        RazorCodeActionContext context,
        CancellationToken cancellationToken);
}
