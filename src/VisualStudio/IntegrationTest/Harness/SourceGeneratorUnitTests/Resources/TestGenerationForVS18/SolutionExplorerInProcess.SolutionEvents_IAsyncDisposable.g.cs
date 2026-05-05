// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell.Interop;
    using IAsyncDisposable = System.IAsyncDisposable;

    internal partial class SolutionExplorerInProcess
    {
        private async Task RunWithSolutionEventsAsync(Func<SolutionEvents, Task> actionAsync, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            await using var solutionEvents = new SolutionEvents(JoinableTaskFactory, solution);
            await actionAsync(solutionEvents);
        }

        private sealed partial class SolutionEvents : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await _joinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
                ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
            }
        }
    }
}
