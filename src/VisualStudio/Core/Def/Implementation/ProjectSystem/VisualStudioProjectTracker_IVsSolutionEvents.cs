// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            _solutionIsClosing = true;

            foreach (var p in _projectMap.Values)
            {
                p.StopPushingToWorkspaceHosts();
            }

            _solutionLoadComplete = false;

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
        {
            Contract.ThrowIfFalse(_projectMap.Count == 0);

            NotifyWorkspaceHosts(host => host.OnSolutionRemoved());
            NotifyWorkspaceHosts(host => host.ClearSolution());

            _projectPathToIdMap.Clear();

            foreach (var workspaceHost in _workspaceHosts)
            {
                workspaceHost.SolutionClosed();
            }

            _solutionIsClosing = false;

            return VSConstants.S_OK;
        }
    }
}
