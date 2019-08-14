// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IAsyncServiceProvider2 _serviceProvider;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory(
            SVsServiceProvider serviceProvider, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _serviceProvider = (IAsyncServiceProvider2)serviceProvider;

            // for now, we use workspace so existing tests can automatically wait for full solution load event
            // subscription done in test
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
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
                return new Service(_serviceProvider, _listener);
            }

            return WorkspaceStatusService.Default;
        }

        /// <summary>
        /// for prototype, we won't care about what solution is actually fully loaded. 
        /// we will just see whatever solution VS has at this point of time has actually fully loaded
        /// </summary>
        private class Service : IWorkspaceStatusService
        {
            private readonly SemaphoreSlim _initializationGate = new SemaphoreSlim(initialCount: 1);
            private readonly IAsyncServiceProvider2 _serviceProvider;

            private bool _initialized = false;

            public event EventHandler StatusChanged;

            public Service(IAsyncServiceProvider2 serviceProvider, IAsynchronousOperationListener listener)
            {
                _serviceProvider = serviceProvider;

                // pre-emptively make sure event is subscribed. if APIs are called before it is done, calls will be blocked
                // until event subscription is done
                var asyncToken = listener.BeginAsyncOperation("StatusChanged_EventSubscription");
                Task.Run(() => EnsureInitializationAsync(CancellationToken.None), CancellationToken.None).CompletesAsyncOperation(asyncToken);
            }

            // unfortunately, IVsOperationProgressStatusService requires UI thread to let project system to proceed to next stages.
            // this method should only be used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can
            // deadlock
            public async Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
            {
                using (Logger.LogBlock(FunctionId.PartialLoad_FullyLoaded, KeyValueLogMessage.NoProperty, cancellationToken))
                {
                    await EnsureInitializationAsync(cancellationToken).ConfigureAwait(false);

                    var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                    if (status == null)
                    {
                        return;
                    }

                    var completionTask = status.WaitForCompletionAsync();
                    Logger.Log(FunctionId.PartialLoad_FullyLoaded, KeyValueLogMessage.Create(LogType.Trace, m => m["AlreadyFullyLoaded"] = completionTask.IsCompleted));

                    // TODO: WaitForCompletionAsync should accept cancellation directly.
                    //       for now, use WithCancellation to indirectly add cancellation
                    await completionTask.WithCancellation(cancellationToken).ConfigureAwait(false);
                }
            }

            // unfortunately, IVsOperationProgressStatusService requires UI thread to let project system to proceed to next stages.
            // this method should only be used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can
            // deadlock
            public async Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            {
                await EnsureInitializationAsync(cancellationToken).ConfigureAwait(false);

                var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                if (status == null)
                {
                    return false;
                }

                return !status.IsInProgress;
            }

            private async Task<IVsOperationProgressStageStatusForSolutionLoad> GetProgressStageStatusAsync(CancellationToken cancellationToken)
            {
                var service = await _serviceProvider.GetServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(throwOnFailure: false)
                                                    .WithCancellation(cancellationToken).ConfigureAwait(false);

                return service?.GetStageStatusForSolutionLoad(CommonOperationProgressStageIds.Intellisense);
            }

            private async Task EnsureInitializationAsync(CancellationToken cancellationToken)
            {
                using (await _initializationGate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (_initialized)
                    {
                        return;
                    }

                    _initialized = true;

                    // with IAsyncServiceProvider, to get a service from BG, there is not much else
                    // we can do to avoid this pattern to subscribe to events
                    var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                    if (status == null)
                    {
                        return;
                    }

                    status.PropertyChanged += (_, e) => this.StatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
