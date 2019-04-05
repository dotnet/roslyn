// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
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
            private readonly IAsyncServiceProvider2 _serviceProvider;

            public event EventHandler<bool> StatusChanged;

            public Service(IAsyncServiceProvider2 serviceProvider, IAsynchronousOperationListener listener)
            {
                _serviceProvider = serviceProvider;

                var asyncToken = listener.BeginAsyncOperation("StatusChanged_EventSubscription");
                Task.Run(async () =>
                {
                    // with IAsyncServiceProvider to get the service from BG, there is not much else
                    // we can do to avoid this pattern to subscribe to events
                    var status = await GetProgressStageStatusAsync().ConfigureAwait(false);
                    if (status == null)
                    {
                        return;
                    }

                    status.InProgressChanged += (_, e) => this.StatusChanged?.Invoke(this, !e.Status.IsInProgress);
                }, CancellationToken.None).CompletesAsyncOperation(asyncToken);
            }

            public async Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
            {
                var status = await GetProgressStageStatusAsync().ConfigureAwait(false);
                if (status == null)
                {
                    return;
                }

                // TODO: this should have cancellation token
                await status.WaitForCompletionAsync().ConfigureAwait(false);
            }

            public async Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
            {
                var status = await GetProgressStageStatusAsync().ConfigureAwait(false);
                if (status == null)
                {
                    return false;
                }

                return !status.Status.IsInProgress;
            }

            private async Task<IVsOperationProgressStageStatus> GetProgressStageStatusAsync()
            {
                var service = await _serviceProvider.GetServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(throwOnFailure: false).ConfigureAwait(false);
                return service?.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            }
        }
    }
}
