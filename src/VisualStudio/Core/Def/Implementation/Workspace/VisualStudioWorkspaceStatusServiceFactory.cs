// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        private IAsyncServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory(IThreadingContext threadingContext, SVsServiceProvider serviceProvider)
        {
            threadingContext.JoinableTaskFactory.Run(async () =>
            {
                // Not sure how to get IAsyncServiceProvider from MEF in mef v2
                await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();
                _serviceProvider = (IAsyncServiceProvider)serviceProvider;
            });
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
                return new Service(_serviceProvider);
            }

            return WorkspaceStatusService.Default;
        }

        /// <summary>
        /// for prototype, we won't care about what solution is actually fully loaded. 
        /// we will just see whatever solution VS has at this point of time has actually fully loaded
        /// </summary>
        private class Service : IWorkspaceStatusService
        {
            private readonly IAsyncServiceProvider _serviceProvider;

            public event EventHandler<bool> StatusChanged;

            public Service(IAsyncServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;

                // TODO: there is no right place to register event?
                Task.Run(RegisterEventAsync, CancellationToken.None);

                async Task RegisterEventAsync()
                {
                    var status = await GetProgressStageStatusAsync().ConfigureAwait(false);
                    if (status == null)
                    {
                        return;
                    }

                    status.InProgressChanged += (s, e) => this.StatusChanged?.Invoke(this, !e.Status.IsInProgress);
                }
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
                var service = (IVsOperationProgressStatusService)await _serviceProvider.GetServiceAsync(typeof(IVsOperationProgressStatusService)).ConfigureAwait(false);
                if (service == null)
                {
                    // when can this return null?
                    return null;
                }

                return service.GetStageStatus(CommonOperationProgressStageIds.Intellisense);
            }
        }
    }
}
