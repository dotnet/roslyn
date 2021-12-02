// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncDisposable = System.IAsyncDisposable;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class ShellInProcess : InProcComponent
    {
        public ShellInProcess(TestServices testServices)
            : base(testServices)
        {
        }

        public new Task<TInterface> GetRequiredGlobalServiceAsync<TService, TInterface>(CancellationToken cancellationToken)
            where TService : class
            where TInterface : class
        {
            return base.GetRequiredGlobalServiceAsync<TService, TInterface>(cancellationToken);
        }

        public new Task<TService> GetComponentModelServiceAsync<TService>(CancellationToken cancellationToken)
            where TService : class
        {
            return base.GetComponentModelServiceAsync<TService>(cancellationToken);
        }

        public async Task<string> GetActiveWindowCaptionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var monitorSelection = await GetRequiredGlobalServiceAsync<SVsShellMonitorSelection, IVsMonitorSelection>(cancellationToken);
            ErrorHandler.ThrowOnFailure(monitorSelection.GetCurrentElementValue((uint)VSConstants.VSSELELEMID.SEID_WindowFrame, out var windowFrameObj));
            var windowFrame = (IVsWindowFrame)windowFrameObj;

            ErrorHandler.ThrowOnFailure(windowFrame.GetProperty((int)__VSFPROPID.VSFPROPID_Caption, out var captionObj));
            return $"{captionObj}";
        }

        public async IAsyncEnumerable<IVsWindowFrame> EnumerateWindowsAsync(__WindowFrameTypeFlags windowFrameTypeFlags, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var uiShell = await GetRequiredGlobalServiceAsync<SVsUIShell, IVsUIShell4>(cancellationToken);
            ErrorHandler.ThrowOnFailure(uiShell.GetWindowEnum((uint)windowFrameTypeFlags, out var enumWindowFrames));
            var frameBuffer = new IVsWindowFrame[1];
            while (true)
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ErrorHandler.ThrowOnFailure(enumWindowFrames.Next((uint)frameBuffer.Length, frameBuffer, out var fetched));
                if (fetched == 0)
                    yield break;

                for (var i = 0; i < fetched; i++)
                {
                    yield return frameBuffer[i];
                }
            }
        }

        public async Task<Version> GetVersionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var shell = await GetRequiredGlobalServiceAsync<SVsShell, IVsShell>(cancellationToken);
            shell.GetProperty((int)__VSSPROPID5.VSSPROPID_ReleaseVersion, out var versionProperty);

            var fullVersion = versionProperty?.ToString() ?? "";
            var firstSpace = fullVersion.IndexOf(' ');
            if (firstSpace >= 0)
            {
                // e.g. "17.1.31907.60 MAIN"
                fullVersion = fullVersion[..firstSpace];
            }

            if (Version.TryParse(fullVersion, out var version))
                return version;

            throw new NotSupportedException($"Unexpected version format: {versionProperty}");
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
