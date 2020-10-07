// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        private readonly IAsyncServiceProvider2 _serviceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory(
            SVsServiceProvider serviceProvider, IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider)
        {
            _serviceProvider = (IAsyncServiceProvider2)serviceProvider;
            _threadingContext = threadingContext;

            // for now, we use workspace so existing tests can automatically wait for full solution load event
            // subscription done in test
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        }

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is VisualStudioWorkspace vsWorkspace)
            {
                var experimentationService = vsWorkspace.Services.GetService<IExperimentationService>();
                if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.PartialLoadMode))
                {
                    // don't enable partial load mode for ones that are not in experiment yet
                    return new WorkspaceStatusService();
                }

                // only VSWorkspace supports partial load mode
                return new Service(_serviceProvider, _threadingContext, _listener);
            }

            return new WorkspaceStatusService();
        }

        /// <summary>
        /// for prototype, we won't care about what solution is actually fully loaded. 
        /// we will just see whatever solution VS has at this point of time has actually fully loaded
        /// </summary>
        private class Service : IWorkspaceStatusService
        {
            private readonly SemaphoreSlim _initializationGate = new(initialCount: 1);
            private readonly IAsyncServiceProvider2 _serviceProvider;
            private readonly IThreadingContext _threadingContext;

            private bool _initialized = false;

            /// <summary>
            /// A task indicating that the <see cref="Guids.GlobalHubClientPackageGuid"/> package has loaded. Calls to
            /// <see cref="IServiceBroker.GetProxyAsync"/> may have a main thread dependency if the proffering package
            /// is not loaded prior to the call.
            /// </summary>
            private JoinableTask _loadHubClientPackage;

            public event EventHandler StatusChanged;

            public Service(IAsyncServiceProvider2 serviceProvider, IThreadingContext threadingContext, IAsynchronousOperationListener listener)
            {
                _serviceProvider = serviceProvider;
                _threadingContext = threadingContext;

                // pre-emptively make sure event is subscribed. if APIs are called before it is done, calls will be blocked
                // until event subscription is done
                var asyncToken = listener.BeginAsyncOperation("StatusChanged_EventSubscription");
                Task.Run(() => EnsureInitializationAsync(CancellationToken.None), CancellationToken.None).CompletesAsyncOperation(asyncToken);
            }

            // unfortunately, IVsOperationProgressStatusService requires UI thread to let project system to proceed to next stages.
            // this method should only be used with either await or JTF.Run, it should be never used with Task.Wait otherwise, it can
            // deadlock
            //
            // This method also ensures the GlobalHubClientPackage package is loaded, since brokered services in Visual
            // Studio require this package to provide proxy interfaces for invoking out-of-process services.
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

                    await _loadHubClientPackage.JoinAsync(cancellationToken).ConfigureAwait(false);
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

                    _loadHubClientPackage = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                    {
                        // Use the disposal token, since the caller's cancellation token will apply instead to the
                        // JoinAsync operation.
                        await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(_threadingContext.DisposalToken);

                        // Make sure the HubClient package is loaded, since we rely on it for proffered OOP services
                        var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell7>().ConfigureAwait(true);
                        Assumes.Present(shell);

                        await shell.LoadPackageAsync(Guids.GlobalHubClientPackageGuid);
                    });

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
