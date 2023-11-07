// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.Input;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Hosting.Diagnostics.Waiters;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess
{
    internal class SolutionExplorer_InProc : InProcComponent
    {
        private readonly SendKeys_InProc _sendKeys;

        private SolutionExplorer_InProc()
        {
            _sendKeys = new SendKeys_InProc(VisualStudio_InProc.Create());
        }

        public static SolutionExplorer_InProc Create()
            => new SolutionExplorer_InProc();

        public void CleanUpOpenSolution()
        {
            var directoriesToDelete = new List<string>();
            var dte = GetDTE();

            InvokeOnUIThread(cancellationToken =>
            {
                if (dte.Solution != null)
                {
                    // Save the full path to each project in the solution. This is so we can
                    // cleanup any folders after the solution is closed.
                    foreach (EnvDTE.Project project in dte.Solution.Projects)
                    {
                        if (!string.IsNullOrEmpty(project.FullName))
                        {
                            directoriesToDelete.Add(Path.GetDirectoryName(project.FullName));
                        }
                    }

                    // Save the full path to the solution. This is so we can cleanup any folders after the solution is closed.
                    // The solution might be zero-impact and thus has no name, so deal with that
                    var solutionFullName = dte.Solution.FullName;

                    if (!string.IsNullOrEmpty(solutionFullName))
                    {
                        directoriesToDelete.Add(Path.GetDirectoryName(solutionFullName));
                    }
                }
            });

            if (dte.Debugger.CurrentMode != EnvDTE.dbgDebugMode.dbgDesignMode)
            {
                // Close the Find Source window in case it's open.
                // 🐛 This is an ugly mitigation for https://github.com/dotnet/roslyn/issues/33785
                _sendKeys.Send(VirtualKey.Escape);

                dte.Debugger.TerminateAll();
                WaitForDesignMode(dte);
            }

            CloseSolution();
            ErrorList_InProc.Create().WaitForNoErrorsInErrorList(Helper.HangMitigatingTimeout);

            foreach (var directoryToDelete in directoriesToDelete)
            {
                IntegrationHelper.TryDeleteDirectoryRecursively(directoryToDelete);
            }
        }

        private static void WaitForDesignMode(EnvDTE.DTE dte)
        {
#if TODO// https://github.com/dotnet/roslyn/issues/35965
            // This delay was originally added to address test failures in BasicEditAndContinue. When running
            // multiple tests in sequence, situations were observed where the Edit and Continue state was not reset:
            //
            // 1. Test A runs, starts debugging with Edit and Continue
            // 2. Test A completes, and the debugger is terminated
            // 3. A new project is created for test B
            // 4. Test B attempts to set the text for the document created in step (3), but fails
            //
            // Step (4) was causing test failures because the project created for test B remained in a read-only
            // state believing a debugger session was active.
            //
            // This delay should be replaced with a proper wait condition once the correct one is determined.
            var debugService = GetComponentModelService<VisualStudioWorkspace>().Services.GetRequiredService<IDebuggingWorkspaceService>();
            using (var debugSessionEndedEvent = new ManualResetEventSlim(initialState: false))
            {
                debugService.BeforeDebuggingStateChanged += (_, e) =>
                {
                    if (e.After == DebuggingState.Design)
                    {
                        debugSessionEndedEvent.Set();
                    }
                };

                if (dte.Debugger.CurrentMode == EnvDTE.dbgDebugMode.dbgDesignMode)
                {
                    return;
                }

                if (!debugSessionEndedEvent.Wait(Helper.HangMitigatingTimeout))
                {
                    throw new TimeoutException("Failed to enter design mode in a timely manner.");
                }
            }
#endif
        }

        private void CloseSolution()
        {
            var solution = GetGlobalService<SVsSolution, IVsSolution>();
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            if (!(bool)isOpen)
            {
                return;
            }

            using (var semaphore = new SemaphoreSlim(1))
            using (var solutionEvents = new SolutionEvents(solution))
            {
                semaphore.Wait();
                void HandleAfterCloseSolution(object sender, EventArgs e) => semaphore.Release();
                solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
                try
                {
                    InvokeOnUIThread(cancellationToken =>
                    {
                        ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
                    });

                    semaphore.Wait();
                }
                finally
                {
                    solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
                }
            }

            var waitingService = new TestWaitingService(GetComponentModel().DefaultExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>());
            waitingService.WaitForAsyncOperations(FeatureAttribute.Workspace, waitForWorkspaceFirst: true);
        }

        private sealed class SolutionEvents : IVsSolutionEvents, IDisposable
        {
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(IVsSolution solution)
            {
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler? AfterCloseSolution;

            public void Dispose()
            {
                InvokeOnUIThread(cancellationToken =>
                {
                    ErrorHandler.ThrowOnFailure(_solution.UnadviseSolutionEvents(_cookie));
                });
            }

            public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
            {
                return VSConstants.S_OK;
            }

            public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            {
                return VSConstants.S_OK;
            }

            public int OnBeforeCloseSolution(object pUnkReserved)
            {
                return VSConstants.S_OK;
            }

            public int OnAfterCloseSolution(object pUnkReserved)
            {
                AfterCloseSolution?.Invoke(this, EventArgs.Empty);
                return VSConstants.S_OK;
            }
        }
    }
}
