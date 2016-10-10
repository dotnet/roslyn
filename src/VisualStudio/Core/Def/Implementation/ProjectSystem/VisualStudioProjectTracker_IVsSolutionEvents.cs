// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioProjectTracker : IVsSolutionEvents
    {
        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            AssertIsForeground();

            if (IsDeferredSolutionLoadEnabled())
            {
                var deferredProjectWorkspaceService = _workspaceServices.GetService<IDeferredProjectWorkspaceService>();
                LoadSolutionFromMSBuildAsync(deferredProjectWorkspaceService, _solutionParsingCancellationTokenSource.Token).FireAndForget();
            }

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            AssertIsForeground();

            _solutionIsClosing = true;

            foreach (var p in this.ImmutableProjects)
            {
                p.StopPushingToWorkspaceHosts();
            }

            _solutionLoadComplete = false;

            // Cancel any background solution parsing. NOTE: This means that that work needs to
            // check the token periodically, and whenever resuming from an "await"
            _solutionParsingCancellationTokenSource.Cancel();
            _solutionParsingCancellationTokenSource = new CancellationTokenSource();

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            AssertIsForeground();

            if (IsDeferredSolutionLoadEnabled())
            {
                // Copy to avoid modifying the collection while enumerating
                var loadedProjects = ImmutableProjects.ToList();
                foreach (var p in loadedProjects)
                {
                    p.Disconnect();
                }
            }

            lock (_gate)
            {
                Contract.ThrowIfFalse(_projectMap.Count == 0);
            }

            NotifyWorkspaceHosts_Foreground(host => host.OnSolutionRemoved());
            NotifyWorkspaceHosts_Foreground(host => host.ClearSolution());

            lock (_gate)
            {
                _projectPathToIdMap.Clear();
            }

            foreach (var workspaceHost in _workspaceHosts)
            {
                workspaceHost.SolutionClosed();
            }

            _solutionIsClosing = false;

            return VSConstants.S_OK;
        }
    }
}
