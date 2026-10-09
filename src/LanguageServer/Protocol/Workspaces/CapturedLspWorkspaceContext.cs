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
    private Task<LspWorkspaceContext>? _immediateResult;
    private AsyncLazy<LspWorkspaceContext>? _deferredResult;

    public CapturedLspWorkspaceContext(LspWorkspaceContext context)
        => _immediateResult = Task.FromResult(context);

    public CapturedLspWorkspaceContext(Func<Task<LspWorkspaceContext>> resolveAsync)
        => _deferredResult = AsyncLazy.Create(static (resolveAsync, _) => resolveAsync(), resolveAsync);

    public Task<LspWorkspaceContext> ResolveAsync()
        => Volatile.Read(ref _immediateResult)
            ?? Volatile.Read(ref _deferredResult)?.GetValueAsync(CancellationToken.None)
            ?? throw new InvalidOperationException("Workspace context has been cleared.");

    public void Clear()
    {
        Interlocked.Exchange(ref _immediateResult, null);
        Interlocked.Exchange(ref _deferredResult, null);
    }
}
