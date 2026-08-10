// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal abstract partial class LanguageServerProjectLoader
{
    internal sealed class ProjectLoadHandle
    {
        private readonly TaskCompletionSource<ProjectLoadResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ProjectLoadResult> Completion => _completionSource.Task;

        internal void Complete(ProjectLoadResult result)
            => _completionSource.TrySetResult(result);

        internal void Cancel(CancellationToken cancellationToken)
            => _completionSource.TrySetCanceled(cancellationToken);
    }
}
