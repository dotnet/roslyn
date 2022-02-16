// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class ShellInProcess
    {
        public async Task<PauseFileChangesRestorer> PauseFileChangesAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var fileChangeService = await GetRequiredGlobalServiceAsync<SVsFileChangeEx, IVsFileChangeEx3>(cancellationToken);
            Assumes.Present(fileChangeService);

            await fileChangeService.Pause();
            return new PauseFileChangesRestorer(fileChangeService);
        }

        // This is based on WaitForQuiescenceAsync in the FileChangeService tests
        public async Task WaitForFileChangeNotificationsAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var fileChangeService = await GetRequiredGlobalServiceAsync<SVsFileChangeEx, IVsFileChangeEx>(cancellationToken);
            Assumes.Present(fileChangeService);

            var jobSynchronizer = fileChangeService.GetPropertyValue("JobSynchronizer");
            Assumes.Present(jobSynchronizer);

            var type = jobSynchronizer.GetType();
            var methodInfo = type.GetMethod("GetActiveSpawnedTasks", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assumes.Present(methodInfo);

            while (true)
            {
                var tasks = (Task[])methodInfo.Invoke(jobSynchronizer, null);
                if (!tasks.Any())
                    return;

                await Task.WhenAll(tasks);
            }
        }

        public readonly struct PauseFileChangesRestorer : IAsyncDisposable
        {
            private readonly IVsFileChangeEx3 _fileChangeService;

            public PauseFileChangesRestorer(IVsFileChangeEx3 fileChangeService)
            {
                _fileChangeService = fileChangeService;
            }

            public async ValueTask DisposeAsync()
            {
                await _fileChangeService.Resume();
            }
        }
    }
}
