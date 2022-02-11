// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;
using IOleCommandTarget = Microsoft.VisualStudio.OLE.Interop.IOleCommandTarget;
using OLECMDEXECOPT = Microsoft.VisualStudio.OLE.Interop.OLECMDEXECOPT;

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    internal partial class ShellInProcess
    {
        public async Task<(Guid commandGroup, uint commandId)> PrepareCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var commandWindow = await GetRequiredGlobalServiceAsync<SVsCommandWindow, IVsCommandWindow>(cancellationToken);
            var result = new PREPARECOMMANDRESULT[1];
            ErrorHandler.ThrowOnFailure(commandWindow.PrepareCommand(command, out var commandGroup, out var commandId, out var cmdArg, result));

            if (cmdArg != IntPtr.Zero)
            {
                throw new NotSupportedException("Unable to create a disposable wrapper for command arguments (VARIANT).");
            }

            return (commandGroup, commandId);
        }

        public async Task ExecuteCommandAsync(string command, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var (commandGuid, commandId) = await PrepareCommandAsync(command, cancellationToken);
            await ExecuteCommandAsync(commandGuid, commandId, cancellationToken);
        }

        public Task ExecuteCommandAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
            where TEnum : struct, Enum
        {
            var commandGuid = command switch
            {
                EditorConstants.EditorCommandID => EditorConstants.EditorCommandSet,
                _ => typeof(TEnum).GUID,
            };

            return ExecuteCommandAsync(commandGuid, Convert.ToUInt32(command), cancellationToken);
        }

        public Task ExecuteCommandAsync((Guid commandGuid, uint commandId) command, CancellationToken cancellationToken)
            => ExecuteCommandAsync(command.commandGuid, command.commandId, cancellationToken);

        public async Task ExecuteCommandAsync(Guid commandGuid, uint commandId, CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
            ErrorHandler.ThrowOnFailure(dispatcher.Exec(commandGuid, commandId, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, IntPtr.Zero, IntPtr.Zero));
        }

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
