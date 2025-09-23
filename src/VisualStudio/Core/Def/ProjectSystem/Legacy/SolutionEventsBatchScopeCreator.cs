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
internal sealed class SolutionEventsBatchScopeCreator
{
    /// <summary>
    /// A lock for mutating all objects in this object. This class isn't expected to have any "interesting" locking requirements, so this should just be acquired
    /// in all methods.
    /// </summary>
    private readonly object _gate = new();
    private readonly List<(ProjectSystemProject project, IVsHierarchy hierarchy, ProjectSystemProject.BatchScope batchScope)> _fullSolutionLoadScopes = [];

    /// <summary>
    /// The cookie for our subscription to the running document table. Null if we're not currently subscribed.
    /// </summary>
    private uint? _runningDocumentTableEventsCookie;
    private bool _isSubscribedToSolutionEvents = false;

    private readonly IVsBackgroundSolution _backgroundSolution;
    private readonly IVsRunningDocumentTable _runningDocumentTable;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SolutionEventsBatchScopeCreator([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
    {
        // Fetch services we're going to need later; these are all free-threaded and cacheable on creation, and since we're only going to be
        // creating this part once we're in a solution load, the services would have already been created.
        _backgroundSolution = (IVsBackgroundSolution)serviceProvider.GetService(typeof(SVsBackgroundSolution));
        _runningDocumentTable = (IVsRunningDocumentTable)serviceProvider.GetService(typeof(SVsRunningDocumentTable));
    }

    public void StartTrackingProject(ProjectSystemProject project, IVsHierarchy hierarchy)
    {
        lock (_gate)
        {
            EnsureSubscribedToSolutionEvents();

            if (_backgroundSolution.IsSolutionOpening)
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

    /// <summary>
    /// Closes all batch scopes for all currently tracked projects, called when the solution has finished loading.
    /// </summary>
    private Task StopTrackingAllProjectsAsync()
    {
        ImmutableArray<Task> batchScopeTasks;

        lock (_gate)
        {
            // Kick off on a background thread the work to close each of the batches. The expectation is each batch closure will fairly quickly hit the solution-level
            // semaphore, so we don't need to explicitly throttle this work here.
            batchScopeTasks = _fullSolutionLoadScopes.SelectAsArray(static s => Task.Run(() => s.batchScope.DisposeAsync().AsTask()));

            // We always want to ensure we clear out the list and unsubscribe, even if cancellation has been requested.
            _fullSolutionLoadScopes.Clear();

            EnsureUnsubscribedFromRunningDocumentTableEventsIfNoLongerNeeded();
        }

        return Task.WhenAll(batchScopeTasks);
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

            // We never unsubscribe from these, so we just throw out the subscription. We could consider unsubscribing if/when all our
            // projects are unloaded, but it seems fairly unnecessary -- it'd only be useful if somebody closed one solution but then
            // opened other solutions in entirely different languages from there.
            _ = _backgroundSolution.SubscribeListener(new SolutionEventsEventListener(this));

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

            if (ErrorHandler.Succeeded(_runningDocumentTable.AdviseRunningDocTableEvents(new RunningDocumentTableEventSink(this, _runningDocumentTable), out var runningDocumentTableEventsCookie)))
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

            _runningDocumentTable.UnadviseRunningDocTableEvents(_runningDocumentTableEventsCookie.Value);
            _runningDocumentTableEventsCookie = null;
        }
    }

    private sealed class SolutionEventsEventListener : IVsAsyncSolutionEventListener
    {
        private readonly SolutionEventsBatchScopeCreator _scopeCreator;

        public SolutionEventsEventListener(SolutionEventsBatchScopeCreator scopeCreator)
            => _scopeCreator = scopeCreator;

        public async ValueTask OnAfterOpenSolutionAsync(AfterOpenSolutionArgs args, CancellationToken cancellationToken)
        {
            // NOTE: the cancellationToken here might be cancelled if the user has requested that we cancel the solution load. If the cancellation happened
            // prior to this method being invoked, we might see this method invoked with the token cancelled from the very start. We want to make sure
            // we get rid of all the batch scopes in that case before checking the cancellation token. Thus we won't pass the token to StopTrackingAllProjectsAsync.
            await _scopeCreator.StopTrackingAllProjectsAsync().WithCancellation(cancellationToken).ConfigureAwait(false);
        }

        #region Unimplemented Members

        public void OnUnhandledException(Exception exception)
        {
        }

        public ValueTask OnBeforeOpenSolutionAsync(BeforeOpenSolutionArgs args, CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }

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
