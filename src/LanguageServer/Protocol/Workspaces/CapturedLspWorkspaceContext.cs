// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer;

/// <summary>
/// Captures a request's workspace state during dispatch and resolves it once outside dispatch.
/// </summary>
internal sealed class CapturedLspWorkspaceContext
{
    private AsyncLazy<LspWorkspaceContext>? _result;

    public CapturedLspWorkspaceContext(LspWorkspaceContext context)
        => _result = AsyncLazy.Create(context);

    public CapturedLspWorkspaceContext(Func<Task<LspWorkspaceContext>> resolveAsync)
        => _result = AsyncLazy.Create(static (resolveAsync, _) => resolveAsync(), resolveAsync);

    public Task<LspWorkspaceContext> ResolveAsync(CancellationToken cancellationToken)
        => _result?.GetValueAsync(cancellationToken)
            ?? throw new InvalidOperationException("Workspace context has been cleared.");

    public void Clear()
    {
        _result = null;
    }
}
