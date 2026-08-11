// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed class LanguageServerProjectLoadHandle
{
    private readonly TaskCompletionSource<LanguageServerProjectLoadResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<LanguageServerProjectLoadResult> Completion => _completionSource.Task;

    internal void Complete(LanguageServerProjectLoadResult result)
        => _completionSource.TrySetResult(result);

    internal void Cancel(CancellationToken cancellationToken)
        => _completionSource.TrySetCanceled(cancellationToken);
}