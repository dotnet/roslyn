// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Xunit;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class ShellInProcess
{
    /// <returns>True if the AllInOneSearch is being used for Navigation</returns>
    public async Task<bool> ShowNavigateToDialogAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        await TestServices.Shell.ExecuteCommandAsync(VSConstants.VSStd12CmdID.NavigateTo, cancellationToken);

        return await WaitForNavigateToFocusAsync(cancellationToken);

        async Task<bool> WaitForNavigateToFocusAsync(CancellationToken cancellationToken)
        {
            bool? isAllInOneSearchActive = null;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Take no direct action regarding activation, but assert the correct item already has focus
                TestServices.JoinableTaskFactory.Run(async () =>
                {
                    await TestServices.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var searchBox = Assert.IsAssignableFrom<Control>(Keyboard.FocusedElement);
                    if ("PART_SearchBox" == searchBox.Name)
                    {
                        isAllInOneSearchActive = false; // Old search name
                    }
                    else if ("SearchBoxControl" == searchBox.Name)
                    {
                        isAllInOneSearchActive = true; // All-in-one search name
                    }
                });

                if (isAllInOneSearchActive.HasValue)
                {
                    return isAllInOneSearchActive.Value;
                }

                // If the dialog has not been displayed, then wait some time for it to show. The
                // cancellation token passed in should be hang mitigating to avoid possible
                // infinite loop.
                await Task.Delay(100);
            }
        }
    }

    internal async Task<bool> IsActiveTabProvisionalAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var shellMonitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
        if (!ErrorHandler.Succeeded(shellMonitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_DocumentFrame, out var windowFrameObject)))
        {
            throw new InvalidOperationException("Tried to get the active document frame but no documents were open.");
        }

        var windowFrame = (IVsWindowFrame)windowFrameObject;
        if (!ErrorHandler.Succeeded(windowFrame.GetProperty((int)VsFramePropID.IsProvisional, out var isProvisionalObject)))
        {
            throw new InvalidOperationException("The active window frame did not have an 'IsProvisional' property.");
        }

        return (bool)isProvisionalObject;
    }

    public Task ExecuteCommandAsync<TCommand>(CancellationToken cancellationToken) where TCommand : Commands.Command
        => ExecuteRemotableCommandAsync(typeof(TCommand).FullName, cancellationToken);

    private async Task ExecuteRemotableCommandAsync(string commandName, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var commandService = await GetRequiredGlobalServiceAsync<SVsRemotableCommandInteropService, IVsRemotableCommandInteropService2>(cancellationToken);

        ErrorHandler.ThrowOnFailure(commandService.GetControlDataSourceAsync(
            (uint)__VSCOMMANDTYPES.cCommandTypeButton,
            commandName,
            timeout: 0,
            out var dataSourceTask));

        Assumes.NotNull(dataSourceTask);
        if (dataSourceTask.GetResult() is not IVsUIDataSource commandSource)
        {
            throw new InvalidOperationException($"Error resolving command '{commandName}'.");
        }

        ErrorHandler.ThrowOnFailure(commandSource.Invoke("Execute", pvaIn: null, out _));
    }

    public async Task<string> GetActiveDocumentFileNameAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
        ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
        var windowFrame = (IVsWindowFrame)windowFrameObj;

        ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_pszMkDocument, out var documentPathObj));
        var documentPath = (string)documentPathObj;
        return Path.GetFileName(documentPath);
    }

    internal async Task<IntPtr> GetMainWindowAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        var dte = await GetRequiredGlobalServiceAsync<SDTE, EnvDTE.DTE>(cancellationToken);
        return dte.MainWindow.HWnd;
    }

    public async Task<PauseFileChangesRestorer> PauseFileChangesAsync(CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var fileChangeService = await GetRequiredGlobalServiceAsync<SVsFileChangeEx, IVsFileChangeEx3>(cancellationToken);
        Assumes.Present(fileChangeService);

        await fileChangeService.Pause();
        return new PauseFileChangesRestorer(fileChangeService);
    }

    public Task<bool> IsCommandAvailableAsync(CommandID command, CancellationToken cancellationToken)
        => IsCommandAvailableAsync(command.Guid, (uint)command.ID, cancellationToken);

    public Task<bool> IsCommandAvailableAsync<TEnum>(TEnum command, CancellationToken cancellationToken)
        where TEnum : struct, Enum
    {
        return IsCommandAvailableAsync(typeof(TEnum).GUID, Convert.ToUInt32(command), cancellationToken);
    }

    public async Task<bool> IsCommandAvailableAsync(Guid commandGuid, uint commandId, CancellationToken cancellationToken)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

        var dispatcher = await TestServices.Shell.GetRequiredGlobalServiceAsync<SUIHostCommandDispatcher, IOleCommandTarget>(cancellationToken);
        OLECMD[] commands =
        [
            new OLECMD { cmdID = commandId },
        ];

        var status = dispatcher.QueryStatus(commandGuid, (uint)commands.Length, commands, pCmdText: IntPtr.Zero);
        ErrorHandler.ThrowOnFailure(status);
        return ((OLECMDF)commands[0].cmdf).HasFlag(OLECMDF.OLECMDF_SUPPORTED)
            && ((OLECMDF)commands[0].cmdf).HasFlag(OLECMDF.OLECMDF_ENABLED);
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
