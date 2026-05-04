// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Threading;
    using Task = System.Threading.Tasks.Task;

    [TestService]
    internal partial class SolutionExplorerInProcess
    {
        public async Task<bool> IsSolutionOpenAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            ErrorHandler.ThrowOnFailure(solution.GetProperty((int)__VSPROPID.VSPROPID_IsSolutionOpen, out var isOpen));
            return (bool)isOpen;
        }

        /// <summary>
        /// Close the currently open solution without saving.
        /// </summary>
        public async Task CloseSolutionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var solution = await GetRequiredGlobalServiceAsync<SVsSolution, IVsSolution>(cancellationToken);
            if (!await IsSolutionOpenAsync(cancellationToken))
            {
                return;
            }

#pragma warning disable RS0030 // Do not use banned APIs
            using SemaphoreSlim semaphore = new SemaphoreSlim(1);
#pragma warning restore RS0030
            await RunWithSolutionEventsAsync(
                async solutionEvents =>
                {
#pragma warning disable RS0030 // Do not use banned APIs
                    await semaphore.WaitAsync(cancellationToken);
#pragma warning restore RS0030

                    void HandleAfterCloseSolution(object sender, EventArgs e)
#pragma warning disable RS0030 // Do not use banned APIs
                        => semaphore.Release();
#pragma warning restore RS0030

                    solutionEvents.AfterCloseSolution += HandleAfterCloseSolution;
                    try
                    {
                        ErrorHandler.ThrowOnFailure(solution.CloseSolutionElement((uint)__VSSLNCLOSEOPTIONS.SLNCLOSEOPT_DeleteProject | (uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_NoSave, null, 0));
#pragma warning disable RS0030 // Do not use banned APIs
                        await semaphore.WaitAsync(cancellationToken);
#pragma warning restore RS0030
                    }
                    finally
                    {
                        solutionEvents.AfterCloseSolution -= HandleAfterCloseSolution;
                    }
                },
                cancellationToken);
        }

        private sealed partial class SolutionEvents : IVsSolutionEvents
        {
            private readonly JoinableTaskFactory _joinableTaskFactory;
            private readonly IVsSolution _solution;
            private readonly uint _cookie;

            public SolutionEvents(JoinableTaskFactory joinableTaskFactory, IVsSolution solution)
            {
                Application.Current.Dispatcher.VerifyAccess();

                _joinableTaskFactory = joinableTaskFactory;
                _solution = solution;
                ErrorHandler.ThrowOnFailure(solution.AdviseSolutionEvents(this, out _cookie));
            }

            public event EventHandler? AfterCloseSolution;

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
