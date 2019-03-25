// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServices.Experimentation;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is VisualStudioWorkspace vsWorkspace)
            {
                var experimentationService = vsWorkspace.Services.GetService<IExperimentationService>();
                if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.PartialLoadMode))
                {
                    // don't enable partial load mode for ones that are not in experiment yet
                    return WorkspaceStatusService.Default;
                }

                // only VSWorkspace supports partial load mode
                return new Service(vsWorkspace);
            }

            return WorkspaceStatusService.Default;
        }

        /// <summary>
        /// for prototype, we won't care about what solution is actually fully loaded. 
        /// we will just see whatever solution VS has at this point of time has actually fully loaded
        /// </summary>
        private class Service : IWorkspaceStatusService
        {
            private readonly VisualStudioWorkspace _workspace;

            // only needed for testing
            private ResettableDelay _lastTimeCalled;

            public event EventHandler<bool> StatusChanged;

            public Service(VisualStudioWorkspace workspace)
            {
                // until we get new platform API, use legacy one that is not fully do what we want
                _workspace = workspace;
            }

            public async System.Threading.Tasks.Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
            {
                if (_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_ForceDelay))
                {
                    await System.Threading.Tasks.Task.Delay(_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_DelayInMS)).ConfigureAwait(false);
                }

                if (await IsFullyLoadedAsync(cancellationToken).ConfigureAwait(false))
                {
                    // already fully loaded
                    return;
                }

                var taskCompletionSource = new TaskCompletionSource<object>();

                // we are using this API for now, until platform provide us new API for prototype
                KnownUIContexts.SolutionExistsAndFullyLoadedContext.WhenActivated(() => taskCompletionSource.SetResult(null));

                await taskCompletionSource.Task.ConfigureAwait(false);
            }

            public Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            {
                if (_workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_ForceDelay))
                {
                    // this is for prototype/mock for people/teams trying partial solution load prototyping
                    // it shouldn't concern anyone outside of that group. 
                    //
                    // "experimentationService.IsExperimentEnabled(WellKnownExperimentNames.PartialLoadMode)" check above
                    // make sure this part of code "Service : IWorkspaceStatusService" is not exposed anyone outside of
                    // the partial solution load group.
                    //
                    // for ones that is in the group, one can try lightbulb behavior through internal option page
                    // such as moving around caret on lines where LB should show up.
                    //
                    // it works as below
                    // 1. when this is first called, we return false and start timer to change its status in delayInMS
                    // 2. once delayInMs passed, we clear the delay and return true.
                    // 3. if it is called before the timeout, we reset the timer and return false
                    if (_lastTimeCalled == null)
                    {
                        var delay = _workspace.Options.GetOption(ExperimentationOptions.SolutionStatusService_DelayInMS);
                        _lastTimeCalled = new ResettableDelay(delayInMilliseconds: delay, AsynchronousOperationListenerProvider.NullListener);
                        _ = _lastTimeCalled.Task.SafeContinueWith(_ => StatusChanged?.Invoke(this, true), TaskScheduler.Default);

                        return SpecializedTasks.False;
                    }

                    if (_lastTimeCalled.Task.IsCompleted)
                    {
                        _lastTimeCalled = null;
                        return SpecializedTasks.True;
                    }

                    _lastTimeCalled.Reset();
                    return SpecializedTasks.False;
                }

                // we are using this API for now, until platform provide us new API for prototype
                return KnownUIContexts.SolutionExistsAndFullyLoadedContext.IsActive ? SpecializedTasks.True : SpecializedTasks.False;
            }
        }
    }
}
