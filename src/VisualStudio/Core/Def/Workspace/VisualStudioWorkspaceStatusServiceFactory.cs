// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.OperationProgress;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IAsyncServiceProvider2 = Microsoft.VisualStudio.Shell.IAsyncServiceProvider2;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [ExportWorkspaceServiceFactory(typeof(IWorkspaceStatusService), ServiceLayer.Host), Shared]
    internal sealed class VisualStudioWorkspaceStatusServiceFactory : IWorkspaceServiceFactory
    {
        private static readonly Option2<bool> s_partialLoadModeFeatureFlag = new("visual_studio_workspace_partial_load_mode", defaultValue: false);

        private readonly IAsyncServiceProvider2 _serviceProvider;
        private readonly IThreadingContext _threadingContext;
        private readonly IGlobalOptionService _globalOptions;
        private readonly IAsynchronousOperationListener _listener;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceStatusServiceFactory(
            SVsServiceProvider serviceProvider,
            IThreadingContext threadingContext,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _serviceProvider = (IAsyncServiceProvider2)serviceProvider;
            _threadingContext = threadingContext;
            _globalOptions = globalOptions;

            // for now, we use workspace so existing tests can automatically wait for full solution load event
            // subscription done in test
            _listener = listenerProvider.GetListener(FeatureAttribute.Workspace);
        }

        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (workspaceServices.Workspace is VisualStudioWorkspace)
            {
                if (!_globalOptions.GetOption(s_partialLoadModeFeatureFlag))
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
            private readonly IAsyncServiceProvider2 _serviceProvider;
            private readonly IThreadingContext _threadingContext;

            /// <summary>
            /// A task indicating that the <see cref="Guids.GlobalHubClientPackageGuid"/> package has loaded. Calls to
            /// <see cref="IServiceBroker.GetProxyAsync"/> may have a main thread dependency if the proffering package
            /// is not loaded prior to the call.
            /// </summary>
            private readonly JoinableTask _loadHubClientPackage;

            /// <summary>
            /// A task providing the result of asynchronous computation of
            /// <see cref="IVsOperationProgressStatusService.GetStageStatusForSolutionLoad"/>. The result of this
            /// operation is accessed through <see cref="GetProgressStageStatusAsync"/>.
            /// </summary>
            private readonly JoinableTask<IVsOperationProgressStageStatusForSolutionLoad?> _progressStageStatus;

            public event EventHandler? StatusChanged;

            public Service(IAsyncServiceProvider2 serviceProvider, IThreadingContext threadingContext, IAsynchronousOperationListener listener)
            {
                _serviceProvider = serviceProvider;
                _threadingContext = threadingContext;

                _loadHubClientPackage = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    // Use the disposal token, since the caller's cancellation token will apply instead to the
                    // JoinAsync operation in GetProgressStageStatusAsync.
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _threadingContext.DisposalToken);

                    // Make sure the HubClient package is loaded, since we rely on it for proffered OOP services
                    var shell = await _serviceProvider.GetServiceAsync<SVsShell, IVsShell7>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);
                    Assumes.Present(shell);

                    await shell.LoadPackageAsync(Guids.GlobalHubClientPackageGuid);
                });

                _progressStageStatus = _threadingContext.JoinableTaskFactory.RunAsync(async () =>
                {
                    // pre-emptively make sure event is subscribed. if APIs are called before it is done, calls will be blocked
                    // until event subscription is done
                    using var asyncToken = listener.BeginAsyncOperation("StatusChanged_EventSubscription");

                    await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(alwaysYield: true, _threadingContext.DisposalToken);
                    var service = await serviceProvider.GetServiceAsync<SVsOperationProgress, IVsOperationProgressStatusService>(_threadingContext.JoinableTaskFactory, throwOnFailure: false).ConfigureAwait(true);
                    if (service is null)
                        return null;

                    var status = service.GetStageStatusForSolutionLoad(CommonOperationProgressStageIds.Intellisense);
                    status.PropertyChanged += (_, _) => StatusChanged?.Invoke(this, EventArgs.Empty);

                    return status;
                });
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
                    var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                    if (status == null)
                    {
                        return;
                    }

                    var completionTask = status.WaitForCompletionAsync();
                    Logger.Log(FunctionId.PartialLoad_FullyLoaded, KeyValueLogMessage.Create(LogType.Trace, m => m["AlreadyFullyLoaded"] = completionTask.IsCompleted, LogLevel.Debug));

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
                var status = await GetProgressStageStatusAsync(cancellationToken).ConfigureAwait(false);
                if (status == null)
                {
                    return false;
                }

                return !status.IsInProgress;
            }

            private async ValueTask<IVsOperationProgressStageStatusForSolutionLoad?> GetProgressStageStatusAsync(CancellationToken cancellationToken)
            {
                // Workaround for lack of fast path in JoinAsync; avoid calling when already completed
                // https://github.com/microsoft/vs-threading/pull/696
                if (_progressStageStatus.Task.IsCompleted)
                {
                    return await _progressStageStatus.Task.ConfigureAwait(false);
                }

                return await _progressStageStatus.JoinAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
