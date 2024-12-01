// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

/// <summary>
/// Creates batch scopes for projects based on IVsSolutionEvents. This is useful for projects types that don't otherwise have
/// good batching concepts.
/// </summary> 
/// <remarks>All members of this class are affinitized to the UI thread.</remarks>
[Export(typeof(SolutionEventsBatchScopeCreator))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SolutionEventsBatchScopeCreator(IThreadingContext threadingContext, [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
{
    private readonly List<(ProjectSystemProject project, IVsHierarchy hierarchy, ProjectSystemProject.BatchScope batchScope)> _fullSolutionLoadScopes = new List<(ProjectSystemProject, IVsHierarchy, ProjectSystemProject.BatchScope)>();

    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private uint? _runningDocumentTableEventsCookie;
    private bool _isSubscribedToSolutionEvents = false;
    private bool _solutionLoaded = false;

    public void StartTrackingProject(ProjectSystemProject project, IVsHierarchy hierarchy)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        EnsureSubscribedToSolutionEvents();

        if (!_solutionLoaded)
        {
            _fullSolutionLoadScopes.Add((project, hierarchy, project.CreateBatchScope()));

            EnsureSubscribedToRunningDocumentTableEvents();
        }
    }

    public void StopTrackingProject(ProjectSystemProject project)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        foreach (var scope in _fullSolutionLoadScopes)
        {
            if (scope.project == project)
            {
                scope.batchScope.Dispose();
                _fullSolutionLoadScopes.Remove(scope);
                break;
            }
        }

        EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded();
    }

    private void StopTrackingAllProjects()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        foreach (var (_, _, batchScope) in _fullSolutionLoadScopes)
        {
            batchScope.Dispose();
        }

        _fullSolutionLoadScopes.Clear();

        EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded();
    }

    private void StopTrackingAllProjectsMatchingHierarchy(IVsHierarchy hierarchy)
    {
        _threadingContext.ThrowIfNotOnUIThread();

        for (var i = 0; i < _fullSolutionLoadScopes.Count; i++)
        {
            if (_fullSolutionLoadScopes[i].hierarchy == hierarchy)
            {
                _fullSolutionLoadScopes[i].batchScope.Dispose();
                _fullSolutionLoadScopes.RemoveAt(i);

                // Go back by one so we re-check the same index
                i--;
            }
        }

        EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded();
    }

    private void EnsureSubscribedToSolutionEvents()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_isSubscribedToSolutionEvents)
        {
            return;
        }

        var solution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));

        // We never unsubscribe from these, so we just throw out the cookie. We could consider unsubscribing if/when all our
        // projects are unloaded, but it seems fairly unnecessary -- it'd only be useful if somebody closed one solution but then
        // opened other solutions in entirely different languages from there.
        if (ErrorHandler.Succeeded(solution.AdviseSolutionEvents(new SolutionEventsEventSink(this), out _)))
        {
            _isSubscribedToSolutionEvents = true;
        }

        // It's possible that we're loading after the solution has already fully loaded, so see if we missed the event 
        var shellMonitorSelection = (IVsMonitorSelection)_serviceProvider.GetService(typeof(SVsShellMonitorSelection));

        if (ErrorHandler.Succeeded(shellMonitorSelection.GetCmdUIContextCookie(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_guid, out var fullyLoadedContextCookie)))
        {
            if (ErrorHandler.Succeeded(shellMonitorSelection.IsCmdUIContextActive(fullyLoadedContextCookie, out var fActive)) && fActive != 0)
            {
                _solutionLoaded = true;
            }
        }
    }

    private void EnsureSubscribedToRunningDocumentTableEvents()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (_runningDocumentTableEventsCookie.HasValue)
        {
            return;
        }

        var runningDocumentTable = (IVsRunningDocumentTable)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));

        if (ErrorHandler.Succeeded(runningDocumentTable.AdviseRunningDocTableEvents(new RunningDocumentTableEventSink(this, runningDocumentTable), out var runningDocumentTableEventsCookie)))
        {
            _runningDocumentTableEventsCookie = runningDocumentTableEventsCookie;
        }
    }

    private void EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded()
    {
        _threadingContext.ThrowIfNotOnUIThread();

        if (!_runningDocumentTableEventsCookie.HasValue)
        {
            return;
        }

        // If we don't have any scopes left, then there is no reason to be subscribed to Running Document Table events, because
        // there won't be any scopes to complete.
        if (_fullSolutionLoadScopes.Count > 0)
        {
            return;
        }

        var runningDocumentTable = (IVsRunningDocumentTable)_serviceProvider.GetService(typeof(SVsRunningDocumentTable));
        runningDocumentTable.UnadviseRunningDocTableEvents(_runningDocumentTableEventsCookie.Value);
        _runningDocumentTableEventsCookie = null;
    }

    private class SolutionEventsEventSink : IVsSolutionEvents, IVsSolutionLoadEvents
    {
        private readonly SolutionEventsBatchScopeCreator _scopeCreator;

        public SolutionEventsEventSink(SolutionEventsBatchScopeCreator scopeCreator)
            => _scopeCreator = scopeCreator;

        int IVsSolutionLoadEvents.OnBeforeOpenSolution(string pszSolutionFilename)
        {
            Contract.ThrowIfTrue(_scopeCreator._fullSolutionLoadScopes.Any());

            _scopeCreator._solutionLoaded = false;

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnBeforeCloseSolution(object pUnkReserved)
        {
            _scopeCreator._solutionLoaded = false;

            return VSConstants.S_OK;
        }

        int IVsSolutionEvents.OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            _scopeCreator._solutionLoaded = true;
            _scopeCreator.StopTrackingAllProjects();

            return VSConstants.S_OK;
        }

        #region Unimplemented Members

        int IVsSolutionLoadEvents.OnAfterBackgroundSolutionLoadComplete()
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionEvents.OnAfterCloseSolution(object pUnkReserved)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionLoadEvents.OnBeforeBackgroundSolutionLoadBegins()
            => VSConstants.E_NOTIMPL;

        int IVsSolutionLoadEvents.OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.E_NOTIMPL;
        }

        int IVsSolutionLoadEvents.OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
            => VSConstants.E_NOTIMPL;

        int IVsSolutionLoadEvents.OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
            => VSConstants.E_NOTIMPL;

        #endregion
    }

    private class RunningDocumentTableEventSink : IVsRunningDocTableEvents
    {
        private readonly SolutionEventsBatchScopeCreator _scopeCreator;
        private readonly IVsRunningDocumentTable4 _runningDocumentTable;

        public RunningDocumentTableEventSink(SolutionEventsBatchScopeCreator scopeCreator, IVsRunningDocumentTable runningDocumentTable)
        {
            _scopeCreator = scopeCreator;
            _runningDocumentTable = (IVsRunningDocumentTable4)runningDocumentTable;
        }

        int IVsRunningDocTableEvents.OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            _runningDocumentTable.GetDocumentHierarchyItem(docCookie, out var hierarchy, out _);

            // Some document is being opened in this project; we need to ensure the project is fully updated so any requests
            // for CodeModel or the workspace are successful.
            _scopeCreator.StopTrackingAllProjectsMatchingHierarchy(hierarchy);

            return VSConstants.S_OK;
        }

        #region Unimplemented Members

        int IVsRunningDocTableEvents.OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
            => VSConstants.E_NOTIMPL;

        int IVsRunningDocTableEvents.OnAfterSave(uint docCookie)
            => VSConstants.E_NOTIMPL;

        int IVsRunningDocTableEvents.OnAfterAttributeChange(uint docCookie, uint grfAttribs)
            => VSConstants.E_NOTIMPL;

        int IVsRunningDocTableEvents.OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
            => VSConstants.E_NOTIMPL;

        int IVsRunningDocTableEvents.OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
            => VSConstants.E_NOTIMPL;

        #endregion
    }
}
