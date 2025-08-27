// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.Internal.VisualStudio.Shell.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Legacy;

/// <summary>
/// Creates batch scopes for projects based on solution and running document table events. This is useful for projects types that don't otherwise have
/// good batching concepts.
/// </summary> 
[Export(typeof(SolutionEventsBatchScopeCreator))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SolutionEventsBatchScopeCreator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
{
    /// <summary>
    /// A lock for mutating all objects in this object. This class isn't expected to have any "interesting" locking requirements, so this should just be acquired
    /// in all methods.
    /// </summary>
    private readonly object _gate = new object();
    private readonly List<(ProjectSystemProject project, IVsHierarchy hierarchy, ProjectSystemProject.BatchScope batchScope)> _fullSolutionLoadScopes = [];

    /// <summary>
    /// The cookie for our subscription to the running document table. Null if we're not currently subscribed.
    /// </summary>
    private uint? _runningDocumentTableEventsCookie;
    private bool _isSubscribedToSolutionEvents = false;
    private bool _solutionLoading = false;

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    private void UpdateSolutionLoading(bool solutionLoading)
    {
        // We acquire a lock here just so there's no surprise of this changing underneath any of the other operations of this class.
        lock (_gate)
        {
            _solutionLoading = solutionLoading;
        }
    }

    public void StartTrackingProject(ProjectSystemProject project, IVsHierarchy hierarchy)
    {
        lock (_gate)
        {
            EnsureSubscribedToSolutionEvents();

            if (_solutionLoading)
            {
                _fullSolutionLoadScopes.Add((project, hierarchy, project.CreateBatchScope()));

                EnsureSubscribedToRunningDocumentTableEvents();
            }
        }
    }

    public void StopTrackingProject(ProjectSystemProject project)
    {
        lock (_gate)
        {
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
    }

    private Task StopTrackingAllProjectsAsync(CancellationToken cancellationToken)
    {
        ImmutableArray<Task> batchScopeTasks;

        lock (_gate)
        {
            // Kick off on a background thread the work to close each of the batches. The expectation is each batch closure will fairly quickly hit the solution-level
            // semaphore, so we don't need to explicitly throttle this work here.
            batchScopeTasks = _fullSolutionLoadScopes.SelectAsArray(static s => Task.Run(() => s.batchScope.DisposeAsync().AsTask()));

            _fullSolutionLoadScopes.Clear();

            EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded();
        }

        return Task.WhenAll(batchScopeTasks).WithCancellation(cancellationToken);
    }

    private void StopTrackingAllProjectsMatchingHierarchy(IVsHierarchy hierarchy)
    {
        lock (_gate)
        {
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
    }

    private void EnsureSubscribedToSolutionEvents()
    {
        lock (_gate)
        {
            if (_isSubscribedToSolutionEvents)
            {
                return;
            }

            var backgroundSolution = (IVsBackgroundSolution)_serviceProvider.GetService(typeof(SVsBackgroundSolution));

            // We never unsubscribe from these, so we just throw out the subscription. We could consider unsubscribing if/when all our
            // projects are unloaded, but it seems fairly unnecessary -- it'd only be useful if somebody closed one solution but then
            // opened other solutions in entirely different languages from there.
            _ = backgroundSolution.SubscribeListener(new SolutionEventsEventListener(this));

            _solutionLoading = backgroundSolution.IsSolutionOpening;
            _isSubscribedToSolutionEvents = true;
        }
    }

    private void EnsureSubscribedToRunningDocumentTableEvents()
    {
        lock (_gate)
        {
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
    }

    private void EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded()
    {
        lock (_gate)
        {
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
    }

    private sealed class SolutionEventsEventListener : IVsAsyncSolutionEventListener
    {
        private readonly SolutionEventsBatchScopeCreator _scopeCreator;

        public SolutionEventsEventListener(SolutionEventsBatchScopeCreator scopeCreator)
            => _scopeCreator = scopeCreator;

        public ValueTask OnBeforeOpenSolutionAsync(BeforeOpenSolutionArgs args, CancellationToken cancellationToken)
        {
            Contract.ThrowIfTrue(_scopeCreator._fullSolutionLoadScopes.Any());

            _scopeCreator.UpdateSolutionLoading(true);

            return ValueTask.CompletedTask;
        }

        public async ValueTask OnAfterOpenSolutionAsync(AfterOpenSolutionArgs args, CancellationToken cancellationToken)
        {
            _scopeCreator.UpdateSolutionLoading(false);
            await _scopeCreator.StopTrackingAllProjectsAsync(cancellationToken).ConfigureAwait(false);
        }

        public void OnUnhandledException(Exception exception)
        {
        }

        #region Unimplemented Members

        public ValueTask OnBeforeCloseSolutionAsync(BeforeCloseSolutionArgs args, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask OnAfterCloseSolutionAsync(AfterCloseSolutionArgs args, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask OnAfterRenameSolutionAsync(AfterRenameSolutionArgs args, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

        #endregion
    }

    private sealed class RunningDocumentTableEventSink : IVsRunningDocTableEvents
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
