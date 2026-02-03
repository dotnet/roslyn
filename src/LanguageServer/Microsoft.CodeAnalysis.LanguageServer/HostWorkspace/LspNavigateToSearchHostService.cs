// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigateTo;

namespace Microsoft.CodeAnalysis.LanguageServer.Features.NavigateTo;

/// <summary>
/// LSP implementation of <see cref="IWorkspaceNavigateToSearcherHostService"/> that always reports
/// as fully loaded. In LSP scenarios, we don't need the remote host hydration check since we
/// don't use the remote host for navigate-to operations.
/// </summary>
[ExportWorkspaceService(typeof(IWorkspaceNavigateToSearcherHostService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LspNavigateToSearchHostService() : IWorkspaceNavigateToSearcherHostService
{
    public ValueTask<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
        => new(true);
}
